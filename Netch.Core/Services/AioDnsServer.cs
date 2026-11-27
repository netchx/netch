using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using Netch.Servers;
using Netch.Utils;

namespace Netch.Services;

public sealed class AioDnsOptions
{
    public IPAddress ListenAddress { get; init; } = IPAddress.Loopback;

    public ushort ListenPort { get; init; } = 53;

    public string? RuleFilePath { get; init; }

    public string? LogFilePath { get; init; }

    public required DnsEndpoint ChinaEndpoint { get; init; }

    public required DnsEndpoint OtherEndpoint { get; init; }

    public IReadOnlyList<IPAddress>? ChinaBootstrapIps { get; init; }

    public IReadOnlyList<IPAddress>? OtherBootstrapIps { get; init; }

    public SocksServer? OtherSocks5Proxy { get; init; }

    public Func<DnsEndpoint, IReadOnlyList<IPAddress>?, SocksServer?, HttpMessageHandler>? DohHandlerFactory { get; init; }
}

public sealed class DomainRuleMatcher
{
    private readonly HashSet<string> _rules;

    public DomainRuleMatcher(IEnumerable<string> domains)
    {
        _rules = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in domains)
        {
            var normalized = NormalizeRule(raw);
            if (!string.IsNullOrEmpty(normalized))
                _rules.Add(normalized);
        }
    }

    public int Count => _rules.Count;

    public static DomainRuleMatcher LoadFromFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new DomainRuleMatcher([]);

        return new DomainRuleMatcher(File.ReadLines(path));
    }

    public bool IsChinaDomain(string? qname)
    {
        if (string.IsNullOrWhiteSpace(qname) || _rules.Count == 0)
            return false;

        var normalized = qname.Trim().Trim('.').ToLowerInvariant();
        if (normalized.Length == 0)
            return false;

        var lookup = _rules.GetAlternateLookup<ReadOnlySpan<char>>();
        var span = normalized.AsSpan();
        while (true)
        {
            if (lookup.Contains(span))
                return true;

            var dot = span.IndexOf('.');
            if (dot < 0 || dot + 1 >= span.Length)
                return false;

            span = span[(dot + 1)..];
        }
    }

    public static bool IsInAddrArpa(string? qname)
    {
        if (string.IsNullOrWhiteSpace(qname))
            return false;

        var span = qname.AsSpan().Trim().Trim('.');
        return span.Equals("in-addr.arpa", StringComparison.OrdinalIgnoreCase) ||
               span.EndsWith(".in-addr.arpa", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRule(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('#') || trimmed.StartsWith(';'))
            return string.Empty;

        return trimmed.Trim('.').ToLowerInvariant();
    }
}

public static class DnsWireMessage
{
    private static readonly byte[] PtrRDataNetch = [5, (byte)'N', (byte)'e', (byte)'t', (byte)'c', (byte)'h', 0];

    public static bool TryParseQuestion(
        ReadOnlySpan<byte> packet,
        out string qname,
        out ushort qtype,
        out int questionEndOffset)
    {
        qname = string.Empty;
        qtype = 0;
        questionEndOffset = 0;

        if (packet.Length < 12)
            return false;

        var qdCount = BinaryPrimitives.ReadUInt16BigEndian(packet[4..6]);
        if (qdCount == 0)
            return false;

        var offset = 12;
        var endAfterName = -1;
        var jumps = 0;
        var sb = new StringBuilder(64);

        while (true)
        {
            if (offset >= packet.Length)
                return false;

            var len = packet[offset];
            if (len == 0)
            {
                if (endAfterName < 0)
                    endAfterName = offset + 1;
                break;
            }

            if ((len & 0xC0) == 0xC0)
            {
                if (offset + 1 >= packet.Length || ++jumps > 16)
                    return false;

                if (endAfterName < 0)
                    endAfterName = offset + 2;

                var pointer = ((len & 0x3F) << 8) | packet[offset + 1];
                if (pointer >= packet.Length || pointer < 12)
                    return false;

                offset = pointer;
                continue;
            }

            if ((len & 0xC0) != 0)
                return false;

            offset++;
            if (offset + len > packet.Length)
                return false;

            if (sb.Length > 0)
                sb.Append('.');

            if (sb.Length + len > 255)
                return false;

            sb.Append(Encoding.ASCII.GetString(packet.Slice(offset, len)));
            offset += len;
        }

        if (endAfterName < 0 || endAfterName + 4 > packet.Length)
            return false;

        qname = sb.ToString();
        qtype = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(endAfterName, 2));
        questionEndOffset = endAfterName + 4;
        return true;
    }

    public static bool IsTruncated(ReadOnlySpan<byte> packet) =>
        packet.Length >= 12 && (packet[2] & 0x02) != 0;

    public static byte[] BuildServFail(ReadOnlySpan<byte> request) =>
        BuildErrorResponse(request, rcode: 2, truncated: false);

    public static byte[] BuildTruncatedReply(ReadOnlySpan<byte> request) =>
        BuildErrorResponse(request, rcode: 0, truncated: true);

    private static byte[] BuildErrorResponse(ReadOnlySpan<byte> request, byte rcode, bool truncated)
    {
        if (request.Length < 2)
            return [];

        var hasQuestion = TryParseQuestion(request, out _, out _, out var qEnd);
        var length = hasQuestion ? qEnd : 12;
        var resp = new byte[length];

        resp[0] = request[0];
        resp[1] = request[1];
        var reqFlags1 = request.Length > 2 ? request[2] : (byte)0x01;
        resp[2] = (byte)((reqFlags1 & 0x79) | 0x80 | (truncated ? 0x02 : 0x00));
        resp[3] = (byte)(0x80 | (rcode & 0x0F));

        if (hasQuestion)
        {
            BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(4, 2), 1);
            request[12..qEnd].CopyTo(resp.AsSpan(12));
        }

        return resp;
    }

    public static byte[] BuildNetchPtrReply(ReadOnlySpan<byte> request)
    {
        if (!TryParseQuestion(request, out _, out _, out var qEnd))
            return BuildServFail(request);

        var answerLen = 2 + 2 + 2 + 4 + 2 + PtrRDataNetch.Length;
        var resp = new byte[qEnd + answerLen];
        request[..qEnd].CopyTo(resp);

        resp[2] = (byte)((request[2] & 0x79) | 0x80);
        resp[3] = 0x80;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(6, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(8, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(10, 2), 0);

        var pos = qEnd;
        resp[pos++] = 0xC0;
        resp[pos++] = 0x0C;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(pos, 2), 12); // PTR
        pos += 2;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(pos, 2), 1); // IN
        pos += 2;
        BinaryPrimitives.WriteUInt32BigEndian(resp.AsSpan(pos, 4), 60); // TTL
        pos += 4;
        BinaryPrimitives.WriteUInt16BigEndian(resp.AsSpan(pos, 2), (ushort)PtrRDataNetch.Length);
        pos += 2;
        PtrRDataNetch.CopyTo(resp.AsSpan(pos));
        return resp;
    }

    public static byte[] CopyWithTransactionId(ReadOnlySpan<byte> packet, ushort txId)
    {
        var copy = packet.ToArray();
        if (copy.Length >= 2)
            BinaryPrimitives.WriteUInt16BigEndian(copy.AsSpan(0, 2), txId);
        return copy;
    }

    public static ushort ReadAnswerCount(ReadOnlySpan<byte> packet) =>
        packet.Length >= 8 ? BinaryPrimitives.ReadUInt16BigEndian(packet[6..8]) : (ushort)0;

    public static string QTypeToString(ushort qtype) => qtype switch
    {
        1 => "A",
        2 => "NS",
        5 => "CNAME",
        6 => "SOA",
        12 => "PTR",
        15 => "MX",
        16 => "TXT",
        28 => "AAAA",
        33 => "SRV",
        64 => "SVCB",
        65 => "HTTPS",
        255 => "ANY",
        _ => $"TYPE{qtype}"
    };
}

public sealed class AioDnsServer : IAsyncDisposable
{
    private const int UdpBufferSize = 4096;
    private const int TcpMaxFrameSize = 65535;
    private static readonly TimeSpan UpstreamTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan TcpIdleTimeout = TimeSpan.FromSeconds(10);

    private readonly AioDnsOptions _options;
    private readonly DomainRuleMatcher _matcher;
    private readonly AioDnsUpstreamClient _chinaClient;
    private readonly AioDnsUpstreamClient _otherClient;
    private readonly object _logLock = new();

    private StreamWriter? _logWriter;
    private Socket? _udpSocket;
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cts;
    private Task? _udpLoopTask;
    private Task? _tcpLoopTask;

    public AioDnsServer(AioDnsOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _matcher = DomainRuleMatcher.LoadFromFile(_options.RuleFilePath);
        _chinaClient = new AioDnsUpstreamClient("ChinaDNS", _options.ChinaEndpoint, _options.ChinaBootstrapIps, null, _options.DohHandlerFactory);
        _otherClient = new AioDnsUpstreamClient("OtherDNS", _options.OtherEndpoint, _options.OtherBootstrapIps, _options.OtherSocks5Proxy, _options.DohHandlerFactory);
    }

    public ushort BoundUdpPort { get; private set; }

    public ushort BoundTcpPort { get; private set; }

    public Task StartAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        OpenLogFile();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            var udp = new Socket(_options.ListenAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            DisableUdpConnReset(udp);
            udp.Bind(new IPEndPoint(_options.ListenAddress, _options.ListenPort));
            _udpSocket = udp;
            BoundUdpPort = (ushort)((udp.LocalEndPoint as IPEndPoint)?.Port ?? _options.ListenPort);

            var tcpPort = _options.ListenPort == 0 ? 0 : _options.ListenPort;
            var tcp = new TcpListener(_options.ListenAddress, tcpPort);
            tcp.Start();
            _tcpListener = tcp;
            BoundTcpPort = (ushort)((IPEndPoint)tcp.LocalEndpoint).Port;

            _udpLoopTask = Task.Run(() => RunUdpLoopAsync(udp, token), CancellationToken.None);
            _tcpLoopTask = Task.Run(() => RunTcpLoopAsync(tcp, token), CancellationToken.None);

            WriteLog("INFO",
                $"started: listen_udp={_options.ListenAddress}:{BoundUdpPort} listen_tcp={_options.ListenAddress}:{BoundTcpPort} rules={_matcher.Count} ChinaDNS={_options.ChinaEndpoint.Normalized} OtherDNS={_options.OtherEndpoint.Normalized}");
        }
        catch
        {
            DisposeSockets();
            CloseLogFile();
            _cts?.Dispose();
            _cts = null;
            throw;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        WriteLog("INFO", "stopping");
        var cts = Interlocked.Exchange(ref _cts, null);
        if (cts != null)
        {
            try { await cts.CancelAsync(); }
            catch (ObjectDisposedException) { }
        }

        DisposeSockets();

        var loops = new List<Task>(2);
        if (_udpLoopTask != null)
            loops.Add(_udpLoopTask);
        if (_tcpLoopTask != null)
            loops.Add(_tcpLoopTask);
        if (loops.Count > 0)
        {
            try { await Task.WhenAll(loops).WaitAsync(TimeSpan.FromSeconds(3)); }
            catch (Exception) { }
        }

        _udpLoopTask = null;
        _tcpLoopTask = null;
        cts?.Dispose();

        _chinaClient.Dispose();
        _otherClient.Dispose();
        CloseLogFile();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private void DisposeSockets()
    {
        try { _udpSocket?.Close(); } catch (Exception) { }
        _udpSocket?.Dispose();
        _udpSocket = null;

        try { _tcpListener?.Stop(); } catch (Exception) { }
        _tcpListener = null;
    }

    private async Task RunUdpLoopAsync(Socket socket, CancellationToken ct)
    {
        var buffer = new byte[UdpBufferSize];
        var remoteAny = (EndPoint)new IPEndPoint(_options.ListenAddress.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var received = await socket.ReceiveFromAsync(buffer.AsMemory(), SocketFlags.None, remoteAny, ct);
                if (received.ReceivedBytes <= 0)
                    continue;

                var query = buffer.AsSpan(0, received.ReceivedBytes).ToArray();
                var remote = received.RemoteEndPoint;
                _ = Task.Run(() => HandleUdpPacketAsync(socket, query, remote, ct), CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.OperationAborted or SocketError.Interrupted or SocketError.NotSocket)
            {
                break;
            }
            catch (SocketException ex)
            {
                WriteLog("WARN", $"UDP receive error: {ex.SocketErrorCode}");
            }
        }
    }

    private async Task HandleUdpPacketAsync(Socket socket, byte[] query, EndPoint remote, CancellationToken ct)
    {
        var response = await ProcessQueryAsync(query, ct);
        if (response.Length == 0 || ct.IsCancellationRequested)
            return;

        if (response.Length > UdpBufferSize)
            response = DnsWireMessage.BuildTruncatedReply(query);

        try
        {
            await socket.SendToAsync(response.AsMemory(), SocketFlags.None, remote, ct);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.MessageSize)
        {
            var truncated = DnsWireMessage.BuildTruncatedReply(query);
            if (truncated.Length > 0)
            {
                try { await socket.SendToAsync(truncated.AsMemory(), SocketFlags.None, remote, ct); }
                catch (Exception) { }
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException or SocketException)
        {
        }
    }

    private async Task RunTcpLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleTcpClientAsync(client, ct), CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.OperationAborted or SocketError.Interrupted or SocketError.NotSocket)
            {
                break;
            }
            catch (Exception ex)
            {
                WriteLog("WARN", $"TCP accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleTcpClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            client.NoDelay = true;
            await using var stream = client.GetStream();
            var lenBuf = new byte[2];

            while (!ct.IsCancellationRequested)
            {
                using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                idleCts.CancelAfter(TcpIdleTimeout);

                try
                {
                    var read = await ReadAtMostOrEofAsync(stream, lenBuf, idleCts.Token);
                    if (read == 0)
                        break;
                    if (read < 2)
                        break;

                    var frameLen = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
                    if (frameLen == 0)
                        break;

                    var query = new byte[frameLen];
                    await stream.ReadExactlyAsync(query, idleCts.Token);

                    var response = await ProcessQueryAsync(query, ct);
                    if (response.Length == 0 || response.Length > TcpMaxFrameSize)
                        response = DnsWireMessage.BuildServFail(query);

                    if (response.Length == 0)
                        break;

                    var frame = new byte[2 + response.Length];
                    BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0, 2), (ushort)response.Length);
                    response.CopyTo(frame.AsSpan(2));
                    await stream.WriteAsync(frame, ct);
                }
                catch (Exception ex) when (ex is OperationCanceledException or EndOfStreamException or IOException or ObjectDisposedException)
                {
                    break;
                }
            }
        }
    }

    private async Task<byte[]> ProcessQueryAsync(byte[] query, CancellationToken ct)
    {
        if (!DnsWireMessage.TryParseQuestion(query, out var qname, out var qtype, out _))
            return DnsWireMessage.BuildServFail(query);

        if (DomainRuleMatcher.IsInAddrArpa(qname))
            return DnsWireMessage.BuildNetchPtrReply(query);

        var isChina = _matcher.IsChinaDomain(qname);
        var client = isChina ? _chinaClient : _otherClient;
        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await client.ExchangeAsync(query, ct);
            var elapsedMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var answers = DnsWireMessage.ReadAnswerCount(response);
            WriteLog("INFO",
                $"query={qname}/{DnsWireMessage.QTypeToString(qtype)} resolver={client.Name} upstream={client.Endpoint.Normalized} elapsed={elapsedMs}ms answers={answers}");
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return [];
        }
        catch (Exception ex)
        {
            var elapsedMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            WriteLog("WARN",
                $"query={qname}/{DnsWireMessage.QTypeToString(qtype)} resolver={client.Name} upstream={client.Endpoint.Normalized} elapsed={elapsedMs}ms error={ex.Message}");
            return DnsWireMessage.BuildServFail(query);
        }
    }

    private static async Task<int> ReadAtMostOrEofAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct);
            if (n == 0)
                return total;
            total += n;
        }
        return total;
    }

    internal static void DisableUdpConnReset(Socket socket)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            const int SioUdpConnReset = unchecked((int)0x9800000C);
            socket.IOControl(SioUdpConnReset, [0, 0, 0, 0], null);
        }
        catch (SocketException)
        {
        }
    }

    private void OpenLogFile()
    {
        if (string.IsNullOrWhiteSpace(_options.LogFilePath))
            return;

        try
        {
            var dir = Path.GetDirectoryName(_options.LogFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var fs = new FileStream(_options.LogFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            lock (_logLock)
                _logWriter = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open AioDNS log file {LogPath}", _options.LogFilePath);
        }
    }

    private void CloseLogFile()
    {
        lock (_logLock)
        {
            _logWriter?.Dispose();
            _logWriter = null;
        }
    }

    private void WriteLog(string level, string message)
    {
        lock (_logLock)
        {
            if (_logWriter == null)
                return;

            try
            {
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                _logWriter.WriteLine($"[{stamp}][{level}] {message}");
            }
            catch (Exception)
            {
            }
        }
    }

    private sealed class AioDnsUpstreamClient : IDisposable
    {
        private readonly IReadOnlyList<IPAddress>? _bootstrapIps;
        private readonly SocksServer? _socks5Proxy;
        private readonly HttpClient? _httpClient;

        public AioDnsUpstreamClient(
            string name,
            DnsEndpoint endpoint,
            IReadOnlyList<IPAddress>? bootstrapIps,
            SocksServer? socks5Proxy,
            Func<DnsEndpoint, IReadOnlyList<IPAddress>?, SocksServer?, HttpMessageHandler>? dohHandlerFactory)
        {
            Name = name;
            Endpoint = endpoint;
            _bootstrapIps = bootstrapIps is { Count: > 0 } ? bootstrapIps : null;
            _socks5Proxy = socks5Proxy;

            if (endpoint.Transport == DnsTransport.Https)
            {
                var handler = dohHandlerFactory != null
                    ? dohHandlerFactory(endpoint, _bootstrapIps, _socks5Proxy)
                    : CreateDohHandler();
                _httpClient = new HttpClient(handler, disposeHandler: true)
                {
                    Timeout = UpstreamTimeout
                };
            }
        }

        public string Name { get; }

        public DnsEndpoint Endpoint { get; }

        public async Task<byte[]> ExchangeAsync(byte[] query, CancellationToken ct)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(UpstreamTimeout);
            var token = timeoutCts.Token;

            return Endpoint.Transport switch
            {
                DnsTransport.Udp => await ExchangeUdpAsync(query, token),
                DnsTransport.Tcp => await ExchangeTcpAsync(query, token),
                DnsTransport.Tls => await ExchangeTlsAsync(query, token),
                DnsTransport.Https => await ExchangeDohAsync(query, token),
                _ => throw new NotSupportedException($"Unsupported DNS transport: {Endpoint.Transport}")
            };
        }

        private async Task<byte[]> ExchangeUdpAsync(byte[] query, CancellationToken ct)
        {
            var targetIp = await ResolveSingleDirectIpAsync(ct);
            var remoteEndPoint = new IPEndPoint(targetIp, Endpoint.Port);
            using var socket = new Socket(targetIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            DisableUdpConnReset(socket);
            await socket.ConnectAsync(remoteEndPoint, ct);
            await socket.SendAsync(query.AsMemory(), SocketFlags.None, ct);

            var buffer = new byte[UdpBufferSize];
            var len = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, ct);
            if (len < 12)
                throw new InvalidDataException($"Upstream UDP DNS returned truncated packet ({len} bytes).");

            var response = buffer.AsSpan(0, len).ToArray();
            if (DnsWireMessage.IsTruncated(response))
                return await ExchangeTcpAsync(query, ct);

            return response;
        }

        private async Task<byte[]> ExchangeTcpAsync(byte[] query, CancellationToken ct)
        {
            await using var stream = await OpenStreamAsync(Endpoint.Host, Endpoint.Port, ct);
            return await ExchangeFramedStreamAsync(stream, query, ct);
        }

        private async Task<byte[]> ExchangeTlsAsync(byte[] query, CancellationToken ct)
        {
            await using var inner = await OpenStreamAsync(Endpoint.Host, Endpoint.Port, ct);
            await using var ssl = new SslStream(inner, leaveInnerStreamOpen: false);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = Endpoint.Host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, ct);
            return await ExchangeFramedStreamAsync(ssl, query, ct);
        }

        private async Task<byte[]> ExchangeDohAsync(byte[] query, CancellationToken ct)
        {
            if (_httpClient == null || query.Length < 2)
                throw new InvalidOperationException("DoH client is not initialized.");

            var clientTxId = BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(0, 2));
            var txZeroQuery = DnsWireMessage.CopyWithTransactionId(query, 0);

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint.Normalized)
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                Content = new ByteArrayContent(txZeroQuery)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-message"));

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsByteArrayAsync(ct);
            if (body.Length < 12)
                throw new InvalidDataException($"DoH upstream returned invalid DNS payload ({body.Length} bytes).");

            BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(0, 2), clientTxId);
            return body;
        }

        private SocketsHttpHandler CreateDohHandler() => new()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            ConnectTimeout = UpstreamTimeout,
            AutomaticDecompression = DecompressionMethods.None,
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = async (context, ct) =>
                await OpenStreamAsync(context.DnsEndPoint.Host, (ushort)context.DnsEndPoint.Port, ct)
        };

        private static async Task<byte[]> ExchangeFramedStreamAsync(Stream stream, byte[] query, CancellationToken ct)
        {
            if (query.Length is < 12 or > ushort.MaxValue)
                throw new InvalidDataException($"Invalid DNS query length ({query.Length}).");

            var frame = new byte[2 + query.Length];
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0, 2), (ushort)query.Length);
            query.CopyTo(frame.AsSpan(2));
            await stream.WriteAsync(frame, ct);
            await stream.FlushAsync(ct);

            var lenBuf = new byte[2];
            await stream.ReadExactlyAsync(lenBuf, ct);
            var responseLen = BinaryPrimitives.ReadUInt16BigEndian(lenBuf);
            if (responseLen < 12)
                throw new InvalidDataException($"Upstream TCP/TLS DNS returned invalid length ({responseLen}).");

            var response = new byte[responseLen];
            await stream.ReadExactlyAsync(response, ct);
            return response;
        }

        private async Task<Stream> OpenStreamAsync(string targetHost, ushort targetPort, CancellationToken ct)
        {
            if (_socks5Proxy != null)
                return await ConnectViaSocks5Async(_socks5Proxy, targetHost, targetPort, ct);

            var candidates = await ResolveDirectCandidatesAsync(targetHost, ct);
            Exception? lastError = null;
            foreach (var ip in candidates)
            {
                var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(ip, targetPort), ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception ex) when (ex is SocketException or OperationCanceledException && !ct.IsCancellationRequested)
                {
                    lastError = ex;
                    socket.Dispose();
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }

            throw lastError ?? new SocketException((int)SocketError.HostNotFound);
        }

        private async Task<IPAddress> ResolveSingleDirectIpAsync(CancellationToken ct)
        {
            var candidates = await ResolveDirectCandidatesAsync(Endpoint.Host, ct);
            return candidates[0];
        }

        private async Task<IPAddress[]> ResolveDirectCandidatesAsync(string host, CancellationToken ct)
        {
            var normalizedHost = StripHostBrackets(host);
            if (_bootstrapIps != null && string.Equals(normalizedHost, Endpoint.Host, StringComparison.OrdinalIgnoreCase))
                return [.. _bootstrapIps];

            if (IPAddress.TryParse(normalizedHost, out var literal))
                return [literal];

            var ips = await DnsUtils.LookupAllAsync(normalizedHost, dns: Global.Settings.OutboundDNS, timeout: 3000, ct: ct);
            if (ips.Length == 0)
                throw new SocketException((int)SocketError.HostNotFound);

            return ips;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    internal static string StripHostBrackets(string? host)
    {
        var trimmed = (host ?? string.Empty).Trim();
        return trimmed.Length > 2 && trimmed[0] == '[' && trimmed[^1] == ']'
            ? trimmed[1..^1]
            : trimmed;
    }

    internal static async Task<NetworkStream> ConnectViaSocks5Async(
        SocksServer proxy,
        string targetHost,
        ushort targetPort,
        CancellationToken ct)
    {
        var strippedProxyHost = StripHostBrackets(proxy.Address);
        var proxyHost = strippedProxyHost switch
        {
            "0.0.0.0" => "127.0.0.1",
            "::" => "::1",
            _ => strippedProxyHost
        };
        var proxyIp = IPAddress.TryParse(proxyHost, out var parsedIp)
            ? parsedIp
            : (await DnsUtils.LookupAllAsync(proxyHost, timeout: 3000, dns: Global.Settings.OutboundDNS, ct: ct)).FirstOrDefault()
              ?? throw new SocketException((int)SocketError.HostNotFound);

        var socket = new Socket(proxyIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(proxyIp, proxy.Port), ct);
            var stream = new NetworkStream(socket, ownsSocket: true);

            var hasAuth = proxy.Auth();
            byte[] greeting = hasAuth ? [0x05, 0x02, 0x00, 0x02] : [0x05, 0x01, 0x00];
            await stream.WriteAsync(greeting, ct);

            var methodReply = new byte[2];
            await stream.ReadExactlyAsync(methodReply, ct);
            if (methodReply[0] != 0x05 || methodReply[1] == 0xFF)
                throw new IOException("SOCKS5 proxy rejected authentication methods.");

            if (methodReply[1] == 0x02)
            {
                if (!hasAuth)
                    throw new IOException("SOCKS5 proxy requires username/password authentication.");

                var userBytes = Encoding.UTF8.GetBytes(proxy.Username ?? string.Empty);
                var passBytes = Encoding.UTF8.GetBytes(proxy.Password ?? string.Empty);
                if (userBytes.Length > 255 || passBytes.Length > 255)
                    throw new IOException("SOCKS5 credentials exceed maximum length.");

                var authReq = new byte[3 + userBytes.Length + passBytes.Length];
                authReq[0] = 0x01;
                authReq[1] = (byte)userBytes.Length;
                userBytes.CopyTo(authReq.AsSpan(2));
                authReq[2 + userBytes.Length] = (byte)passBytes.Length;
                passBytes.CopyTo(authReq.AsSpan(3 + userBytes.Length));
                await stream.WriteAsync(authReq, ct);

                var authReply = new byte[2];
                await stream.ReadExactlyAsync(authReply, ct);
                if (authReply[1] != 0x00)
                    throw new IOException("SOCKS5 username/password authentication failed.");
            }
            else if (methodReply[1] != 0x00)
            {
                throw new IOException($"SOCKS5 selected unsupported auth method {methodReply[1]}.");
            }

            var cleanTarget = StripHostBrackets(targetHost);
            byte[] connectReq;
            if (IPAddress.TryParse(cleanTarget, out var targetIp))
            {
                var addrBytes = targetIp.GetAddressBytes();
                var atyp = targetIp.AddressFamily == AddressFamily.InterNetwork ? (byte)0x01 : (byte)0x04;
                connectReq = new byte[4 + addrBytes.Length + 2];
                connectReq[0] = 0x05;
                connectReq[1] = 0x01;
                connectReq[2] = 0x00;
                connectReq[3] = atyp;
                addrBytes.CopyTo(connectReq.AsSpan(4));
                BinaryPrimitives.WriteUInt16BigEndian(connectReq.AsSpan(4 + addrBytes.Length, 2), targetPort);
            }
            else
            {
                var hostBytes = Encoding.ASCII.GetBytes(cleanTarget);
                if (hostBytes.Length is 0 or > 255)
                    throw new IOException($"Invalid target hostname for SOCKS5: {targetHost}");

                connectReq = new byte[5 + hostBytes.Length + 2];
                connectReq[0] = 0x05;
                connectReq[1] = 0x01;
                connectReq[2] = 0x00;
                connectReq[3] = 0x03;
                connectReq[4] = (byte)hostBytes.Length;
                hostBytes.CopyTo(connectReq.AsSpan(5));
                BinaryPrimitives.WriteUInt16BigEndian(connectReq.AsSpan(5 + hostBytes.Length, 2), targetPort);
            }

            await stream.WriteAsync(connectReq, ct);

            var replyHeader = new byte[4];
            await stream.ReadExactlyAsync(replyHeader, ct);
            if (replyHeader[0] != 0x05 || replyHeader[1] != 0x00)
                throw new IOException($"SOCKS5 CONNECT failed with reply code {replyHeader[1]}.");

            var drainLen = replyHeader[3] switch
            {
                0x01 => 4 + 2,
                0x04 => 16 + 2,
                0x03 => -1,
                _ => throw new IOException($"SOCKS5 returned unknown address type {replyHeader[3]}.")
            };

            if (drainLen < 0)
            {
                var lenByte = new byte[1];
                await stream.ReadExactlyAsync(lenByte, ct);
                drainLen = lenByte[0] + 2;
            }

            var drain = new byte[drainLen];
            await stream.ReadExactlyAsync(drain, ct);
            return stream;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}

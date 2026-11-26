using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.Threading;

namespace Netch.Utils;

public sealed class PacHttpServer : IDisposable
{
    private const int MaxRequestHeaderBytes = 8192;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private string _pacTemplate = string.Empty;
    private ushort _mixedPort;
    private bool _allowLan;

    public ushort ListeningPort { get; private set; }

    public string PacPrefix { get; private set; } = string.Empty;

    public string PacUrl { get; private set; } = string.Empty;

    public void Start(string pacTemplate, ushort mixedPort, string bindAddress, ushort preferredPacPort)
    {
        if (_listener != null)
            throw new InvalidOperationException("PAC HTTP server is already running.");

        ArgumentException.ThrowIfNullOrEmpty(pacTemplate);
        if (mixedPort == 0)
            throw new ArgumentOutOfRangeException(nameof(mixedPort));

        _pacTemplate = pacTemplate;
        _mixedPort = mixedPort;

        var ip = string.Equals(bindAddress, "0.0.0.0", StringComparison.Ordinal) ? IPAddress.Any : IPAddress.Loopback;
        _allowLan = ip.Equals(IPAddress.Any);

        TcpListener? listener = null;
        if (preferredPacPort > 0)
        {
            try
            {
                listener = new TcpListener(ip, preferredPacPort);
                listener.Start();
            }
            catch (SocketException ex)
            {
                Log.Warning(ex, "Preferred PAC port {Port} is unavailable, choosing an available port", preferredPacPort);
                listener?.Stop();
                listener = null;
            }
        }

        if (listener == null)
        {
            listener = new TcpListener(ip, 0);
            listener.Start();
        }

        _listener = listener;
        ListeningPort = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        PacPrefix = $"http://127.0.0.1:{ListeningPort}/pac";
        PacUrl = $"{PacPrefix}?t={DateTime.Now:yyyyMMddHHmmssfff}";

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _acceptLoop = Task.Run(() => AcceptLoopAsync(listener, token), CancellationToken.None);

        Log.Information("PAC HTTP server listening on {Address}:{Port} ({Url})", ip, ListeningPort, PacUrl);
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (token.IsCancellationRequested)
                    break;
                continue;
            }

            Task.Run(() => HandleClientAsync(client, token), CancellationToken.None).Forget();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 3000;
                client.SendTimeout = 5000;
                await using var stream = client.GetStream();

                using var readCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                readCts.CancelAfter(TimeSpan.FromSeconds(3));
                var requestText = await ReadHttpRequestHeadersAsync(stream, readCts.Token);
                if (string.IsNullOrEmpty(requestText))
                    return;

                var lines = requestText.Split("\r\n", StringSplitOptions.None);
                var requestLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (requestLine.Length < 2)
                {
                    await WriteSimpleResponseAsync(stream, "400 Bad Request", "text/plain; charset=utf-8", "Bad Request", includeBody: true, token);
                    return;
                }

                var method = requestLine[0].ToUpperInvariant();
                if (method is not ("GET" or "HEAD"))
                {
                    await WriteSimpleResponseAsync(stream, "405 Method Not Allowed", "text/plain; charset=utf-8", "Method Not Allowed", includeBody: false, token);
                    return;
                }

                var rawTarget = requestLine[1];
                var path = Uri.TryCreate(rawTarget, UriKind.Absolute, out var reqUri)
                    ? reqUri.AbsolutePath
                    : rawTarget.Split('?', 2)[0];
                if (!IsPacRequestPath(path))
                {
                    await WriteSimpleResponseAsync(stream, "404 Not Found", "text/plain; charset=utf-8", "Not Found", includeBody: method == "GET", token);
                    return;
                }

                var proxyHost = ResolveProxyHost(lines);
                var proxyEndpoint = proxyHost.Contains(':') && !proxyHost.StartsWith('[')
                    ? $"[{proxyHost}]:{_mixedPort}"
                    : $"{proxyHost}:{_mixedPort}";
                var script = _pacTemplate.Replace("__PROXY__", $"PROXY {proxyEndpoint};", StringComparison.Ordinal);
                await WriteSimpleResponseAsync(
                    stream,
                    "200 OK",
                    "application/x-ns-proxy-autoconfig; charset=utf-8",
                    script,
                    includeBody: method == "GET",
                    token);
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException)
            {
                // Ignore transient client disconnects or shutdown cancellations.
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "PAC HTTP client handling failed");
            }
        }
    }

    private static bool IsPacRequestPath(string path) =>
        path.Equals("/", StringComparison.Ordinal) ||
        path.Equals("/pac", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/pac/", StringComparison.OrdinalIgnoreCase);

    private string ResolveProxyHost(string[] headerLines)
    {
        if (!_allowLan)
            return "127.0.0.1";

        foreach (var line in headerLines.Skip(1))
        {
            if (!line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                continue;

            var hostHeader = line[5..].Trim();
            if (hostHeader.Length == 0)
                break;

            var host = ExtractHostWithoutPort(hostHeader);
            if (IsSafeHostLiteral(host) && host is not ("0.0.0.0" or "::" or "[::]"))
                return host;
            break;
        }

        return "127.0.0.1";
    }

    private static string ExtractHostWithoutPort(string hostHeader)
    {
        if (hostHeader.StartsWith('['))
        {
            var close = hostHeader.IndexOf(']');
            return close > 1 ? hostHeader[..(close + 1)] : string.Empty;
        }

        var colon = hostHeader.IndexOf(':');
        return colon > 0 && hostHeader.LastIndexOf(':') == colon ? hostHeader[..colon] : hostHeader;
    }

    private static bool IsSafeHostLiteral(string host)
    {
        if (host.Length == 0 || host.Length > 253)
            return false;

        foreach (var c in host)
        {
            if (!(char.IsLetterOrDigit(c) || c is '.' or '-' or ':' or '[' or ']'))
                return false;
        }

        return true;
    }

    private static async Task<string> ReadHttpRequestHeadersAsync(NetworkStream stream, CancellationToken token)
    {
        var buffer = new byte[MaxRequestHeaderBytes];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), token);
            if (read == 0)
                break;

            total += read;
            var span = buffer.AsSpan(0, total);
            if (span.IndexOf("\r\n\r\n"u8) >= 0)
                return Encoding.ASCII.GetString(span);
        }

        return total > 0 ? Encoding.ASCII.GetString(buffer, 0, total) : string.Empty;
    }

    private static async Task WriteSimpleResponseAsync(
        NetworkStream stream,
        string status,
        string contentType,
        string body,
        bool includeBody,
        CancellationToken token)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var headers =
            $"HTTP/1.1 {status}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
            "Pragma: no-cache\r\n" +
            "Expires: 0\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n";

        var headerBytes = Encoding.ASCII.GetBytes(headers);
        await stream.WriteAsync(headerBytes, token);
        if (includeBody && bodyBytes.Length > 0)
            await stream.WriteAsync(bodyBytes, token);
        await stream.FlushAsync(token);
    }

    public async Task StopAsync()
    {
        var listener = Interlocked.Exchange(ref _listener, null);
        var cts = Interlocked.Exchange(ref _cts, null);
        var acceptLoop = Interlocked.Exchange(ref _acceptLoop, null);

        if (cts != null)
        {
            try { await cts.CancelAsync(); }
            catch (ObjectDisposedException) { }
        }

        if (listener != null)
        {
            try { listener.Stop(); }
            catch (Exception) { }
        }

        if (acceptLoop != null)
        {
            try
            {
                await acceptLoop.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception)
            {
                // Best-effort wait on shutdown.
            }
        }

        cts?.Dispose();
        ListeningPort = 0;
        PacPrefix = string.Empty;
        PacUrl = string.Empty;
    }

    public void Dispose()
    {
        var listener = Interlocked.Exchange(ref _listener, null);
        var cts = Interlocked.Exchange(ref _cts, null);
        var acceptLoop = Interlocked.Exchange(ref _acceptLoop, null);
        acceptLoop?.Forget();

        try { cts?.Cancel(); }
        catch (ObjectDisposedException) { }

        try { listener?.Stop(); }
        catch (Exception) { }

        cts?.Dispose();
    }
}

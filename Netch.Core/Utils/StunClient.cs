using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Netch.Utils;

public enum BindingTestResult
{
    Unknown,
    UnsupportedServer,
    Success,
    Fail
}

public enum MappingBehavior
{
    Unknown,
    UnsupportedServer,
    Direct,
    EndpointIndependent,
    AddressDependent,
    AddressAndPortDependent,
    Fail
}

public enum FilteringBehavior
{
    Unknown,
    UnsupportedServer,
    EndpointIndependent,
    AddressDependent,
    AddressAndPortDependent,
    None
}

public sealed class StunServer
{
    public const ushort DefaultPort = 3478;

    public string Hostname { get; }

    public ushort Port { get; }

    public StunServer(string hostname, ushort port = DefaultPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        Hostname = hostname;
        Port = port;
    }

    public static bool TryParse(string? s, [NotNullWhen(true)] out StunServer? result, ushort defaultPort = DefaultPort)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        var trimmed = s.Trim();
        if (trimmed.Contains("://", StringComparison.Ordinal) ||
            trimmed.Contains('/') ||
            trimmed.Any(char.IsWhiteSpace))
        {
            return false;
        }

        string host;
        var port = defaultPort;

        if (trimmed.StartsWith('['))
        {
            var closeIdx = trimmed.IndexOf(']');
            if (closeIdx <= 1)
                return false;

            var inner = trimmed[1..closeIdx];
            if (!IPAddress.TryParse(inner, out var v6) || v6.AddressFamily != AddressFamily.InterNetworkV6)
                return false;

            host = v6.ToString();
            if (closeIdx < trimmed.Length - 1)
            {
                if (trimmed[closeIdx + 1] != ':' ||
                    !ushort.TryParse(trimmed[(closeIdx + 2)..], out port) ||
                    port == 0)
                {
                    return false;
                }
            }
        }
        else if (IPAddress.TryParse(trimmed, out var bareIp) && bareIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
            host = bareIp.ToString();
        }
        else
        {
            var firstColon = trimmed.IndexOf(':');
            var lastColon = trimmed.LastIndexOf(':');
            if (firstColon != lastColon)
                return false;

            if (firstColon >= 0)
            {
                host = trimmed[..firstColon];
                if (host.Length == 0 ||
                    !ushort.TryParse(trimmed[(firstColon + 1)..], out port) ||
                    port == 0)
                {
                    return false;
                }
            }
            else
            {
                host = trimmed;
            }
        }

        if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            return false;

        result = new StunServer(host, port);
        return true;
    }

    public override string ToString()
    {
        if (IPAddress.TryParse(Hostname, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
            return Port == DefaultPort ? Hostname : $"[{Hostname}]:{Port}";

        return Port == DefaultPort ? Hostname : $"{Hostname}:{Port}";
    }
}

public sealed class StunResult5389
{
    public IPEndPoint? PublicEndPoint { get; set; }

    public IPEndPoint? LocalEndPoint { get; set; }

    public IPEndPoint? OtherEndPoint { get; set; }

    public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;

    public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;

    public FilteringBehavior FilteringBehavior { get; set; } = FilteringBehavior.Unknown;
}

internal static class StunProtocol
{
    internal const uint MagicCookie = 0x2112A442;
    private const ushort BindingRequestType = 0x0001;
    private const ushort BindingSuccessResponseType = 0x0101;
    private const ushort AttrMappedAddress = 0x0001;
    private const ushort AttrChangeRequest = 0x0003;
    private const ushort AttrSourceAddress = 0x0004;
    private const ushort AttrChangedAddress = 0x0005;
    private const ushort AttrXorMappedAddress = 0x0020;
    private const ushort AttrResponseOrigin = 0x802B;
    private const ushort AttrOtherAddress = 0x802C;

    internal static byte[] BuildBindingRequest(ReadOnlySpan<byte> transactionId, bool changeIp = false, bool changePort = false)
    {
        if (transactionId.Length != 12)
            throw new ArgumentException("Transaction ID must be 12 bytes.", nameof(transactionId));

        var hasChangeRequest = changeIp || changePort;
        var packet = new byte[hasChangeRequest ? 28 : 20];
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(0, 2), BindingRequestType);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2, 2), (ushort)(hasChangeRequest ? 8 : 0));
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(4, 4), MagicCookie);
        transactionId.CopyTo(packet.AsSpan(8, 12));

        if (hasChangeRequest)
        {
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(20, 2), AttrChangeRequest);
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(22, 2), 4);
            uint flags = 0;
            if (changeIp)
                flags |= 0x04;
            if (changePort)
                flags |= 0x02;
            BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(24, 4), flags);
        }

        return packet;
    }

    internal static bool TryParseBindingResponse(
        ReadOnlySpan<byte> packet,
        ReadOnlySpan<byte> expectedTxId,
        out IPEndPoint? mapped,
        out IPEndPoint? other,
        out IPEndPoint? responseOrigin)
    {
        mapped = null;
        other = null;
        responseOrigin = null;

        if (expectedTxId.Length != 12 || packet.Length < 20)
            return false;

        if (BinaryPrimitives.ReadUInt16BigEndian(packet[0..2]) != BindingSuccessResponseType)
            return false;

        if (BinaryPrimitives.ReadUInt32BigEndian(packet[4..8]) != MagicCookie)
            return false;

        if (!packet[8..20].SequenceEqual(expectedTxId))
            return false;

        var msgLen = BinaryPrimitives.ReadUInt16BigEndian(packet[2..4]);
        var end = 20 + msgLen;
        if (end > packet.Length)
            return false;

        var xorMask = packet[4..20];
        IPEndPoint? xorMapped = null;
        IPEndPoint? plainMapped = null;
        IPEndPoint? otherAddr = null;
        IPEndPoint? changedAddr = null;
        IPEndPoint? respOriginAddr = null;
        IPEndPoint? sourceAddr = null;

        var offset = 20;
        while (offset + 4 <= end)
        {
            var attrType = BinaryPrimitives.ReadUInt16BigEndian(packet[offset..(offset + 2)]);
            var attrLen = BinaryPrimitives.ReadUInt16BigEndian(packet[(offset + 2)..(offset + 4)]);
            var valStart = offset + 4;
            if (valStart + attrLen > end)
                return false;

            var attrVal = packet.Slice(valStart, attrLen);
            switch (attrType)
            {
                case AttrXorMappedAddress:
                    if (TryParseStunAddress(attrVal, xorMask, xor: true, out var epXor))
                        xorMapped = epXor;
                    break;
                case AttrMappedAddress:
                    if (TryParseStunAddress(attrVal, xorMask, xor: false, out var epMapped))
                        plainMapped = epMapped;
                    break;
                case AttrOtherAddress:
                    if (TryParseStunAddress(attrVal, xorMask, xor: false, out var epOther))
                        otherAddr = epOther;
                    break;
                case AttrChangedAddress:
                    if (TryParseStunAddress(attrVal, xorMask, xor: false, out var epChanged))
                        changedAddr = epChanged;
                    break;
                case AttrResponseOrigin:
                    if (TryParseStunAddress(attrVal, xorMask, xor: false, out var epRespOrigin))
                        respOriginAddr = epRespOrigin;
                    break;
                case AttrSourceAddress:
                    if (TryParseStunAddress(attrVal, xorMask, xor: false, out var epSource))
                        sourceAddr = epSource;
                    break;
            }

            offset = valStart + ((attrLen + 3) & ~3);
        }

        mapped = NormalizeEndPoint(xorMapped ?? plainMapped);
        other = NormalizeEndPoint(otherAddr ?? changedAddr);
        responseOrigin = NormalizeEndPoint(respOriginAddr ?? sourceAddr);
        return true;
    }

    private static bool TryParseStunAddress(
        ReadOnlySpan<byte> val,
        ReadOnlySpan<byte> xorMask,
        bool xor,
        out IPEndPoint? endPoint)
    {
        endPoint = null;
        if (val.Length < 8)
            return false;

        var family = val[1];
        var rawPort = BinaryPrimitives.ReadUInt16BigEndian(val[2..4]);
        var port = xor ? (ushort)(rawPort ^ (ushort)(MagicCookie >> 16)) : rawPort;

        if (family == 0x01)
        {
            Span<byte> addrBytes = stackalloc byte[4];
            val[4..8].CopyTo(addrBytes);
            if (xor)
            {
                for (var i = 0; i < 4; i++)
                    addrBytes[i] ^= xorMask[i];
            }

            endPoint = new IPEndPoint(new IPAddress(addrBytes), port);
            return true;
        }

        if (family == 0x02 && val.Length >= 20)
        {
            Span<byte> addrBytes = stackalloc byte[16];
            val[4..20].CopyTo(addrBytes);
            if (xor)
            {
                for (var i = 0; i < 16; i++)
                    addrBytes[i] ^= xorMask[i];
            }

            endPoint = new IPEndPoint(new IPAddress(addrBytes), port);
            return true;
        }

        return false;
    }

    internal static IPEndPoint? NormalizeEndPoint(IPEndPoint? ep)
    {
        if (ep == null)
            return null;

        return ep.Address.IsIPv4MappedToIPv6
            ? new IPEndPoint(ep.Address.MapToIPv4(), ep.Port)
            : ep;
    }

    internal static byte[] WrapSocks5UdpPacket(IPEndPoint target, ReadOnlySpan<byte> payload)
    {
        var address = target.Address.IsIPv4MappedToIPv6 ? target.Address.MapToIPv4() : target.Address;
        var isV6 = address.AddressFamily == AddressFamily.InterNetworkV6;
        var addrLen = isV6 ? 16 : 4;
        var packet = new byte[4 + addrLen + 2 + payload.Length];

        packet[0] = 0x00;
        packet[1] = 0x00;
        packet[2] = 0x00;
        packet[3] = isV6 ? (byte)0x04 : (byte)0x01;

        address.TryWriteBytes(packet.AsSpan(4, addrLen), out _);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4 + addrLen, 2), (ushort)target.Port);
        payload.CopyTo(packet.AsSpan(4 + addrLen + 2));
        return packet;
    }

    internal static bool TryUnwrapSocks5UdpPacket(
        ReadOnlySpan<byte> datagram,
        out IPEndPoint? source,
        out ReadOnlySpan<byte> payload)
    {
        source = null;
        payload = default;

        if (datagram.Length < 4 || datagram[0] != 0x00 || datagram[1] != 0x00 || datagram[2] != 0x00)
            return false;

        switch (datagram[3])
        {
            case 0x01:
                if (datagram.Length < 10)
                    return false;
                source = new IPEndPoint(new IPAddress(datagram[4..8]), BinaryPrimitives.ReadUInt16BigEndian(datagram[8..10]));
                payload = datagram[10..];
                return true;

            case 0x04:
                if (datagram.Length < 22)
                    return false;
                source = NormalizeEndPoint(new IPEndPoint(new IPAddress(datagram[4..20]), BinaryPrimitives.ReadUInt16BigEndian(datagram[20..22])));
                payload = datagram[22..];
                return true;

            case 0x03:
                if (datagram.Length < 5)
                    return false;
                var domainLen = datagram[4];
                var headerLen = 5 + domainLen + 2;
                if (datagram.Length < headerLen)
                    return false;
                payload = datagram[headerLen..];
                return true;

            default:
                return false;
        }
    }
}

internal sealed class StunClient5389Udp(
    IPEndPoint serverEndPoint,
    IPAddress socksAddress,
    ushort socksPort,
    string? username,
    string? password) : IDisposable
{
    private static readonly int[] RetransmitDelaysMs = [500, 500, 1000];

    private readonly IPEndPoint _serverEndPoint = StunProtocol.NormalizeEndPoint(serverEndPoint) ?? serverEndPoint;
    private TcpClient? _tcpClient;
    private UdpClient? _udpClient;
    private IPEndPoint? _relayEndPoint;

    public StunResult5389 State { get; } = new();

    public async Task ConnectProxyAsync(CancellationToken ct)
    {
        _tcpClient?.Dispose();
        _udpClient?.Dispose();

        var controlAddress = socksAddress.IsIPv4MappedToIPv6 ? socksAddress.MapToIPv4() : socksAddress;
        if (controlAddress.Equals(IPAddress.Any))
            controlAddress = IPAddress.Loopback;
        else if (controlAddress.Equals(IPAddress.IPv6Any))
            controlAddress = IPAddress.IPv6Loopback;

        var tcp = new TcpClient(controlAddress.AddressFamily);
        _tcpClient = tcp;

        await tcp.ConnectAsync(controlAddress, socksPort, ct).ConfigureAwait(false);
        var stream = tcp.GetStream();

        var hasAuth = !string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password);
        byte[] greeting = hasAuth ? [0x05, 0x02, 0x00, 0x02] : [0x05, 0x01, 0x00];
        await stream.WriteAsync(greeting, ct).ConfigureAwait(false);

        var methodReply = new byte[2];
        await stream.ReadExactlyAsync(methodReply, ct).ConfigureAwait(false);
        if (methodReply[0] != 0x05)
            throw new InvalidDataException("Invalid SOCKS5 greeting response.");

        if (methodReply[1] == 0x02)
        {
            if (!hasAuth)
                throw new InvalidOperationException("SOCKS5 server requires authentication.");

            var userBytes = Encoding.UTF8.GetBytes(username ?? string.Empty);
            var passBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            if (userBytes.Length > byte.MaxValue || passBytes.Length > byte.MaxValue)
                throw new InvalidOperationException("SOCKS5 credentials exceed maximum length.");

            var authReq = new byte[3 + userBytes.Length + passBytes.Length];
            authReq[0] = 0x01;
            authReq[1] = (byte)userBytes.Length;
            userBytes.CopyTo(authReq, 2);
            authReq[2 + userBytes.Length] = (byte)passBytes.Length;
            passBytes.CopyTo(authReq, 3 + userBytes.Length);
            await stream.WriteAsync(authReq, ct).ConfigureAwait(false);

            var authReply = new byte[2];
            await stream.ReadExactlyAsync(authReply, ct).ConfigureAwait(false);
            if (authReply[1] != 0x00)
                throw new InvalidOperationException("SOCKS5 username/password authentication failed.");
        }
        else if (methodReply[1] != 0x00)
        {
            throw new InvalidOperationException($"Unsupported SOCKS5 authentication method: {methodReply[1]}.");
        }

        var isV6 = controlAddress.AddressFamily == AddressFamily.InterNetworkV6;
        var assocReq = new byte[4 + (isV6 ? 16 : 4) + 2];
        assocReq[0] = 0x05;
        assocReq[1] = 0x03; // UDP ASSOCIATE
        assocReq[2] = 0x00;
        assocReq[3] = isV6 ? (byte)0x04 : (byte)0x01;
        await stream.WriteAsync(assocReq, ct).ConfigureAwait(false);

        var header = new byte[4];
        await stream.ReadExactlyAsync(header, ct).ConfigureAwait(false);
        if (header[0] != 0x05 || header[1] != 0x00)
            throw new InvalidOperationException($"SOCKS5 UDP ASSOCIATE failed with reply code {header[1]}.");

        IPAddress bndAddress;
        switch (header[3])
        {
            case 0x01:
                var v4 = new byte[4];
                await stream.ReadExactlyAsync(v4, ct).ConfigureAwait(false);
                bndAddress = new IPAddress(v4);
                break;
            case 0x04:
                var v6 = new byte[16];
                await stream.ReadExactlyAsync(v6, ct).ConfigureAwait(false);
                bndAddress = new IPAddress(v6);
                break;
            case 0x03:
                var lenBuf = new byte[1];
                await stream.ReadExactlyAsync(lenBuf, ct).ConfigureAwait(false);
                var hostBuf = new byte[lenBuf[0]];
                await stream.ReadExactlyAsync(hostBuf, ct).ConfigureAwait(false);
                var host = Encoding.ASCII.GetString(hostBuf);
                var resolved = await DnsUtils.LookupAsync(host).WaitAsync(ct).ConfigureAwait(false);
                bndAddress = resolved ?? controlAddress;
                break;
            default:
                throw new InvalidDataException($"Unsupported SOCKS5 address type {header[3]}.");
        }

        var portBuf = new byte[2];
        await stream.ReadExactlyAsync(portBuf, ct).ConfigureAwait(false);
        var bndPort = BinaryPrimitives.ReadUInt16BigEndian(portBuf);
        if (bndPort == 0)
            throw new InvalidDataException("SOCKS5 UDP ASSOCIATE returned port 0.");

        if (bndAddress.IsIPv4MappedToIPv6)
            bndAddress = bndAddress.MapToIPv4();
        if (bndAddress.Equals(IPAddress.Any) || bndAddress.Equals(IPAddress.IPv6Any))
            bndAddress = controlAddress;

        _relayEndPoint = new IPEndPoint(bndAddress, bndPort);
        var bindAddress = _relayEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
        _udpClient = new UdpClient(new IPEndPoint(bindAddress, 0));
        if (OperatingSystem.IsWindows())
        {
            try
            {
                const int sioUdpConnReset = -1744830452;
                _udpClient.Client.IOControl((IOControlCode)sioUdpConnReset, [0, 0, 0, 0], null);
            }
            catch (SocketException)
            {
            }
            catch (PlatformNotSupportedException)
            {
            }
        }

        State.LocalEndPoint = _udpClient.Client.LocalEndPoint as IPEndPoint ?? _relayEndPoint;
    }

    public async Task QueryAsync(CancellationToken ct)
    {
        if (_udpClient == null || _relayEndPoint == null)
            throw new InvalidOperationException("ConnectProxyAsync must succeed before QueryAsync.");

        var r1 = await ProbeAsync(_serverEndPoint, changeIp: false, changePort: false, ct).ConfigureAwait(false);
        if (!r1.Responded)
        {
            State.BindingTestResult = BindingTestResult.Fail;
            return;
        }

        if (r1.Mapped == null)
        {
            State.BindingTestResult = BindingTestResult.UnsupportedServer;
            return;
        }

        State.BindingTestResult = BindingTestResult.Success;
        State.PublicEndPoint = r1.Mapped;
        State.OtherEndPoint = r1.Other;

        // Mapping Behavior (RFC 5780 Section 4.3)
        if (r1.Other == null || Equals(r1.Other.Address, _serverEndPoint.Address) || r1.Other.Port == _serverEndPoint.Port)
        {
            State.MappingBehavior = MappingBehavior.UnsupportedServer;
        }
        else if (Equals(r1.Mapped, State.LocalEndPoint))
        {
            State.MappingBehavior = MappingBehavior.Direct;
        }
        else
        {
            var r2 = await ProbeAsync(new IPEndPoint(r1.Other.Address, _serverEndPoint.Port), changeIp: false, changePort: false, ct)
                .ConfigureAwait(false);
            if (!r2.Responded || r2.Mapped == null)
            {
                State.MappingBehavior = MappingBehavior.Fail;
            }
            else if (Equals(r2.Mapped, r1.Mapped))
            {
                State.MappingBehavior = MappingBehavior.EndpointIndependent;
            }
            else
            {
                var r3 = await ProbeAsync(r1.Other, changeIp: false, changePort: false, ct).ConfigureAwait(false);
                if (!r3.Responded || r3.Mapped == null)
                    State.MappingBehavior = MappingBehavior.Fail;
                else if (Equals(r3.Mapped, r2.Mapped))
                    State.MappingBehavior = MappingBehavior.AddressDependent;
                else
                    State.MappingBehavior = MappingBehavior.AddressAndPortDependent;
            }
        }

        // Filtering Behavior (RFC 5780 Section 4.4)
        if (r1.Other == null || Equals(r1.Other.Address, _serverEndPoint.Address) || r1.Other.Port == _serverEndPoint.Port)
        {
            State.FilteringBehavior = FilteringBehavior.UnsupportedServer;
        }
        else
        {
            var f2 = await ProbeAsync(_serverEndPoint, changeIp: true, changePort: true, ct).ConfigureAwait(false);
            if (f2.Responded)
            {
                State.FilteringBehavior = FilteringBehavior.EndpointIndependent;
            }
            else
            {
                var f3 = await ProbeAsync(_serverEndPoint, changeIp: false, changePort: true, ct).ConfigureAwait(false);
                State.FilteringBehavior = f3.Responded
                    ? FilteringBehavior.AddressDependent
                    : FilteringBehavior.AddressAndPortDependent;
            }
        }
    }

    private async Task<(bool Responded, IPEndPoint? Mapped, IPEndPoint? Other)> ProbeAsync(
        IPEndPoint target,
        bool changeIp,
        bool changePort,
        CancellationToken ct)
    {
        if (_udpClient == null || _relayEndPoint == null)
            return (false, null, null);

        var txId = new byte[12];
        RandomNumberGenerator.Fill(txId);
        var stunReq = StunProtocol.BuildBindingRequest(txId, changeIp, changePort);
        var wrapped = StunProtocol.WrapSocks5UdpPacket(target, stunReq);

        foreach (var delayMs in RetransmitDelaysMs)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await _udpClient.SendAsync(wrapped, _relayEndPoint, ct).ConfigureAwait(false);
            }
            catch (SocketException) when (!ct.IsCancellationRequested)
            {
                return (false, null, null);
            }

            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            attemptCts.CancelAfter(delayMs);

            while (true)
            {
                UdpReceiveResult received;
                try
                {
                    received = await _udpClient.ReceiveAsync(attemptCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    break;
                }
                catch (SocketException) when (!ct.IsCancellationRequested)
                {
                    return (false, null, null);
                }

                if (StunProtocol.TryUnwrapSocks5UdpPacket(received.Buffer, out var udpSource, out var payload) &&
                    StunProtocol.TryParseBindingResponse(payload, txId, out var mapped, out var other, out var responseOrigin))
                {
                    if (changeIp || changePort)
                    {
                        var origin = responseOrigin ?? udpSource;
                        if (changeIp && origin != null && Equals(origin.Address, target.Address))
                            continue;
                        if (changePort && origin != null && origin.Port == target.Port)
                            continue;
                    }

                    return (true, mapped, other);
                }
            }
        }

        return (false, null, null);
    }

    public void Dispose()
    {
        _udpClient?.Dispose();
        _tcpClient?.Dispose();
    }
}

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class TrustTunnelUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 11;
    public string TypeName { get; } = nameof(EConfigType.TrustTunnel);
    public string FullName { get; } = "TrustTunnel";
    public string ShortName { get; } = "TT";
    public string[] UriScheme { get; } = ["trusttunnel", "tt"];
    public Type ServerType { get; } = typeof(TrustTunnelServer);

    public string GetShareLink(Server s)
    {
        if (s is not TrustTunnelServer server)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(server.Cert) || !string.IsNullOrWhiteSpace(server.EchConfigList))
            return string.Empty;

        var extra = server.ProtoExtra;
        var query = TlsQuery(server);
        if (!string.IsNullOrWhiteSpace(extra.TrustTunnelNameCertVerify))
            query["name-cert-verify"] = Utils.Utils.UrlEncode(extra.TrustTunnelNameCertVerify.Trim());
        if (extra.TrustTunnelHealthCheck is { } healthCheck)
            query["health-check"] = healthCheck ? "1" : "0";
        if (extra.TrustTunnelQuic == true)
        {
            query["quic"] = "1";
            if (!string.IsNullOrWhiteSpace(extra.CongestionControl))
                query["congestion-controller"] = Utils.Utils.UrlEncode(extra.CongestionControl.Trim());
            if (!string.IsNullOrWhiteSpace(extra.BbrProfile))
                query["bbr-profile"] = Utils.Utils.UrlEncode(extra.BbrProfile.Trim());
        }
        if (extra.TrustTunnelMaxConnections is > 0)
            query["max-connections"] = extra.TrustTunnelMaxConnections.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.TrustTunnelMinStreams is > 0)
            query["min-streams"] = extra.TrustTunnelMinStreams.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.TrustTunnelMaxStreams is > 0)
            query["max-streams"] = extra.TrustTunnelMaxStreams.Value.ToString(CultureInfo.InvariantCulture);

        var userInfo = server.Username.IsNotEmpty() || server.Password.IsNotEmpty()
            ? $"{Utils.Utils.UrlEncode(server.Username ?? string.Empty)}:{Utils.Utils.UrlEncode(server.Password ?? string.Empty)}"
            : string.Empty;

        return ToShareUri(EConfigType.TrustTunnel, server, userInfo, query);
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var trimmed = text.Trim();
        var schemeSep = trimmed.IndexOf("://", StringComparison.Ordinal);
        if (schemeSep > 0)
        {
            var rest = trimmed[(schemeSep + 3)..];
            if (rest.StartsWith('?'))
            {
                var queryAndFrag = rest[1..];
                var hashIdx = queryAndFrag.IndexOf('#');
                var payload = hashIdx >= 0 ? queryAndFrag[..hashIdx] : queryAndFrag;
                var remark = hashIdx >= 0 ? Utils.Utils.UrlDecode(queryAndFrag[(hashIdx + 1)..]) : string.Empty;
                if (!payload.TrimEnd('=').Contains('=', StringComparison.Ordinal))
                    return ParseDeepLinkTlv(payload, remark);
            }
        }

        var url = Utils.Utils.TryUri(trimmed);
        if (url == null || url.IdnHost.IsNullOrEmpty())
            return [];

        var server = new TrustTunnelServer
        {
            Address = url.IdnHost,
            Port = url.Port < 0 ? 443 : url.Port,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            Username = string.Empty,
            Password = string.Empty
        };

        if (url.UserInfo.IsNotEmpty())
        {
            var (user, pass, _) = SplitUserInfo(url.UserInfo);
            server.Username = user;
            server.Password = pass;
        }

        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQuery(query, server);

        var extra = server.ProtoExtra;
        extra.TrustTunnelNameCertVerify = GetFirstQueryValue(query, "name-cert-verify", "name_cert_verify");
        extra.TrustTunnelHealthCheck = ParseBoolFlag(GetFirstQueryValue(query, "health-check", "health_check"));
        extra.TrustTunnelQuic = ParseBoolFlag(GetQueryValue(query, "quic"));
        var cc = GetFirstQueryValue(query, "congestion-controller", "congestion_controller", "congestion_control", "congestion-control");
        extra.CongestionControl = cc.IsNotEmpty() ? cc.Trim().ToLowerInvariant() : string.Empty;
        var bbr = GetFirstQueryValue(query, "bbr-profile", "bbr_profile");
        extra.BbrProfile = bbr.IsNotEmpty() ? bbr.Trim().ToLowerInvariant() : string.Empty;
        if (!TryParseOptionalInt(GetFirstQueryValue(query, "max-connections", "max_connections"), out var maxConnections) ||
            !TryParseOptionalInt(GetFirstQueryValue(query, "min-streams", "min_streams"), out var minStreams) ||
            !TryParseOptionalInt(GetFirstQueryValue(query, "max-streams", "max_streams"), out var maxStreams))
        {
            return [];
        }
        extra.TrustTunnelMaxConnections = maxConnections;
        extra.TrustTunnelMinStreams = minStreams;
        extra.TrustTunnelMaxStreams = maxStreams;
        return [server];
    }

    public bool CheckServer(Server s) => s is TrustTunnelServer && ServerValidator.Validate(s) == null;

    private static IEnumerable<Server> ParseDeepLinkTlv(string payload, string remark)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return [];

        byte[] bytes;
        try
        {
            var clean = payload.Trim().Replace('-', '+').Replace('_', '/');
            clean = clean.PadRight(clean.Length + (4 - clean.Length % 4) % 4, '=');
            bytes = Convert.FromBase64String(clean);
        }
        catch (FormatException)
        {
            return [];
        }

        string? hostname = null;
        string? firstAddress = null;
        var hasAddressTag = false;
        var firstPort = 0;
        string? customSni = null;
        var username = string.Empty;
        var password = string.Empty;
        var certPem = string.Empty;
        bool? quic = null;
        var parsedName = string.Empty;

        var offset = 0;
        while (offset < bytes.Length)
        {
            if (!TryReadQuicVarint(bytes, ref offset, out var tag))
                return [];
            if (!TryReadQuicVarint(bytes, ref offset, out var length))
                return [];
            if (length > (ulong)(bytes.Length - offset))
                return [];

            var len = (int)length;
            var value = bytes.AsSpan(offset, len);
            offset += len;

            switch (tag)
            {
                case 0x00:
                    // Deep-link format version; skip if varint is valid or raw bytes already consumed.
                    break;
                case 0x01:
                    hostname = Encoding.UTF8.GetString(value).Trim();
                    break;
                case 0x02:
                    hasAddressTag = true;
                    if (firstAddress == null && TryParseEndpoint(Encoding.UTF8.GetString(value).Trim(), out var parsedHost, out var parsedPort))
                    {
                        firstAddress = parsedHost;
                        firstPort = parsedPort;
                    }
                    break;
                case 0x03:
                    customSni = Encoding.UTF8.GetString(value).Trim();
                    break;
                case 0x05:
                    username = Encoding.UTF8.GetString(value);
                    break;
                case 0x06:
                    password = Encoding.UTF8.GetString(value);
                    break;
                case 0x08:
                    if (value.Length > 0)
                    {
                        var text = Encoding.UTF8.GetString(value);
                        certPem = text.TrimStart().StartsWith("-----BEGIN ", StringComparison.Ordinal)
                            ? text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
                            : new string(PemEncoding.Write("CERTIFICATE", value)) + "\n";
                    }
                    break;
                case 0x09:
                    if (value.Length >= 1)
                    {
                        if (value[0] == 0x02)
                            quic = true;
                        else if (value[0] == 0x01)
                            quic = false;
                    }
                    break;
                case 0x0C:
                    if (parsedName.Length == 0)
                        parsedName = Encoding.UTF8.GetString(value).Trim();
                    break;
            }
        }

        string address;
        int port;
        if (firstAddress != null)
        {
            address = firstAddress;
            port = firstPort > 0 ? firstPort : 443;
        }
        else if (hasAddressTag)
        {
            return [];
        }
        else if (!string.IsNullOrWhiteSpace(hostname))
        {
            var trimmedHostname = hostname.Trim();
            if (Uri.CheckHostName(trimmedHostname) == UriHostNameType.Unknown)
                return [];
            address = trimmedHostname;
            port = 443;
        }
        else
        {
            return [];
        }

        var server = new TrustTunnelServer
        {
            Address = address,
            Port = port,
            Username = username,
            Password = password,
            Remarks = string.IsNullOrWhiteSpace(remark) ? parsedName : remark,
            Sni = !string.IsNullOrWhiteSpace(customSni) ? customSni : (hostname ?? string.Empty),
            Cert = certPem,
            StreamSecurity = Constants.StreamSecurity,
            ProtoExtra = { TrustTunnelQuic = quic }
        };

        return [server];
    }

    private static bool TryReadQuicVarint(ReadOnlySpan<byte> data, ref int offset, out ulong value)
    {
        value = 0;
        if (offset >= data.Length)
            return false;

        var first = data[offset];
        var length = 1 << (first >> 6);
        if (offset + length > data.Length)
            return false;

        ulong result = (ulong)(first & 0x3F);
        for (var i = 1; i < length; i++)
            result = (result << 8) | data[offset + i];

        offset += length;
        value = result;
        return true;
    }

    private static bool TryParseEndpoint(string raw, out string host, out int port)
    {
        host = string.Empty;
        port = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        raw = raw.Trim();
        if (raw.StartsWith('['))
        {
            var closeBracket = raw.IndexOf(']');
            if (closeBracket <= 1)
                return false;
            var candidateHost = raw[1..closeBracket].Trim();
            if (!IPAddress.TryParse(candidateHost, out var bracketIp) ||
                bracketIp.AddressFamily != AddressFamily.InterNetworkV6)
                return false;
            host = bracketIp.ToString();
            if (closeBracket + 1 == raw.Length)
                return true;
            if (raw[closeBracket + 1] != ':')
                return false;
            return int.TryParse(raw[(closeBracket + 2)..], NumberStyles.None, CultureInfo.InvariantCulture, out port)
                && ServerValidator.IsValidPort(port);
        }

        if (IPAddress.TryParse(raw, out var bareIp) &&
            (bareIp.AddressFamily == AddressFamily.InterNetworkV6 || raw.Count(c => c == '.') == 3))
        {
            host = bareIp.ToString();
            return true;
        }

        if (IPEndPoint.TryParse(raw, out var ipEndpoint))
        {
            host = ipEndpoint.Address.ToString();
            port = ipEndpoint.Port;
            return !string.IsNullOrWhiteSpace(host) && ServerValidator.IsValidPort(port);
        }

        var colonCount = raw.Count(c => c == ':');
        if (colonCount == 1)
        {
            var idx = raw.IndexOf(':');
            if (!int.TryParse(raw[(idx + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out port) ||
                !ServerValidator.IsValidPort(port))
                return false;
            host = raw[..idx].Trim();
            return host.Length > 0 && Uri.CheckHostName(host) != UriHostNameType.Unknown;
        }

        if (colonCount > 1)
            return false;

        host = raw;
        return host.Length > 0 && Uri.CheckHostName(host) != UriHostNameType.Unknown;
    }

    private static bool TryParseOptionalInt(string? value, out int? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }
}

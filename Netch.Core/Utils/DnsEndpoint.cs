using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace Netch.Utils;

public enum DnsTransport
{
    Udp,
    Tcp,
    Tls,
    Https
}

public sealed record DnsEndpoint(
    DnsTransport Transport,
    string Host,
    ushort Port,
    string? PathAndQuery,
    string Normalized)
{
    public bool IsIpAddress => TryParseStrictIpAddress(Host, out _);

    public IPAddress? TryGetIpAddress() => TryParseStrictIpAddress(Host, out var ip) ? ip : null;

    public static bool TryParseStrictIpAddress(string? raw, [NotNullWhen(true)] out IPAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        if (trimmed.Any(char.IsWhiteSpace))
            return false;

        var hasBrackets = trimmed.Length > 2 && trimmed[0] == '[' && trimmed[^1] == ']';
        var candidate = hasBrackets ? trimmed[1..^1] : trimmed;

        if (candidate.Contains(':'))
            return IPAddress.TryParse(candidate, out address) && address.AddressFamily == AddressFamily.InterNetworkV6;

        if (hasBrackets || candidate.AsSpan().Count('.') != 3)
            return false;

        return IPAddress.TryParse(candidate, out address) && address.AddressFamily == AddressFamily.InterNetwork;
    }

    public static string Normalize(string? raw, string fallback) =>
        TryParse(raw, out var endpoint) ? endpoint.Normalized : fallback;

    public static bool TryParse(string? raw, [NotNullWhen(true)] out DnsEndpoint? endpoint)
    {
        endpoint = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        if (trimmed.Any(char.IsWhiteSpace))
            return false;

        if (!trimmed.Contains("://", StringComparison.Ordinal))
            return TryParseBareIp(trimmed, out endpoint);

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            string.IsNullOrEmpty(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        var host = uri.Host.Trim('[', ']');
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
            return false;

        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("udp" or "tcp" or "tls" or "https"))
            return false;

        ushort defaultPort = scheme switch
        {
            "tls" => 853,
            "https" => 443,
            _ => 53
        };

        var portValue = uri.IsDefaultPort ? defaultPort : uri.Port;
        if (portValue is < 1 or > ushort.MaxValue)
            return false;

        // Reject explicit :0 which Uri.TryCreate may reject or rewrite on some platforms.
        if (HasExplicitZeroPort(uri.Authority))
            return false;

        var port = (ushort)portValue;
        var formattedHost = FormatHost(host);

        if (scheme == "https")
        {
            var pathAndQuery = string.IsNullOrEmpty(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
            var authority = port == 443 ? formattedHost : $"{formattedHost}:{port}";
            var normalized = $"https://{authority}{pathAndQuery}";
            endpoint = new DnsEndpoint(DnsTransport.Https, host, port, pathAndQuery, normalized);
            return true;
        }

        if (!string.IsNullOrEmpty(uri.Query) || (uri.AbsolutePath.Length > 0 && uri.AbsolutePath != "/"))
            return false;

        var transport = scheme switch
        {
            "tcp" => DnsTransport.Tcp,
            "tls" => DnsTransport.Tls,
            _ => DnsTransport.Udp
        };

        endpoint = new DnsEndpoint(transport, host, port, null, $"{scheme}://{formattedHost}:{port}");
        return true;
    }

    private static bool TryParseBareIp(string trimmed, [NotNullWhen(true)] out DnsEndpoint? endpoint)
    {
        endpoint = null;
        if (trimmed.Contains('/') || trimmed.Contains('?') || trimmed.Contains('#'))
            return false;

        if (TryParseStrictIpAddress(trimmed, out var bareIp))
        {
            var formatted = FormatHost(bareIp.ToString());
            endpoint = new DnsEndpoint(DnsTransport.Udp, bareIp.ToString(), 53, null, $"udp://{formatted}:53");
            return true;
        }

        if (IPEndPoint.TryParse(trimmed, out var ipEndPoint))
        {
            // Explicit :0 should be rejected rather than silently defaulted to 53.
            if (HasExplicitZeroPort(trimmed))
                return false;

            var colon = trimmed.LastIndexOf(':');
            if (colon <= 0 || !TryParseStrictIpAddress(trimmed[..colon], out _))
                return false;

            var port = ipEndPoint.Port == 0 ? (ushort)53 : (ushort)ipEndPoint.Port;
            var ipStr = ipEndPoint.Address.ToString();
            endpoint = new DnsEndpoint(DnsTransport.Udp, ipStr, port, null, $"udp://{FormatHost(ipStr)}:{port}");
            return true;
        }

        return false;
    }

    private static bool HasExplicitZeroPort(string authority)
    {
        var colon = authority.LastIndexOf(':');
        if (colon < 0)
            return false;

        var bracket = authority.LastIndexOf(']');
        if (bracket > colon)
            return false;

        var portSpan = authority.AsSpan(colon + 1);
        return portSpan.Length == 0 || (int.TryParse(portSpan, out var p) && p <= 0);
    }

    private static string FormatHost(string host) =>
        IPAddress.TryParse(host, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6
            ? $"[{ip}]"
            : host;
}

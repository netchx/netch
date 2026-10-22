using System.Net;
using System.Net.Sockets;
using System.Web;

namespace Netch.Utils;

public static class ProxyUri
{
    public static Uri Parse(string text, params string[] schemes)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            !schemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) || uri.Port is < 1 or > 65535)
            throw new FormatException("Invalid proxy URI");

        return uri;
    }

    public static string Host(string host)
    {
        if (host.StartsWith('[') && host.EndsWith(']'))
            return host;

        return IPAddress.TryParse(host, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6
            ? $"[{host}]"
            : host;
    }

    public static string HostName(Uri uri)
    {
        return uri.HostNameType == UriHostNameType.IPv6 ? uri.Host.Trim('[', ']') : uri.Host;
    }

    public static string Remark(Uri uri)
    {
        return Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
    }

    public static string UserInfo(Uri uri)
    {
        return Uri.UnescapeDataString(uri.UserInfo);
    }

    public static System.Collections.Specialized.NameValueCollection Query(Uri uri)
    {
        return HttpUtility.ParseQueryString(uri.Query);
    }
}

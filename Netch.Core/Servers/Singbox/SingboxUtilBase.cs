using System.Collections.Specialized;
using Netch.Enums;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public abstract class SingboxUtilBase : ServerUtilBase
{
    protected static void ResolveTlsQuery(NameValueCollection query, Server server)
    {
        ResolveTlsQueryOnly(query, server);
        if (server.StreamSecurity.IsNullOrEmpty() || server.StreamSecurity == Constants.None)
        {
            server.StreamSecurity = Constants.StreamSecurity;
        }
    }

    protected static string ToShareUri(EConfigType type, Server server, string encodedUserInfo, Dictionary<string, string>? query = null)
    {
        var queryText = query is { Count: > 0 } ? "?" + string.Join("&", query.Select(p => $"{p.Key}={p.Value}")) : string.Empty;
        var auth = string.IsNullOrEmpty(encodedUserInfo) ? string.Empty : $"{encodedUserInfo}@";
        var remark = server.Remarks.IsNullOrEmpty() ? string.Empty : $"#{Utils.Utils.UrlEncode(server.Remarks)}";
        return $"{Constants.ProtocolShares[type]}{auth}{GetIpv6(server.Address)}:{server.Port}{queryText}{remark}";
    }

    protected static Dictionary<string, string> TlsQuery(Server server)
    {
        var query = new Dictionary<string, string>();
        if (server.Sni.IsNotEmpty())
        {
            query["sni"] = Utils.Utils.UrlEncode(server.Sni);
        }
        if (server.ConfigType == EConfigType.Naive)
        {
            return query;
        }
        if (server.Alpn.IsNotEmpty())
        {
            query["alpn"] = Utils.Utils.UrlEncode(server.Alpn);
        }
        if (server.Fingerprint.IsNotEmpty() && server.Fingerprint != Constants.None)
        {
            query["fp"] = Utils.Utils.UrlEncode(server.Fingerprint);
        }
        if (server.AllowInsecure == true)
        {
            query["insecure"] = "1";
        }
        return query;
    }

    protected static string? GetPortOrDefault(Uri url, int defaultPort)
    {
        return url.Port > 0 ? url.Port.ToString() : defaultPort.ToString();
    }
}

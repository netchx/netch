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
        ResolveUriQuery(query, ref server);
        if (server.StreamSecurity.IsNullOrEmpty() || server.StreamSecurity == Constants.None)
        {
            server.StreamSecurity = Constants.StreamSecurity;
        }
    }

    protected static string ToShareUri(EConfigType type, Server server, string userInfo, Dictionary<string, string>? query = null)
    {
        var remark = server.Remarks.IsNullOrEmpty() ? string.Empty : $"#{Utils.Utils.UrlEncode(server.Remarks)}";
        return ToUri(type, server.Address, server.Port, userInfo, query, remark);
    }

    protected static Dictionary<string, string> TlsQuery(Server server)
    {
        var query = new Dictionary<string, string>();
        if (server.Sni.IsNotEmpty())
        {
            query["sni"] = Utils.Utils.UrlEncode(server.Sni);
        }
        if (server.Alpn.IsNotEmpty())
        {
            query["alpn"] = Utils.Utils.UrlEncode(server.Alpn);
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

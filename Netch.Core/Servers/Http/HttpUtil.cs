using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public class HttpUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 0;

    public string TypeName { get; } = "HTTP";

    public string FullName { get; } = "HTTP";

    public string ShortName { get; } = "HTTP";

    public string[] UriScheme { get; } = { "http", "https" };

    public Type ServerType { get; } = typeof(HttpServer);

    public string GetShareLink(Server s)
    {
        return string.Empty;
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null || !ServerValidator.IsValidPort(url.Port) || string.IsNullOrEmpty(url.IdnHost))
            return [];

        var server = new HttpServer(url.IdnHost, (ushort)url.Port)
        {
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            StreamSecurity = url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? Constants.StreamSecurity : Constants.None
        };
        if (url.UserInfo.IsNotEmpty())
        {
            var (user, pass, hasSeparator) = SplitUserInfo(url.UserInfo);
            server.Username = user;
            if (hasSeparator)
            {
                server.Password = pass;
            }
        }
        var query = Utils.Utils.ParseQueryString(url.Query);
        if (query.Count > 0)
        {
            var security = GetQueryValue(query, "security");
            if (security.Equals(Constants.StreamSecurity, StringComparison.OrdinalIgnoreCase))
                server.StreamSecurity = Constants.StreamSecurity;
            else if (security.Equals(Constants.None, StringComparison.OrdinalIgnoreCase))
                server.StreamSecurity = Constants.None;

            server.Sni = GetQueryValue(query, "sni");
            server.Alpn = GetQueryValue(query, "alpn");
            server.Fingerprint = GetQueryValue(query, "fp");
            server.AllowInsecure = ParseAllowInsecure(query);
            var headers = GetQueryValue(query, "headers");
            if (headers.IsNotEmpty())
                server.ProtoExtra.HttpHeaders = headers;
        }
        return [server];
    }

    public bool CheckServer(Server s)
    {
        return s.Address.IsNotEmpty() && ServerValidator.IsValidPort(s.Port);
    }
}

using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class MASQUEUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 8;
    public string TypeName { get; } = nameof(EConfigType.MASQUE);
    public string FullName { get; } = "MASQUE";
    public string ShortName { get; } = "MASQUE";
    public string[] UriScheme { get; } = ["masque"];
    public Type ServerType { get; } = typeof(MASQUEServer);

    public string GetShareLink(Server s)
    {
        var query = TlsQuery(s);
        if (s.ProtoExtra.MasquePath.IsNotEmpty()) query["path"] = Utils.Utils.UrlEncode(s.ProtoExtra.MasquePath);
        if (s.ProtoExtra.MasqueHeaders.IsNotEmpty()) query["headers"] = Utils.Utils.UrlEncode(s.ProtoExtra.MasqueHeaders);
        var userInfo = s.Username.IsNotEmpty() || s.Password.IsNotEmpty()
            ? $"{Utils.Utils.UrlEncode(s.Username ?? string.Empty)}:{Utils.Utils.UrlEncode(s.Password ?? string.Empty)}"
            : string.Empty;
        return ToShareUri(EConfigType.MASQUE, s, userInfo, query);
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null || url.IdnHost.IsNullOrEmpty()) return [];
        var server = new MASQUEServer()
        {
            Address = url.IdnHost,
            Port = url.Port > 0 ? url.Port : 443,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            StreamSecurity = Constants.StreamSecurity,
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
        var pathQuery = GetQueryDecoded(query, "path");
        if (pathQuery.IsNotEmpty())
            server.ProtoExtra.MasquePath = pathQuery;
        else if (url.AbsolutePath.IsNotEmpty() && url.AbsolutePath != "/")
            server.ProtoExtra.MasquePath = Utils.Utils.UrlDecode(url.AbsolutePath);
        server.ProtoExtra.MasqueHeaders = GetQueryDecoded(query, "headers");
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty() && ServerValidator.IsValidPort(s.Port);
}

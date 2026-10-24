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

    public void Edit(Server s) => new SingboxServerForm(s, EConfigType.MASQUE).ShowDialog();
    public void Create() => new SingboxServerForm(null, EConfigType.MASQUE).ShowDialog();

    public string GetShareLink(Server s)
    {
        var query = TlsQuery(s);
        if (s.ProtoExtra.MasquePath.IsNotEmpty()) query["path"] = Utils.Utils.UrlEncode(s.ProtoExtra.MasquePath);
        if (s.ProtoExtra.MasqueHeaders.IsNotEmpty()) query["headers"] = Utils.Utils.UrlEncode(s.ProtoExtra.MasqueHeaders);
        return ToShareUri(EConfigType.MASQUE, s, $"{Utils.Utils.UrlEncode(s.Username)}:{Utils.Utils.UrlEncode(s.Password)}", query);
    }

    public IServerController GetController() => new V2rayController();

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null) return [];
        var server = new MASQUEServer()
        {
            Address = url.IdnHost,
            Port = url.Port,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            StreamSecurity = Constants.StreamSecurity,
            Password = string.Empty
        };
        var userInfo = Utils.Utils.UrlDecode(url.UserInfo).Split(':', 2);
        if (userInfo.Length > 0) server.Username = userInfo[0];
        if (userInfo.Length > 1) server.Password = userInfo[1];
        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQuery(query, server);
        server.ProtoExtra.MasquePath = GetQueryDecoded(query, "path", "/");
        server.ProtoExtra.MasqueHeaders = GetQueryDecoded(query, "headers");
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty() && s.Port > 0 && s.Password.IsNotEmpty();
}

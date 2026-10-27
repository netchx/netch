using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class TUICUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 5;
    public string TypeName { get; } = nameof(EConfigType.TUIC);
    public string FullName { get; } = "TUIC";
    public string ShortName { get; } = "TUIC";
    public string[] UriScheme { get; } = ["tuic"];
    public Type ServerType { get; } = typeof(TUICServer);

    public void Edit(Server s) => new SingboxServerForm(s, EConfigType.TUIC).ShowDialog();
    public void Create() => new SingboxServerForm(null, EConfigType.TUIC).ShowDialog();

    public string GetShareLink(Server s)
    {
        var query = TlsQuery(s);
        if (s.AllowInsecure == true) query["allow_insecure"] = "1";
        if (s.ProtoExtra.CongestionControl.IsNotEmpty()) query["congestion_control"] = Utils.Utils.UrlEncode(s.ProtoExtra.CongestionControl);
        return ToShareUri(EConfigType.TUIC, s, $"{Utils.Utils.UrlEncode(s.Username)}:{Utils.Utils.UrlEncode(s.Password)}", query);
    }

    public IServerController GetController() => new SingboxController();

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null) return [];

        var server = new TUICServer()
        {
            Address = url.IdnHost,
            Port = url.Port,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped)
        };
        var userInfo = Utils.Utils.UrlDecode(url.UserInfo).Split(':', 2);
        if (userInfo.Length != 2) return [];
        server.Username = userInfo[0];
        server.Password = userInfo[1];
        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQuery(query, server);
        server.AllowInsecure = GetQueryValue(query, "allow_insecure") == "1" || server.AllowInsecure == true;
        server.ProtoExtra.CongestionControl = GetQueryDecoded(query, "congestion_control");
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty() && s.Port > 0 && s.Username.IsNotEmpty() && s.Password.IsNotEmpty();
}

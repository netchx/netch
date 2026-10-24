using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class AnyTLSUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 6;
    public string TypeName { get; } = nameof(EConfigType.AnyTLS);
    public string FullName { get; } = "AnyTLS";
    public string ShortName { get; } = "AnyTLS";
    public string[] UriScheme { get; } = ["anytls"];
    public Type ServerType { get; } = typeof(AnyTLSServer);

    public void Edit(Server s) => new SingboxServerForm(s, EConfigType.AnyTLS).ShowDialog();
    public void Create() => new SingboxServerForm(null, EConfigType.AnyTLS).ShowDialog();

    public string GetShareLink(Server s)
    {
        return ToShareUri(EConfigType.AnyTLS, s, Utils.Utils.UrlEncode(s.Password), TlsQuery(s));
    }

    public IServerController GetController() => new SingboxController();

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null) return [];
        var server = new AnyTLSServer()
        {
            Address = url.IdnHost,
            Port = url.Port,
            Password = Utils.Utils.UrlDecode(url.UserInfo),
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped)
        };
        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQuery(query, server);
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty() && s.Port > 0 && s.Password.IsNotEmpty();
}

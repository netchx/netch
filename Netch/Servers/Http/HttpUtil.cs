using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class HttpUtil : IServerUtil
{
    public ushort Priority { get; } = 0;

    public string TypeName { get; } = "HTTP";

    public string FullName { get; } = "HTTP";

    public string ShortName { get; } = "HTTP";

    public string[] UriScheme { get; } = { "http" };

    public Type ServerType { get; } = typeof(HttpServer);

    public void Edit(Server s)
    {
        new HttpForm((HttpServer)s).ShowDialog();
    }

    public void Create()
    {
        new HttpForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        return string.Empty;
    }

    public IServerController GetController()
    {
        return new SocksController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null || url.Port <= 0 || string.IsNullOrEmpty(url.IdnHost))
            return [];

        var userInfo = Utils.Utils.UrlDecode(url.UserInfo).Split(':', 2);
        var server = new HttpServer(url.IdnHost, (ushort)url.Port)
        {
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped)
        };
        if (userInfo.Length == 2)
        {
            server.Username = userInfo[0];
            server.Password = userInfo[1];
        }
        return [server];
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}

using System.Web;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class Socks5Util : IServerUtil
{
    public ushort Priority { get; } = 0;

    public string TypeName { get; } = "SOCKS";

    public string FullName { get; } = "SOCKS";

    public string ShortName { get; } = "SOCKS";

    public string[] UriScheme { get; } = { "socks", "socks5", "socks5h" };

    public Type ServerType { get; } = typeof(Socks5Server);

    public void Edit(Server s)
    {
        new Socks5Form((Socks5Server)s).ShowDialog();
    }

    public void Create()
    {
        new Socks5Form().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        var server = (Socks5Server)s;
        if (server.Version != "5" || !string.IsNullOrWhiteSpace(server.RemoteHostname) ||
            (!server.Auth() && (!string.IsNullOrWhiteSpace(server.Username) || !string.IsNullOrWhiteSpace(server.Password))))
            return ShareLink.GetNetchLink(server);

        var credentials = server.Auth()
            ? $"{Uri.EscapeDataString(server.Username!)}:{Uri.EscapeDataString(server.Password!)}@"
            : "";
        var remark = string.IsNullOrEmpty(server.Remark) ? "" : $"#{Uri.EscapeDataString(server.Remark)}";
        return $"socks5://{credentials}{ProxyUri.Host(server.Hostname)}:{server.Port}{remark}";
    }

    public IServerController GetController()
    {
        return new Socks5Controller();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        if (!text.StartsWith("tg://socks?", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("https://t.me/socks?", StringComparison.OrdinalIgnoreCase))
        {
            var uri = ProxyUri.Parse(text, UriScheme);
            var credentials = uri.UserInfo.Split(':', 2);
            var standard = new Socks5Server
            {
                Hostname = ProxyUri.HostName(uri),
                Port = (ushort)uri.Port,
                Remark = ProxyUri.Remark(uri)
            };
            if (credentials.Length > 0 && credentials[0].Length > 0)
                standard.Username = Uri.UnescapeDataString(credentials[0]);
            if (credentials.Length > 1)
                standard.Password = Uri.UnescapeDataString(credentials[1]);
            if (uri.UserInfo.Length > 0 && !standard.Auth())
                throw new FormatException("Incomplete SOCKS5 credentials");
            return new[] { standard };
        }

        var query = HttpUtility.ParseQueryString(new Uri(text).Query);
        if (string.IsNullOrWhiteSpace(query["server"]) || !ushort.TryParse(query["port"], out var port) || port == 0)
            throw new FormatException();

        var data = new Socks5Server
        {
            Hostname = query["server"]!,
            Port = port
        };

        if (!string.IsNullOrWhiteSpace(query["user"]))
            data.Username = query["user"];

        if (!string.IsNullOrWhiteSpace(query["pass"]))
            data.Password = query["pass"];

        if ((!string.IsNullOrWhiteSpace(data.Username) || !string.IsNullOrWhiteSpace(data.Password)) && !data.Auth())
            throw new FormatException("Incomplete SOCKS5 credentials");

        return new[] { data };
    }

    public bool CheckServer(Server s)
    {
        var server = (Socks5Server)s;
        return !string.IsNullOrWhiteSpace(server.Hostname) && server.Port != 0 && SOCKSGlobal.Versions.Contains(server.Version) &&
               ((!string.IsNullOrWhiteSpace(server.Username) && !string.IsNullOrWhiteSpace(server.Password)) ||
                (string.IsNullOrWhiteSpace(server.Username) && string.IsNullOrWhiteSpace(server.Password)));
    }
}

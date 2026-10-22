using System.Web;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class Socks5Util : IServerUtil
{
    public ushort Priority { get; } = 0;

    public string TypeName { get; } = "SOCKS";

    public string FullName { get; } = "SOCKS";

    public string ShortName { get; } = "SOCKS";

    public string[] UriScheme { get; } = { };

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
        // https://t.me/socks?server=1.1.1.1&port=443
        return $"https://t.me/socks?server={Uri.EscapeDataString(server.Hostname)}&port={server.Port}" +
               $"{(!string.IsNullOrWhiteSpace(server.Username) ? $"&user={Uri.EscapeDataString(server.Username)}" : "")}" +
               $"{(server.Auth() ? $"&pass={Uri.EscapeDataString(server.Password!)}" : "")}";
    }

    public IServerController GetController()
    {
        return new Socks5Controller();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var query = HttpUtility.ParseQueryString(new Uri(text).Query);
        if (string.IsNullOrWhiteSpace(query["server"]) || !ushort.TryParse(query["port"], out var port))
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

        return new[] { data };
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}

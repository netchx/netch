using Netch.Models;

namespace Netch.Servers;

public class Socks5Controller : V2rayController
{
    public override string Name { get; } = "Socks5";

    public override Task<Socks5Server> StartAsync(Server s)
    {
        var server = (Socks5Server)s;
        if (!server.Auth())
            throw new ArgumentException();

        return base.StartAsync(s);
    }
}
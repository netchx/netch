using Netch.Models;

namespace Netch.Servers;

public class SocksController : V2rayController
{
    public override string Name { get; } = "Socks";

    public override Task<SocksServer> StartAsync(Server s)
    {
        var server = (SocksServer)s;
        if (!server.Auth())
            throw new ArgumentException();

        return base.StartAsync(s);
    }
}
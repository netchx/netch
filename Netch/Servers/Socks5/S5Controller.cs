using System.Threading.Tasks;
using Netch.Models;

namespace Netch.Servers
{
    public class S5Controller : V2rayController
    {
        public override string Name { get; } = "Socks5";

        public override async Task<Socks5> StartAsync(Server s)
        {
            var server = (Socks5)s;
            if (server.Auth())
                await base.StartAsync(s);

            return server;
        }
    }
}
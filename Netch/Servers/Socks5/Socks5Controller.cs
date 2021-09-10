using System;
using System.Threading.Tasks;
using Netch.Models;

namespace Netch.Servers
{
    public class Socks5Controller : V2rayController
    {
        public override string Name { get; } = "Socks5";

        public override async Task<Socks5LocalServer> StartAsync(Server s)
        {
            var server = (Socks5Server)s;
            if (!server.Auth())
                throw new ArgumentException();

            return await base.StartAsync(s);
        }
    }
}
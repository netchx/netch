using Netch.Models;
using Netch.Servers;

namespace Netch.Servers
{
    public class S5Controller : V2rayController
    {
        public override string Name { get; } = "Socks5";

        public override Socks5 Start(in Server s)
        {
            var server = (Socks5)s;
            if (server.Auth())
                base.Start(s);

            return server;
        }
    }
}
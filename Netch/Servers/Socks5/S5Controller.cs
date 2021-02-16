using Netch.Models;
using Netch.Servers.V2ray;

namespace Netch.Servers.Socks5
{
    public class S5Controller : V2rayController
    {
        public override string Name { get; } = "Socks5";

        public override bool Start(in Server s, in Mode mode)
        {
            var server = (Socks5) s;
            if (server.Auth())
                return base.Start(s, mode);

            return true;
        }
    }
}
using System.IO;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.VMess.Utils;

namespace Netch.Servers.Socks5
{
    public class S5Controller : Guard, IServerController
    {
        public override string Name { get; protected set; } = "Socks5";
        public override string MainFile { get; protected set; } = "v2ray.exe";

        public bool Start(in Server s, in Mode mode)
        {
            var server = (Socks5) s;
            if (server.Auth())
            {
                File.WriteAllText("data\\last.json", V2rayConfigUtils.GenerateClientConfig(s, mode));
                if (StartInstanceAuto("-config ..\\data\\last.json"))
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        public override void Stop()
        {
            if (Instance != null)
                StopInstance();
        }

        public ushort? Socks5LocalPort { get; set; }

        public string LocalAddress { get; set; }
    }
}
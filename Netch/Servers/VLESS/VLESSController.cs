using System.IO;
using Netch.Controllers;
using Netch.Models;

namespace Netch.Servers.VLESS
{
    public class VLESSController : Guard, IServerController
    {
        public override string Name { get; protected set; } = "VLESS";
        public override string MainFile { get; protected set; } = "v2ray.exe";

        public int? Socks5LocalPort { get; set; }

        public string LocalAddress { get; set; }

        public bool Start(Server s, Mode mode)
        {
            var server = (VLESS) s;

            File.WriteAllText("data\\last.json", VMess.Utils.V2rayConfigUtils.GenerateClientConfig(server, mode));

            if (StartInstanceAuto("-config ..\\data\\last.json"))
            {
                if (File.Exists("data\\last.json")) File.Delete("data\\last.json");
                return true;
            }

            return false;
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
using System.IO;
using Netch.Controllers;
using Netch.Models;

namespace Netch.Servers.VMess
{
    public class VMessController : Guard, IServerController
    {
        public VMessController()
        {
            StartedKeywords.Add("started");
            StoppedKeywords.AddRange(new[] {"config file not readable", "failed to"});
        }

        public override string Name { get; protected set; } = "V2Ray";
        public override string MainFile { get; protected set; } = "v2ray.exe";
        public int? Socks5LocalPort { get; set; }
        public string LocalAddress { get; set; }


        public bool Start(Server s, Mode mode)
        {
            File.WriteAllText("data\\last.json", Utils.V2rayConfigUtils.GenerateClientConfig(s, mode));

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
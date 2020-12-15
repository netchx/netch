using System.IO;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.VMess.Utils;

namespace Netch.Servers.VMess
{
    public class VMessController : Guard, IServerController
    {
        public VMessController()
        {
            StartedKeywords.Add("started");
            StoppedKeywords.AddRange(new[] {"config file not readable", "failed to"});
        }

        public override string Name { get; protected set; } = "VMess";
        public override string MainFile { get; protected set; } = "v2ray.exe";
        public ushort? Socks5LocalPort { get; set; }
        public string LocalAddress { get; set; }


        public  bool Start(in Server s,in Mode mode)
        {
            File.WriteAllText("data\\last.json", V2rayConfigUtils.GenerateClientConfig(s, mode));
            return StartInstanceAuto("-config ..\\data\\last.json");
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.V2ray.Utils;

namespace Netch.Servers.V2ray
{
    public class V2rayController : Guard, IServerController
    {
        public override string MainFile { get; protected set; } = "xray.exe";

        protected override IEnumerable<string> StartedKeywords { get; } = new[] {"started"};

        protected override IEnumerable<string> StoppedKeywords { get; } = new[] {"config file not readable", "failed to"};

        public override string Name { get; } = "Xray";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public virtual void Start(in Server s, in Mode mode)
        {
            File.WriteAllText("data\\last.json", V2rayConfigUtils.GenerateClientConfig(s, mode));
            StartInstanceAuto("-config ..\\data\\last.json");
        }

        public override void Stop()
        {
            StopInstance();
        }

        protected override void InitInstance(string argument)
        {
            base.InitInstance(argument);
            if (!Global.Settings.V2RayConfig.XrayCone)
                Instance!.StartInfo.Environment["XRAY_CONE_DISABLED"] = "true";
        }
    }
}
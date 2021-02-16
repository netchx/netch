using System.IO;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.V2ray.Utils;

namespace Netch.Servers.V2ray
{
    public class V2rayController : Guard, IServerController
    {
        public V2rayController()
        {
            StartedKeywords.Add("started");
            StoppedKeywords.AddRange(new[] {"config file not readable", "failed to"});
        }
        public override string MainFile { get; protected set; } = "xray.exe";

        public override string Name { get; } = "Xray";
        public ushort? Socks5LocalPort { get; set; }
        public string LocalAddress { get; set; }
        public virtual bool Start(in Server s, in Mode mode)
        {
            File.WriteAllText("data\\last.json", V2rayConfigUtils.GenerateClientConfig(s, mode));
            return StartInstanceAuto("-config ..\\data\\last.json");
        }

        public override void Stop()
        {
            StopInstance();
        }

        protected override void InitInstance(string argument)
        {
            base.InitInstance(argument);
            if (!Global.Settings.V2RayConfig.XrayCone)
                Instance.StartInfo.Environment["XRAY_CONE_DISABLED"] = "true";
        }
    }
}
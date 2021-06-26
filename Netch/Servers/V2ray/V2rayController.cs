using System.Collections.Generic;
using System.IO;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers.V2ray.Utils;

namespace Netch.Servers.V2ray
{
    public class V2rayController : Guard, IServerController
    {
        public V2rayController() : base("xray.exe")
        {
            if (!Global.Settings.V2RayConfig.XrayCone)
                Instance.StartInfo.Environment["XRAY_CONE_DISABLED"] = "true";
        }

        protected override IEnumerable<string> StartedKeywords => new[] { "started" };

        protected override IEnumerable<string> FailedKeywords => new[] { "config file not readable", "failed to" };

        public override string Name => "Xray";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public virtual Socks5.Socks5 Start(in Server s)
        {
            File.WriteAllText(Constants.TempConfig, V2rayConfigUtils.GenerateClientConfig(s));
            StartGuard("-config ..\\data\\last.json");
            return new Socks5.Socks5(this.LocalAddress(), this.Socks5LocalPort());
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers.Utils;

namespace Netch.Servers
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

        public virtual Socks5 Start(in Server s)
        {
            using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write))
            {
                JsonSerializer.SerializeAsync(fileStream, V2rayConfigUtils.GenerateClientConfig(s), Global.NewCustomJsonSerializerOptions()).Wait();
            }

            StartGuard("-config ..\\data\\last.json");
            return new Socks5Bridge(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), s.Hostname);
        }
    }
}
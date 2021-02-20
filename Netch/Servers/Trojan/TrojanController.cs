using System.Collections.Generic;
using System.IO;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.Trojan.Models;
using Newtonsoft.Json;

namespace Netch.Servers.Trojan
{
    public class TrojanController : Guard, IServerController
    {
        public override string MainFile { get; protected set; } = "Trojan.exe";

        protected override IEnumerable<string> StartedKeywords { get; } = new[] {"started"};

        protected override IEnumerable<string> StoppedKeywords { get; } = new[] {"exiting"};

        public override string Name { get; } = "Trojan";

        public ushort? Socks5LocalPort { get; set; }

        public string LocalAddress { get; set; }

        public void Start(in Server s, in Mode mode)
        {
            var server = (Trojan) s;
            var trojanConfig = new TrojanConfig
            {
                local_addr = this.LocalAddress(),
                local_port = this.Socks5LocalPort(),
                remote_addr = server.Hostname,
                remote_port = server.Port,
                password = new List<string>
                {
                    server.Password
                }
            };

            if (!string.IsNullOrWhiteSpace(server.Host))
                trojanConfig.ssl.sni = server.Host;

            File.WriteAllText("data\\last.json",
                JsonConvert.SerializeObject(trojanConfig,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));

            StartInstanceAuto("-c ..\\data\\last.json");
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
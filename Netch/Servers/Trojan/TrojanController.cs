using System.Collections.Generic;
using System.IO;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.Trojan.Models;
using Netch.Utils;
using Newtonsoft.Json;

namespace Netch.Servers.Trojan
{
    public class TrojanController : Guard, IServerController
    {
        public TrojanController()
        {
            StartedKeywords.Add("started");
            StoppedKeywords.Add("exiting");
        }

        public override string MainFile { get; protected set; } = "Trojan.exe";
        public override string Name { get; protected set; } = "Trojan";
        public Server Server { get; set; }
        public ushort? Socks5LocalPort { get; set; }
        public string LocalAddress { get; set; }


        public bool Start(in Server s, in Mode mode)
        {
            Server = s;
            var server = (Trojan) s;
            File.WriteAllText("data\\last.json", JsonConvert.SerializeObject(new TrojanConfig
            {
                local_addr = this.LocalAddress(),
                local_port = this.Socks5LocalPort(),
                remote_addr = server.Hostname,
                remote_port = server.Port,
                password = new List<string>
                {
                    server.Password
                }
            }));

            return StartInstanceAuto("-c ..\\data\\last.json");
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using Netch.Models;
using Netch.ServerEx.Trojan.Models;
using Newtonsoft.Json;

namespace Netch.ServerEx.Trojan
{
    public class TrojanController : ServerController
    {
        public TrojanController()
        {
            Name = "Trojan";
            MainFile = "Trojan.exe";
            StartedKeywords.Add("started");
            StoppedKeywords.Add("exiting");
        }

        public override bool Start(Server s, Mode mode)
        {
            var server = (Trojan) s;
            File.WriteAllText("data\\last.json", JsonConvert.SerializeObject(new TrojanConfig
            {
                local_addr = LocalAddress,
                local_port = Socks5LocalPort,
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
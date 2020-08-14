using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Netch.Models;
using Netch.Utils;
using Newtonsoft.Json;

namespace Netch.Controllers
{
    public class TrojanController : EncryptedProxy
    {
        public TrojanController()
        {
            Name = "Trojan";
            MainFile = "Trojan.exe";
            StartedKeywords.Add("started");
            StoppedKeywords.Add("exiting");
        }

        public override bool Start(Server server, Mode mode)
        {
            File.WriteAllText("data\\last.json", JsonConvert.SerializeObject(new Trojan
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
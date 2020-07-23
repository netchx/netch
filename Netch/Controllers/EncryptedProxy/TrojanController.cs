using System.Collections.Generic;
using System.IO;
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
            StartedKeywords("started");
            StoppedKeywords("exiting");
        }

        public override bool Start(Server server, Mode mode)
        {

            File.WriteAllText("data\\last.json", JsonConvert.SerializeObject(new Trojan
            {
                local_addr = Global.Settings.LocalAddress,
                local_port = Global.Settings.Socks5LocalPort,
                remote_addr = server.Hostname,
                remote_port = server.Port,
                password = new List<string>
                {
                    server.Password
                }
            }));

            Instance = GetProcess();
            Instance.StartInfo.Arguments = "-c ..\\data\\last.json";
            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            State = State.Starting;
            Instance.Start();
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == State.Started) return true;

                if (State == State.Stopped)
                {
                    Logging.Error("Trojan 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Logging.Error("Trojan 进程启动超时");
            Stop();
            return false;
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
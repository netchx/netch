using System.Collections.Generic;
using System.Diagnostics;
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
            MainFile = "Trojan";
            InitCheck();
        }

        public override bool Start(Server server, Mode mode)
        {
            if (!Ready) return false;

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

            Instance = GetProcess("bin\\Trojan.exe");
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

        public override void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!Write(e.Data)) return;
            if (State == State.Starting)
            {
                if (Instance.HasExited)
                    State = State.Stopped;
                else if (e.Data.Contains("started"))
                    State = State.Started;
                else if (e.Data.Contains("exiting")) State = State.Stopped;
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
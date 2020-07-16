using System.Diagnostics;
using System.Threading;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class SSRController : EncryptedProxy
    {
        public SSRController()
        {
            MainFile = "ShadowsocksR";
            InitCheck();
        }

        public override bool Start(Server server, Mode mode)
        {
            if (!Ready) return false;

            Instance = GetProcess("bin\\ShadowsocksR.exe");
            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            Instance.StartInfo.Arguments = $"-s {server.Hostname} -p {server.Port} -k \"{server.Password}\" -m {server.EncryptMethod} -t 120";

            if (!string.IsNullOrEmpty(server.Protocol))
            {
                Instance.StartInfo.Arguments += $" -O {server.Protocol}";

                if (!string.IsNullOrEmpty(server.ProtocolParam)) Instance.StartInfo.Arguments += $" -G \"{server.ProtocolParam}\"";
            }

            if (!string.IsNullOrEmpty(server.OBFS))
            {
                Instance.StartInfo.Arguments += $" -o {server.OBFS}";

                if (!string.IsNullOrEmpty(server.OBFSParam)) Instance.StartInfo.Arguments += $" -g \"{server.OBFSParam}\"";
            }

            Instance.StartInfo.Arguments += $" -b {Global.Settings.LocalAddress} -l {Global.Settings.Socks5LocalPort} -u";

            if (mode.BypassChina) Instance.StartInfo.Arguments += " --acl default.acl";

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
                    Logging.Error("SSR 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Logging.Error("SSR 进程启动超时");
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
                else if (e.Data.Contains("listening at"))
                    State = State.Started;
                else if (e.Data.Contains("Invalid config path") || e.Data.Contains("usage")) State = State.Stopped;
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
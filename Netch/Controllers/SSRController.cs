using Netch.Forms;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Netch.Controllers
{
    public class SSRController
    {
        /// <summary>
        ///		进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        ///		当前状态
        /// </summary>
        public Models.State State = Models.State.Waiting;

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Models.Server server, Models.Mode mode)
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting ShadowsocksR")}");
            if (!File.Exists("bin\\ShadowsocksR.exe"))
            {
                return false;
            }

            Instance = MainController.GetProcess();
            Instance.StartInfo.FileName = "bin\\ShadowsocksR.exe";
            Instance.StartInfo.Arguments = $"-s {server.Hostname} -p {server.Port} -k \"{server.Password}\" -m {server.EncryptMethod}";

            if (!string.IsNullOrEmpty(server.Protocol))
            {
                Instance.StartInfo.Arguments += $" -O {server.Protocol}";

                if (!string.IsNullOrEmpty(server.ProtocolParam))
                {
                    Instance.StartInfo.Arguments += $" -G \"{server.ProtocolParam}\"";
                }
            }

            if (!string.IsNullOrEmpty(server.OBFS))
            {
                Instance.StartInfo.Arguments += $" -o {server.OBFS}";

                if (!string.IsNullOrEmpty(server.OBFSParam))
                {
                    Instance.StartInfo.Arguments += $" -g \"{server.OBFSParam}\"";
                }
            }

            Instance.StartInfo.Arguments += $" -b {Global.Settings.LocalAddress} -l {Global.Settings.Socks5LocalPort} -u";

            if (mode.BypassChina)
            {
                Instance.StartInfo.Arguments += " --acl default.acl";
            }

            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            State = Models.State.Starting;
            Instance.Start();
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == Models.State.Started)
                {
                    return true;
                }

                if (State == Models.State.Stopped)
                {
                    Utils.Logging.Info("SSR 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Utils.Logging.Info("SSR 进程启动超时");
            Stop();
            return false;
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            try
            {
                if (Instance != null && !Instance.HasExited)
                {
                    Instance.Kill();
                }
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                File.AppendAllText("logging\\shadowsocksr.log", $"{e.Data}\r\n");

                if (State == Models.State.Starting)
                {
                    if (Instance.HasExited)
                    {
                        State = Models.State.Stopped;
                    }
                    else if (e.Data.Contains("listening at"))
                    {
                        State = Models.State.Started;
                    }
                    else if (e.Data.Contains("Invalid config path") || e.Data.Contains("usage"))
                    {
                        State = Models.State.Stopped;
                    }
                }
            }
        }
    }
}

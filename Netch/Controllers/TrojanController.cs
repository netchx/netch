using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Netch.Controllers
{
    public class TrojanController
    {
        /// <summary>
        ///     进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        ///     当前状态
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
            Forms.MainForm.Instance.StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting Trojan")}");

            File.Delete("logging\\trojan.log");
            if (!File.Exists("bin\\Trojan.exe"))
            {
                return false;
            }

            File.WriteAllText("data\\last.json", Newtonsoft.Json.JsonConvert.SerializeObject(new Models.Trojan()
            {
                local_addr = Global.Settings.LocalAddress,
                local_port = Global.Settings.Socks5LocalPort,
                remote_addr = server.Hostname,
                remote_port = server.Port,
                password = new List<string>()
                {
                    server.Password
                }
            }));

            Instance = MainController.GetProcess();
            Instance.StartInfo.FileName = "bin\\Trojan.exe";
            Instance.StartInfo.Arguments = "-c ..\\data\\last.json";
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
                    Utils.Logging.Info("Trojan 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Utils.Logging.Info("Trojan 进程启动超时");
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
                    Instance.WaitForExit();
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
                File.AppendAllText("logging\\trojan.log", $"{e.Data}\r\n");

                if (State == Models.State.Starting)
                {
                    if (Instance.HasExited)
                    {
                        State = Models.State.Stopped;
                    }
                    else if (e.Data.Contains("started"))
                    {
                        State = Models.State.Started;
                    }
                    else if (e.Data.Contains("exiting"))
                    {
                        State = Models.State.Stopped;
                    }
                }
            }
        }
    }
}

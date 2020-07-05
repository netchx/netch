using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;
using Newtonsoft.Json;

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
        public State State = State.Waiting;

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Server server, Mode mode)
        {
            MainForm.Instance.StatusText($"{i18N.Translate("Status")}{i18N.Translate(": ")}{i18N.Translate("Starting Trojan")}");

            File.Delete("logging\\trojan.log");
            if (!File.Exists("bin\\Trojan.exe"))
            {
                return false;
            }

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

            Instance = MainController.GetProcess();
            Instance.StartInfo.FileName = "bin\\Trojan.exe";
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

                if (State == State.Started)
                {
                    return true;
                }

                if (State == State.Stopped)
                {
                    Logging.Info("Trojan 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Logging.Info("Trojan 进程启动超时");
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
                Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                File.AppendAllText("logging\\trojan.log", $"{e.Data}\r\n");

                if (State == State.Starting)
                {
                    if (Instance.HasExited)
                    {
                        State = State.Stopped;
                    }
                    else if (e.Data.Contains("started"))
                    {
                        State = State.Started;
                    }
                    else if (e.Data.Contains("exiting"))
                    {
                        State = State.Stopped;
                    }
                }
            }
        }
    }
}

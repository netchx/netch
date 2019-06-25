using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Netch.Controllers
{
    public class SSController
    {
        /// <summary>
        ///		进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        ///		当前状态
        /// </summary>
        public Objects.State State = Objects.State.Waiting;

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <returns>是否成功</returns>
        public bool Start(Objects.Server server)
        {
            if (!File.Exists("bin\\Shadowsocks.exe"))
            {
                return false;
            }

            // 清理上一次的日志文件，防止淤积占用磁盘空间
            if (Directory.Exists("logging"))
            {
                if (File.Exists("logging\\shadowsocks.log"))
                {
                    File.Delete("logging\\shadowsocks.log");
                }
            }

            Instance = MainController.GetProcess();
            Instance.StartInfo.FileName = "bin\\Shadowsocks.exe";
            Instance.StartInfo.Arguments = String.Format("-s {0} -p {1} -l 2801 -m {2} -k \"{3}\" -u", server.Address, server.Port, server.EncryptMethod, server.Password);
            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            State = Objects.State.Starting;
            Instance.Start();
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == Objects.State.Started)
                {
                    return true;
                }

                if (State == Objects.State.Stopped)
                {
                    Utils.Logging.Info("SS 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Utils.Logging.Info("SS 进程启动超时");
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
            if (!String.IsNullOrWhiteSpace(e.Data))
            {
                File.AppendAllText("logging\\shadowsocks.log", $"{e.Data}\r\n");

                if (State == Objects.State.Starting)
                {
                    if (Instance.HasExited)
                    {
                        State = Objects.State.Stopped;
                    }
                    else if (e.Data.Contains("listening at"))
                    {
                        State = Objects.State.Started;
                    }
                    else if (e.Data.Contains("Invalid config path") || e.Data.Contains("usage"))
                    {
                        State = Objects.State.Stopped;
                    }
                }
            }
        }
    }
}

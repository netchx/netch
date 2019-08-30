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
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
            foreach (var proc in Process.GetProcessesByName("Shadowsocks"))
            {
                try
                {
                    proc.Kill();
                }
                catch (Exception)
                {
                    // 跳过
                }
            }

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
            
            if(!String.IsNullOrWhiteSpace(server.OBFS) && !String.IsNullOrWhiteSpace(server.OBFSParam))
            {
                Instance.StartInfo.Arguments = String.Format("-s {0} -p {1} -b 0.0.0.0 -l 2801 -m {2} -k \"{3}\" -u --plugin {4} --plugin-opts \"{5}\"", server.Address, server.Port, server.EncryptMethod, server.Password, server.OBFS, server.OBFSParam);
            }
            else
            {
                Instance.StartInfo.Arguments = String.Format("-s {0} -p {1} -b 0.0.0.0 -l 2801 -m {2} -k \"{3}\" -u", server.Address, server.Port, server.EncryptMethod, server.Password);
            }
            
            if (mode.BypassChina)
            {
                Instance.StartInfo.Arguments += " --acl default.acl";
            }

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
                    else if (e.Data.Contains("Invalid config path") || e.Data.Contains("usage") || e.Data.Contains("plugin service exit unexpectedly"))
                    {
                        State = Objects.State.Stopped;
                    }
                }
            }
        }
    }
}

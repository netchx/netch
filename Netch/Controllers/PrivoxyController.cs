using System;
using System.Diagnostics;
using System.IO;

namespace Netch.Controllers
{
    public class PrivoxyController
    {
        /// <summary>
        ///     进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
            foreach (var proc in Process.GetProcessesByName("Privoxy"))
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

            if (!File.Exists("bin\\Privoxy.exe") || !File.Exists("bin\\default.conf"))
            {
                return false;
            }

            File.WriteAllText("data\\privoxy.conf", File.ReadAllText("bin\\default.conf").Replace("_BIND_PORT_", "2802").Replace("_DEST_PORT_", "2801"));

            Instance = new Process()
            {
                StartInfo =
                {
                    FileName = String.Format("{0}\\bin\\Privoxy.exe", Directory.GetCurrentDirectory()),
                    Arguments = "..\\data\\privoxy.conf",
                    WorkingDirectory = String.Format("{0}\\bin", Directory.GetCurrentDirectory()),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            Instance.Start();

            return true;
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
            catch (Exception)
            {
                // 跳过
            }
        }
    }
}

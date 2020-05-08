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
        public bool Start(Models.Server server, Models.Mode mode)
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

            if (server.Type != "Socks5")
            {
                File.WriteAllText("data\\privoxy.conf", File.ReadAllText("bin\\default.conf").Replace("_BIND_PORT_", Global.Settings.HTTPLocalPort.ToString()).Replace("_DEST_PORT_", Global.Settings.Socks5LocalPort.ToString()).Replace("0.0.0.0", Global.Settings.LocalAddress));
            }
            else
            {
                File.WriteAllText("data\\privoxy.conf", File.ReadAllText("bin\\default.conf").Replace("_BIND_PORT_", Global.Settings.HTTPLocalPort.ToString()).Replace("_DEST_PORT_", server.Port.ToString()).Replace("s 0.0.0.0", $"s {Global.Settings.LocalAddress}").Replace("/ 127.0.0.1", $"/ {server.Hostname}"));
            }


            Instance = new Process
            {
                StartInfo =
                {
                    FileName = string.Format("{0}\\bin\\Privoxy.exe", Directory.GetCurrentDirectory()),
                    Arguments = "..\\data\\privoxy.conf",
                    WorkingDirectory = string.Format("{0}\\bin", Directory.GetCurrentDirectory()),
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
                    Instance.WaitForExit();
                }
            }
            catch (Exception)
            {
                // 跳过
            }
        }
    }
}

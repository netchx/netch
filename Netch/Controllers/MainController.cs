using System;
using System.Diagnostics;
using System.IO;

namespace Netch.Controllers
{
    public class MainController
    {
        public static Process GetProcess()
        {
            var process = new Process();
            process.StartInfo.WorkingDirectory = String.Format("{0}\\bin", Directory.GetCurrentDirectory());
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;

            return process;
        }

        /// <summary>
        ///		SS 控制器
        /// </summary>
        public SSController pSSController = new SSController();

        /// <summary>
        ///     SSR 控制器
        /// </summary>
        public SSRController pSSRController = new SSRController();

        /// <summary>
        ///     V2Ray 控制器
        /// </summary>
        public VMessController pVMessController = new VMessController();

        /// <summary>
        ///		NF 控制器
        /// </summary>
        public NFController pNFController = new NFController();

        /// <summary>
        ///     HTTP 控制器
        /// </summary>
        public HTTPController pHTTPController = new HTTPController();


        /// <summary>
        ///     TUN/TAP 控制器
        /// </summary>
        public TUNTAPController pTUNTAPController = new TUNTAPController();

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
            var result = false;
            switch (server.Type)
            {
                case "Socks5":
                    result = true;
                    break;
                case "Shadowsocks":
                    KillProcess("Shadowsocks");
                    result = pSSController.Start(server, mode);
                    break;
                case "ShadowsocksR":
                    KillProcess("ShadowsocksR");
                    result = pSSRController.Start(server, mode);
                    break;
                case "VMess":
                    KillProcess("v2ray");
                    result = pVMessController.Start(server, mode);
                    break;
                default:
                    break;
            }

            if (result)
            {
                if (mode.Type == 0)
                {
                    // 进程代理模式，启动 NF 控制器
                    result = pNFController.Start(server, mode);
                }
                else if (mode.Type == 1)
                {
                    // TUN/TAP 全局代理模式，启动 TUN/TAP 控制器
                    result = pTUNTAPController.Start(server, mode);
                }
                else if (mode.Type == 2)
                {
                    // TUN/TAP 全局代理模式，启动 TUN/TAP 控制器
                    result = pTUNTAPController.Start(server, mode);
                }
                else if (mode.Type == 3 || mode.Type == 5)
                {
                    // HTTP 系统代理和 Socks5 和 HTTP 代理模式，启动 HTTP 控制器
                    result = pHTTPController.Start(server, mode);
                }
                else if (mode.Type == 4)
                {
                    // Socks5 代理模式，不需要启动额外的控制器
                }
                else
                {
                    result = false;
                }
            }

            if (!result)
            {
                Stop();
            }

            return result;
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            pSSController.Stop();
            pSSRController.Stop();
            pVMessController.Stop();
            pNFController.Stop();
            pHTTPController.Stop();
            pTUNTAPController.Stop();
        }

        public void KillProcess(String name) {
            Process[] processes = Process.GetProcessesByName(name);
            foreach (Process p in processes)
            {
                if (IsChildProcess(p, name))
                {
                    p.Kill();
                }
            }
        }

        private static bool IsChildProcess(Process process,string name)
        {
            bool result;
            try
            {
                string FileName = (Directory.GetCurrentDirectory() + "\\bin\\" + name + ".exe").ToLower();
                string procFileName = process.MainModule.FileName.ToLower();
                result = FileName.Equals(procFileName, StringComparison.Ordinal);
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.Message);
                result = false;
            }
            return result;
        }
    }
}

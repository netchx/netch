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
        public SSController pSSController = null;

        /// <summary>
        ///     SSR 控制器
        /// </summary>
        public SSRController pSSRController = null;

        /// <summary>
        ///     V2Ray 控制器
        /// </summary>
        public VMessController pVMessController = null;

        /// <summary>
        ///		NF 控制器
        /// </summary>
        public NFController pNFController = null;

        /// <summary>
        ///     HTTP 控制器
        /// </summary>
        public HTTPController pHTTPController = null;


        /// <summary>
        ///     TUN/TAP 控制器
        /// </summary>
        public TUNTAPController pTUNTAPController = null;

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Models.Server server, Models.Mode mode)
        {
            var result = false;
            switch (server.Type)
            {
                case "Socks5":
                    if (mode.Type == 4)
                    {
                        result = false;
                    }
                    else
                    {
                        result = true;
                    }
                    break;
                case "SS":
                    KillProcess("Shadowsocks");
                    if (pSSController == null)
                    {
                        pSSController = new SSController();
                    }
                    result = pSSController.Start(server, mode);
                    break;
                case "SSR":
                    KillProcess("ShadowsocksR");
                    if (pSSRController == null)
                    {
                        pSSRController = new SSRController();
                    }
                    result = pSSRController.Start(server, mode);
                    break;
                case "VMess":
                    KillProcess("v2ray");
                    if (pVMessController == null)
                    {
                        pVMessController = new VMessController();
                    }
                    result = pVMessController.Start(server, mode);
                    break;
                default:
                    break;
            }

            if (result)
            {
                if (mode.Type == 0)
                {
                    if (pNFController == null)
                    {
                        pNFController = new NFController();
                    }
                    // 进程代理模式，启动 NF 控制器
                    result = pNFController.Start(server, mode);
                }
                else if (mode.Type == 1)
                {
                    if (pTUNTAPController == null)
                    {
                        pTUNTAPController = new TUNTAPController();
                    }
                    // TUN/TAP 黑名单代理模式，启动 TUN/TAP 控制器
                    result = pTUNTAPController.Start(server, mode);
                }
                else if (mode.Type == 2)
                {
                    if (pTUNTAPController == null)
                    {
                        pTUNTAPController = new TUNTAPController();
                    }
                    // TUN/TAP 白名单代理模式，启动 TUN/TAP 控制器
                    result = pTUNTAPController.Start(server, mode);
                }
                else if (mode.Type == 3 || mode.Type == 5)
                {
                    if (pHTTPController == null)
                    {
                        pHTTPController = new HTTPController();
                    }
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
            if (pSSController != null)
            {
                pSSController.Stop();
            }
            else if (pSSRController != null)
            {
                pSSRController.Stop();
            }
            else if (pVMessController != null)
            {
                pVMessController.Stop();
            }
            
            if (pNFController != null)
            {
                pNFController.Stop();
            }
            else if (pTUNTAPController != null)
            {
                pTUNTAPController.Stop();
            }
            else if (pHTTPController != null)
            {
                pHTTPController.Stop();
            }
            
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

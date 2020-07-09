using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class MainController
    {
        /// <summary>
        ///     HTTP 控制器
        /// </summary>
        public HTTPController pHTTPController;

        /// <summary>
        ///     NF 控制器
        /// </summary>
        public NFController pNFController;

        /// <summary>
        ///     NTT 控制器
        /// </summary>
        public NTTController pNTTController;

        public ServerClient pServerClientController;


        /// <summary>
        ///     TUN/TAP 控制器
        /// </summary>
        public TUNTAPController pTUNTAPController;

        [DllImport("dnsapi", EntryPoint = "DnsFlushResolverCache")]
        public static extern uint FlushDNSResolverCache();

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Server server, Mode mode)
        {
            FlushDNSResolverCache();

            var result = false;
            if (server.Type == "Socks5")
            {
                result = mode.Type != 4;
            }
            else
            {
                switch (server.Type)
                {
                    case "SS":
                        pServerClientController = new SSController();
                        break;
                    case "SSR":
                        pServerClientController = new SSRController();
                        break;
                    case "VMess":
                        pServerClientController = new VMessController();
                        break;
                    case "Trojan":
                        pServerClientController = new TrojanController();
                        break;
                }

                MainForm.Instance.StatusText(i18N.Translate("Starting ", pServerClientController.MainName));
                if (pServerClientController.ready) result = pServerClientController.Start(server, mode);
            }

            if (result) // If server runs,then run mode
                switch (mode.Type)
                {
                    case 0: // 进程代理模式
                        pNFController ??= new NFController();
                        pNTTController ??= new NTTController();
                        if (pNFController.ready) result = pNFController.Start(server, mode, false);

                        if (!result && pNFController.ready)
                        {
                            MainForm.Instance.StatusText(i18N.Translate("Restarting Redirector"));
                            Logging.Info("正常启动失败后尝试停止驱动服务再重新启动");
                            //正常启动失败后尝试停止驱动服务再重新启动
                            result = pNFController.Start(server, mode, true);
                        }

                        if (result)
                            Task.Run(() => pNTTController.Start());
                        break;
                    case 1: // TUN/TAP 黑名单代理模式
                    case 2: // TUN/TAP 白名单代理模式
                        pTUNTAPController ??= new TUNTAPController();
                        pNTTController ??= new NTTController();
                        result = pTUNTAPController.Start(server, mode);
                        if (result)
                            Task.Run(() => pNTTController.Start());
                        break;
                    case 3: // HTTP 系统代理和 Socks5 和 HTTP 代理模式
                    case 5:
                        pHTTPController ??= new HTTPController();
                        result = pHTTPController.Start(server, mode);
                        break;
                    case 4: // Socks5 代理模式，不需要启动额外的Server
                        break;
                    default:
                        result = false;
                        break;
                }

            if (!result) Stop();

            return result;
        }

        /// <summary>
        ///     停止
        /// </summary>
        public void Stop()
        {
            pServerClientController?.Stop();

            if (pNFController != null)
                pNFController.Stop();
            else if (pTUNTAPController != null)
                pTUNTAPController.Stop();
            else
                pHTTPController?.Stop();

            pNTTController?.Stop();
        }

        public static Process GetProcess(string path = null)
        {
            var p = new Process
            {
                StartInfo =
                {
                    Arguments = "",
                    WorkingDirectory = $"{Global.NetchDir}\\bin",
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            if (path != null) p.StartInfo.FileName = Path.GetFullPath(path);
            return p;
        }

        public static void KillProcessByName(string name)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                    if (p.MainModule != null && p.MainModule.FileName.StartsWith(Global.NetchDir))
                        p.Kill();
            }
            catch (Win32Exception e)
            {
                Logging.Info($"结束进程 {name} 错误: " + e.Message);
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
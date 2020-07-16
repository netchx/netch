using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class MainController
    {
        public EncryptedProxy pEncryptedProxyController;

        public ModeController pModeController;

        /// <summary>
        ///     NTT 控制器
        /// </summary>
        public NTTController pNTTController = new NTTController();

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
                        pEncryptedProxyController = new SSController();
                        break;
                    case "SSR":
                        pEncryptedProxyController = new SSRController();
                        break;
                    case "VMess":
                        pEncryptedProxyController = new VMessController();
                        break;
                    case "Trojan":
                        pEncryptedProxyController = new TrojanController();
                        break;
                }

                Global.MainForm.StatusText(i18N.Translate("Starting ", pEncryptedProxyController.Name));
                if (pEncryptedProxyController.Ready) result = pEncryptedProxyController.Start(server, mode);
            }

            if (result)
            {
                Logging.Info("加密代理已启动");
                // 加密代理已启动
                switch (mode.Type)
                {
                    case 0: // 进程代理模式
                        pModeController = new NFController();
                        break;
                    case 1: // TUN/TAP 黑名单代理模式
                    case 2: // TUN/TAP 白名单代理模式
                        pModeController = new TUNTAPController();
                        break;
                    case 3:
                    case 5:
                        pModeController = new HTTPController();
                        break;
                    case 4: // Socks5 代理模式，不需要启动额外的Server
                        result = true;
                        break;
                }

                if (pModeController != null && pModeController.Ready)
                {
                    Global.MainForm.StatusText(i18N.Translate("Starting ", pModeController.Name));
                    result = pModeController.Start(server, mode);
                }

                if (result)
                {
                    Logging.Info("模式已启动");
                    switch (mode.Type)
                    {
                        case 0:
                        case 1:
                        case 2:
                            Task.Run(() =>
                            {
                                Global.MainForm.NatTypeStatusText(i18N.Translate("Starting NatTester"));
                                // Thread.Sleep(1000);
                                var (nttResult, natType, localEnd, publicEnd) = pNTTController.Start();
                                var country = Utils.Utils.GetCityCode(publicEnd);

                                if (nttResult) Global.MainForm.NatTypeStatusText(natType, country);
                            });
                            break;
                    }
                }
            }

            if (!result) Stop();

            return result;
        }

        /// <summary>
        ///     停止
        /// </summary>
        public void Stop()
        {
            pEncryptedProxyController?.Stop();
            pModeController?.Stop();
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
                Logging.Error($"结束进程 {name} 错误：\n" + e);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
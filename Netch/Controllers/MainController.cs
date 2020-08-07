using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class MainController
    {
        /// <summary>
        ///     记录当前使用的端口
        ///     <see cref="MainForm.LocalPortText"/>
        /// </summary>
        public static readonly List<int> UsingPorts = new List<int>();

        public EncryptedProxy pEncryptedProxyController;

        public ModeController pModeController;

        private Server _savedServer;
        private Mode _savedMode;

        public string PortInfo
        {
            get
            {
                if (_savedMode == null || _savedServer == null)
                    return string.Empty;

                var text = new StringBuilder();
                if (_savedServer.Type == "Socks5" && _savedMode.Type != 3 && _savedMode.Type != 5)
                    // 不可控Socks5, 不可控HTTP
                    return string.Empty;

                if (_localAddress == "0.0.0.0")
                    text.Append(i18N.Translate("Allow other Devices to connect") + " ");

                if (_savedServer.Type != "Socks5")
                    // 可控Socks5
                    text.Append($"Socks5 {i18N.Translate("Local Port", ": ")}{_socks5Port}");

                if (_savedMode.Type == 3 || _savedMode.Type == 5)
                    // 有HTTP
                    text.Append($" | HTTP {i18N.Translate("Local Port", ": ")}{_httpPort}");

                return $" ({text})";
            }
        }

        /// <summary>
        ///     NTT 控制器
        /// </summary>
        public NTTController pNTTController = new NTTController();

        private string _localAddress;
        private int _redirectorTCPPort;
        private int _httpPort;
        private int _socks5Port;

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
            Logging.Info($"启动主控制器: {server.Type} [{mode.Type}]{mode.Remark}");

            #region Record Settings

            _httpPort = Global.Settings.HTTPLocalPort;
            _socks5Port = Global.Settings.Socks5LocalPort;
            _redirectorTCPPort = Global.Settings.RedirectorTCPPort;
            _localAddress = Global.Settings.LocalAddress;
            _savedServer = server;
            _savedMode = mode;

            #endregion

            FlushDNSResolverCache();

            bool result;
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

                KillProcessByName(pEncryptedProxyController.MainFile);

                // 检查端口是否被占用
                if (PortHelper.PortInUse(Global.Settings.Socks5LocalPort))
                {
                    MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", "Socks5"));
                    return false;
                }

                if (PortHelper.PortInUse(Global.Settings.HTTPLocalPort))
                {
                    MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", "HTTP"));
                    return false;
                }

                if (PortHelper.PortInUse(Global.Settings.RedirectorTCPPort, PortType.TCP))
                {
                    MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", "Redirector TCP"));
                    return false;
                }

                Global.MainForm.StatusText(i18N.Translate("Starting ", pEncryptedProxyController.Name));
                result = pEncryptedProxyController.Start(server, mode);
            }

            if (result)
            {
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

                if (pModeController != null)
                {
                    Global.MainForm.StatusText(i18N.Translate("Starting ", pModeController.Name));
                    result = pModeController.Start(server, mode);
                }

                if (result)
                {
                    #region Add UsingPorts

                    switch (mode.Type)
                    {
                        // 成功启动
                        case 3:
                        case 5:
                            UsingPorts.Add(Global.Settings.HTTPLocalPort);
                            break;
                        case 0:
                            UsingPorts.Add(Global.Settings.RedirectorTCPPort);
                            break;
                    }

                    if (server.Type != "Socks5")
                        UsingPorts.Add(Global.Settings.Socks5LocalPort);

                    #endregion

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

            if (!result)
            {
                Logging.Error("主控制器启动失败");
                Stop();
            }

            return result;
        }

        /// <summary>
        ///     停止
        /// </summary>
        public async void Stop()
        {
            await Task.WhenAll(new[]
            {
                Task.Run(() => pEncryptedProxyController?.Stop()),
                Task.Run(() => UsingPorts.Clear()),
                Task.Run(() => pModeController?.Stop()),
                Task.Run(() => pNTTController.Stop())
            });
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
                Logging.Error($"结束进程 {name} 错误：" + e.Message);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
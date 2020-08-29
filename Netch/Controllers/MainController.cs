using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

        public EncryptedProxy pEncryptedProxyController { get; private set; }

        public ModeController pModeController { get; private set; }

        private Server _savedServer;
        private Mode _savedMode;

        public string PortInfo
        {
            get
            {
                if (_savedMode == null || _savedServer == null)
                    return string.Empty;

                if (_savedServer.Type == "Socks5" && _savedMode.Type != 3 && _savedMode.Type != 5)
                    // 不可控Socks5, 不可控HTTP
                    return string.Empty;

                var text = new StringBuilder();
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
        public async Task<bool> Start(Server server, Mode mode)
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
            _ = Task.Run(Firewall.AddNetchFwRules);

            bool result;
            if (server.Type == "Socks5")
            {
                result = mode.Type != 4;
            }
            else
            {
                pEncryptedProxyController = server.Type switch
                {
                    "SS" => new SSController(),
                    "SSR" => new SSRController(),
                    "VMess" => new VMessController(),
                    "Trojan" => new TrojanController(),
                    _ => pEncryptedProxyController
                };

                KillProcessByName(pEncryptedProxyController.MainFile);

                // 检查端口是否被占用
                var isPortNotAvailable = false;
                if (_savedServer.Type != "Socks5")
                {
                    isPortNotAvailable |= PortCheckAndShowMessageBox(_socks5Port, "Socks5");
                }

                switch (_savedMode.Type)
                {
                    case 0:
                        isPortNotAvailable |= PortCheckAndShowMessageBox(_redirectorTCPPort, "Redirector TCP");
                        break;
                    case 3:
                    case 5:
                        isPortNotAvailable |= PortCheckAndShowMessageBox(_httpPort, "HTTP");
                        break;
                }

                if (isPortNotAvailable)
                {
                    Logging.Error("主控制器启动失败: 端口被占用");
                    return false;
                }

                Global.MainForm.StatusText(i18N.Translate("Starting ", pEncryptedProxyController.Name));
                try
                {
                    result = await Task.Run(() => pEncryptedProxyController.Start(server, mode));
                }
                catch (Exception e)
                {
                    Logging.Error("加密代理启动失败，未处理异常: " + e);
                    result = false;
                }
            }

            if (result)
            {
                // 加密代理成功启动
                UsingPorts.Add(pEncryptedProxyController?.Socks5LocalPort ?? server.Port); // 记录Socks5使用端口

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
                    try
                    {
                        result = await Task.Run(() => pModeController.Start(server, mode));
                    }
                    catch (Exception e)
                    {
                        if (e is DllNotFoundException || e is FileNotFoundException)
                            MessageBoxX.Show(e.Message + "\n\n" + i18N.Translate("Missing File or runtime components"), owner: Global.MainForm);
                        else
                            Logging.Error("模式启动失败，未处理异常: " + e);
                        result = false;
                    }
                }

                if (result)
                {
                    // 成功启动

                    switch (mode.Type) // 记录使用端口
                    {
                        case 0:
                            UsingPorts.Add(_redirectorTCPPort);
                            break;
                        case 3:
                        case 5:
                            UsingPorts.Add(_httpPort);
                            break;
                    }

                    switch (mode.Type)
                    {
                        case 0:
                        case 1:
                        case 2:
                            _ = Task.Run(() =>
                            {
                                Global.MainForm.NatTypeStatusText(i18N.Translate("Starting NatTester"));
                                // Thread.Sleep(1000);
                                var (nttResult, natType, localEnd, publicEnd) = pNTTController.Start();

                                if (nttResult)
                                {
                                    var country = Utils.Utils.GetCityCode(publicEnd);
                                    Global.MainForm.NatTypeStatusText(natType, country);
                                }
                                else
                                    Global.MainForm.NatTypeStatusText(natType);
                            });
                            break;
                    }
                }
            }

            if (!result)
            {
                Logging.Error("主控制器启动失败");
                try
                {
                    await Stop();
                }
                catch
                {
                    // ignored
                }
            }

            return result;
        }

        /// <summary>
        ///     重测NAT
        /// </summary>
        public void RetryNatTest()
        {
            _ = Task.Run(() =>
            {
                Global.MainForm.NatTypeStatusText(i18N.Translate("Starting NatTester"));
                // Thread.Sleep(1000);
                var (nttResult, natType, localEnd, publicEnd) = pNTTController.Start();
                var country = Utils.Utils.GetCityCode(publicEnd);

                if (nttResult) Global.MainForm.NatTypeStatusText(natType, country);
            });
        }

        /// <summary>
        ///     停止
        /// </summary>
        public async Task Stop()
        {
            var tasks = new Task[]
            {
                Task.Run(() => pEncryptedProxyController?.Stop()),
                Task.Run(() => UsingPorts.Clear()),
                Task.Run(() => pModeController?.Stop()),
                Task.Run(() => pNTTController.Stop())
            };
            await Task.WhenAll(tasks);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="portName"></param>
        /// <param name="portType"></param>
        /// <returns>端口是否被占用</returns>
        private static bool PortCheckAndShowMessageBox(int port, string portName, PortType portType = PortType.Both)
        {
            if (!PortHelper.PortInUse(port, portType)) return false;
            MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", portName));
            return true;
        }
    }
}
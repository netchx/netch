using System;
using System.IO;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Utils;
using static Netch.Forms.MainForm;
using static Netch.Utils.PortHelper;

namespace Netch.Controllers
{
    public static class MainController
    {
        public static EncryptedProxy EncryptedProxyController { get; private set; }
        public static ModeController ModeController { get; private set; }

        public static bool NttTested;

        private static readonly NTTController NTTController = new NTTController();

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public static async Task<bool> Start(Server server, Mode mode)
        {
            Logging.Info($"启动主控制器: {server.Type} [{mode.Type}]{mode.Remark}");

            if (server.Type == "Socks5" && mode.Type == 4)
            {
                return false;
            }

            NativeMethods.FlushDNSResolverCache();

            if (!Utils.Utils.SearchOutboundAdapter())
            {
                MessageBoxX.Show("No internet connection");
                return false;
            }

            _ = Task.Run(Firewall.AddNetchFwRules);

            try
            {
                if (!await StartServer(server, mode))
                {
                    throw new StartFailedException();
                }

                if (!await StartMode(server, mode))
                {
                    throw new StartFailedException();
                }

                // 成功启动
                switch (mode.Type)
                {
                    case 0:
                    case 1:
                    case 2:
                        NatTest();
                        break;
                }

                return true;
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case DllNotFoundException _:
                    case FileNotFoundException _:
                        MessageBoxX.Show(e.Message + "\n\n" + i18N.Translate("Missing File or runtime components"), owner: Global.MainForm);
                        break;
                    case StartFailedException _:
                    case PortInUseException _:
                        break;
                    default:
                        Logging.Error($"主控制器未处理异常: {e}");
                        break;
                }

                try
                {
                    await Stop();
                }
                catch
                {
                    // ignored
                }

                return false;
            }
        }

        private static async Task<bool> StartServer(Server server, Mode mode)
        {
            switch (server.Type)
            {
                case "Socks5":
                    return true;
                case "SS":
                    EncryptedProxyController = new SSController();
                    break;
                case "SSR":
                    EncryptedProxyController = new SSRController();
                    break;
                case "VMess":
                    EncryptedProxyController = new VMessController();
                    break;
                case "Trojan":
                    EncryptedProxyController = new TrojanController();
                    break;
                default:
                    Logging.Error("未知服务器类型");
                    return false;
            }

            Utils.Utils.KillProcessByName(EncryptedProxyController.MainFile);
            PortCheckAndShowMessageBox(Global.Settings.Socks5LocalPort, "Socks5");

            Global.MainForm.StatusText(i18N.Translate("Starting ", EncryptedProxyController.Name));
            if (await Task.Run(() => EncryptedProxyController.Start(server, mode)))
            {
                UsingPorts.Add(StatusPortInfoText.Socks5Port = Global.Settings.Socks5LocalPort);
                StatusPortInfoText.ShareLan = Global.Settings.LocalAddress == "0.0.0.0";
                return true;
            }

            return false;
        }

        private static async Task<bool> StartMode(Server server, Mode mode)
        {
            var port = 0;
            switch (mode.Type)
            {
                case 0:
                    ModeController = new NFController();
                    PortCheckAndShowMessageBox(port = Global.Settings.RedirectorTCPPort, "Redirector TCP");
                    break;
                case 1:
                case 2:
                    ModeController = new TUNTAPController();
                    break;
                case 3:
                case 5:
                    ModeController = new HTTPController();
                    PortCheckAndShowMessageBox(port = Global.Settings.HTTPLocalPort, "HTTP");
                    break;
                case 4:
                    return true;
                default:
                    Logging.Error("未知模式类型");
                    return false;
            }

            Global.MainForm.StatusText(i18N.Translate("Starting ", ModeController.Name));
            if (await Task.Run(() => ModeController.Start(server, mode)))
            {
                UsingPorts.Add(port);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     停止
        /// </summary>
        public static async Task Stop()
        {
            UsingPorts.Clear();

            _ = Task.Run(() => NTTController.Stop());

            var tasks = new[]
            {
                Task.Run(() => EncryptedProxyController?.Stop()),
                Task.Run(() => ModeController?.Stop()),
            };
            await Task.WhenAll(tasks);
        }


        /// <summary>
        ///     检查端口是否被占用, 
        ///     被占用则弹窗提示, 确认后抛出异常
        /// </summary>
        /// <param name="port">检查的端口</param>
        /// <param name="portName">端口用途名称</param>
        /// <param name="portType"></param>
        /// <exception cref="PortInUseException"></exception>
        private static void PortCheckAndShowMessageBox(int port, string portName, PortType portType = PortType.Both)
        {
            if (PortInUse(port, portType))
            {
                MessageBoxX.Show(i18N.TranslateFormat("The {0} port is in use.", $"{portName} ({port})"));
                throw new PortInUseException();
            }
        }

        /// <summary>
        ///     测试 NAT
        /// </summary>
        public static void NatTest()
        {
            NttTested = false;
            Task.Run(() =>
            {
                Global.MainForm.NatTypeStatusText(i18N.Translate("Starting NatTester"));
                // Thread.Sleep(1000);
                var (result, localEnd, publicEnd) = NTTController.Start();

                if (!string.IsNullOrEmpty(publicEnd))
                {
                    var country = Utils.Utils.GetCityCode(publicEnd);
                    Global.MainForm.NatTypeStatusText(result, country);
                }
                else
                    Global.MainForm.NatTypeStatusText(result ?? "Error");

                NttTested = true;
            });
        }
    }

    public class StartFailedException : Exception
    {
    }
}
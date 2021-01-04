using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;
using static Netch.Forms.MainForm;
using static Netch.Utils.PortHelper;

namespace Netch.Controllers
{
    public static class MainController
    {
        public static IServerController ServerController
        {
            get => _serverController;
            private set => _serverController = value;
        }

        public static IServerController UdpServerController
        {
            get => _udpServerController ?? _serverController;
            set => _udpServerController = value;
        }
        public static Mode Mode;

        /// TCP or Both Server
        public static Server Server;

        public static Server UdpServer
        {
            get => _udpServer ?? Server;
            set => _udpServer = value;
        }

        private static Server _udpServer;
        public static IModeController ModeController { get; private set; }

        public static bool NttTested;

        private static readonly NTTController NTTController = new NTTController();
        private static IServerController _serverController;
        private static IServerController _udpServerController;

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public static async Task<bool> Start(Server server, Mode mode)
        {
            Logging.Info($"启动主控制器: {server.Type} [{mode.Type}]{mode.Remark}");
            Server = server;
            Mode = mode;

            if (server is Socks5 && mode.Type == 4)
            {
                return false;
            }

            // 刷新DNS缓存
            NativeMethods.FlushDNSResolverCache();

            try
            {
                WebUtil.BestLocalEndPoint(new IPEndPoint(0x72727272, 53));
            }
            catch (Exception)
            {
                MessageBoxX.Show("No internet connection");
                return false;
            }

            if (Global.Settings.ResolveServerHostname && DNS.Lookup(server.Hostname) == null)
            {
                MessageBoxX.Show("Lookup Server hostname failed");
                return false;
            }

            // 添加Netch到防火墙
            _ = Task.Run(Firewall.AddNetchFwRules);

            try
            {
                if (!ModeHelper.SkipServerController(server, mode))
                {
                    if (!await Task.Run(() => StartServer(server, mode, ref _serverController)))
                    {
                        throw new StartFailedException();
                    }

                    StatusPortInfoText.UpdateShareLan();
                }

                if (!await StartMode(mode))
                {
                    throw new StartFailedException();
                }

                if (mode.TestNatRequired())
                    NatTest();

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

        private static bool StartServer(Server server, Mode mode, ref IServerController controller)
        {
            controller = ServerHelper.GetUtilByTypeName(server.Type).GetController();

            if (controller is Guard instanceController)
            {
                Utils.Utils.KillProcessByName(instanceController.MainFile);
            }

            PortCheckAndShowMessageBox(controller.Socks5LocalPort(), "Socks5");

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", controller.Name));
            if (controller.Start(in server, mode))
            {
                if (controller is Guard guard)
                {
                    if (guard.Instance != null)
                    {
                        Task.Run(() =>
                        {
                            Thread.Sleep(1000);
                            Global.Job.AddProcess(guard.Instance);
                        });
                    }
                }

                if (server is Socks5 socks5)
                {
                    if (socks5.Auth())
                        UsingPorts.Add(StatusPortInfoText.Socks5Port = controller.Socks5LocalPort());
                }
                else
                {
                    UsingPorts.Add(StatusPortInfoText.Socks5Port = controller.Socks5LocalPort());
                }

                return true;
            }

            return false;
        }

        private static async Task<bool> StartMode(Mode mode)
        {
            ModeController = ModeHelper.GetModeControllerByType(mode.Type, out var port, out var portName, out var portType);

            if (ModeController == null)
            {
                return true;
            }

            if (port != null)
            {
                PortCheckAndShowMessageBox((ushort) port, portName, portType);
                UsingPorts.Add((ushort) port);
            }

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", ModeController.Name));
            if (await Task.Run(() => ModeController.Start(mode)))
            {
                if (ModeController is Guard guard)
                {
                    if (guard.Instance != null)
                    {
                        Global.Job.AddProcess(guard.Instance);
                    }
                }

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
            StatusPortInfoText.Reset();

            _ = Task.Run(() => NTTController.Stop());

            var tasks = new[]
            {
                Task.Run(() => ServerController?.Stop()),
                Task.Run(() => ModeController?.Stop()),
            };
            await Task.WhenAll(tasks);
            ModeController = null;
            ServerController = null;
        }


        /// <summary>
        ///     检查端口是否被占用, 
        ///     被占用则弹窗提示, 确认后抛出异常
        /// </summary>
        /// <param name="port">检查的端口</param>
        /// <param name="portName">端口用途名称</param>
        /// <param name="portType"></param>
        /// <exception cref="PortInUseException"></exception>
        public static void PortCheckAndShowMessageBox(ushort port, string portName, PortType portType = PortType.Both)
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
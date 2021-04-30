using System;
using System.IO;
using System.Threading.Tasks;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;

namespace Netch.Controllers
{
    public static class MainController
    {
        public static Mode? Mode;

        /// TCP or Both Server
        public static Server? Server;

        private static Server? _udpServer;

        public static readonly NTTController NTTController = new();
        private static IServerController? _serverController;
        private static IServerController? _udpServerController;

        public static IServerController? ServerController
        {
            get => _serverController;
            private set => _serverController = value;
        }

        public static IServerController? UdpServerController
        {
            get => _udpServerController ?? _serverController;
            set => _udpServerController = value;
        }

        public static Server? UdpServer
        {
            get => _udpServer ?? Server;
            set => _udpServer = value;
        }

        public static IModeController? ModeController { get; private set; }

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        /// <exception cref="MessageException"></exception>
        public static async Task StartAsync(Server server, Mode mode)
        {
            await Task.Run(() => Start(server, mode));
        }

        public static void Start(Server server, Mode mode)
        {
            Global.Logger.Info($"启动主控制器: {server.Type} [{mode.Type}]{mode.Remark}");
            Server = server;
            Mode = mode;

            // 刷新 DNS 缓存
            NativeMethods.RefreshDNSCache();

            if (DnsUtils.Lookup(server.Hostname) == null)
                throw new MessageException(i18N.Translate("Lookup Server hostname failed"));

            // 添加 Netch 到防火墙
            Firewall.AddNetchFwRules();

            try
            {
                if (!ModeHelper.SkipServerController(server, mode))
                {
                    StartServer(server, mode, out _serverController);
                    StatusPortInfoText.UpdateShareLan();
                }

                StartMode(mode);
            }
            catch (Exception e)
            {
                Stop();

                switch (e)
                {
                    case DllNotFoundException:
                    case FileNotFoundException:
                        throw new Exception(e.Message + "\n\n" + i18N.Translate("Missing File or runtime components"));
                    case MessageException:
                        throw;
                    default:
                        Global.Logger.Error(e.ToString());
                        Global.Logger.ShowLog();
                        throw new MessageException($"未处理异常\n{e.Message}");
                }
            }
        }

        private static void StartServer(Server server, Mode mode, out IServerController controller)
        {
            controller = ServerHelper.GetUtilByTypeName(server.Type).GetController();

            TryReleaseTcpPort(controller.Socks5LocalPort(), "Socks5");

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", controller.Name));

            controller.Start(in server, mode);

            if (server is Socks5 socks5)
            {
                if (socks5.Auth())
                    StatusPortInfoText.Socks5Port = controller.Socks5LocalPort();
            }
            else
            {
                StatusPortInfoText.Socks5Port = controller.Socks5LocalPort();
            }
        }

        private static void StartMode(Mode mode)
        {
            ModeController = ModeHelper.GetModeControllerByType(mode.Type, out var port, out var portName);

            if (port != null)
                TryReleaseTcpPort((ushort)port, portName);

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", ModeController.Name));

            ModeController.Start(mode);
        }

        public static async Task StopAsync()
        {
            await Task.Run(Stop);
        }

        /// <summary>
        ///     停止
        /// </summary>
        public static void Stop()
        {
            if (_serverController == null && ModeController == null)
                return;

            StatusPortInfoText.Reset();

            _ = Task.Run(() => NTTController.Stop());

            var tasks = new[]
            {
                Task.Run(() => ServerController?.Stop()),
                Task.Run(() => ModeController?.Stop())
            };

            try
            {
                Task.WaitAll(tasks);
            }
            catch (Exception e)
            {
                Global.Logger.Error(e.ToString());
                Global.Logger.ShowLog();
            }

            ModeController = null;
            ServerController = null;
        }

        public static void PortCheck(ushort port, string portName, PortType portType = PortType.Both)
        {
            try
            {
                PortHelper.CheckPort(port, portType);
            }
            catch (PortInUseException)
            {
                throw new MessageException(i18N.TranslateFormat("The {0} port is in use.", $"{portName} ({port})"));
            }
            catch (PortReservedException)
            {
                throw new MessageException(i18N.TranslateFormat("The {0} port is reserved by system.", $"{portName} ({port})"));
            }
        }

        public static void TryReleaseTcpPort(ushort port, string portName)
        {
            foreach (var p in PortHelper.GetProcessByUsedTcpPort(port))
            {
                var fileName = p.MainModule?.FileName;
                if (fileName == null)
                    continue;

                if (fileName.StartsWith(Global.NetchDir))
                {
                    p.Kill();
                    p.WaitForExit();
                }
                else
                {
                    throw new MessageException(i18N.TranslateFormat("The {0} port is used by {1}.", $"{portName} ({port})", $"({p.Id}){fileName}"));
                }
            }

            PortCheck(port, portName, PortType.TCP);
        }
    }
}
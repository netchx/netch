using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;
using static Netch.Utils.PortHelper;

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
            Logging.Info($"启动主控制器: {server.Type} [{mode.Type}]{mode.Remark}");
            Server = server;
            Mode = mode;

            if (server is Socks5 && mode.Type == 4)
                throw new MessageException("Already Socks5 Server");

            // 刷新DNS缓存
            NativeMethods.FlushDNSResolverCache();

            if (DnsUtils.Lookup(server.Hostname) == null)
                throw new MessageException(i18N.Translate("Lookup Server hostname failed"));

            // 添加Netch到防火墙
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
                        Logging.Error(e.ToString());
                        Utils.Utils.Open(Logging.LogFile);
                        throw new MessageException($"未处理异常\n{e.Message}");
                }
            }
        }

        private static void StartServer(Server server, Mode mode, out IServerController controller)
        {
            controller = ServerHelper.GetUtilByTypeName(server.Type).GetController();

            if (controller is Guard instanceController)
                Utils.Utils.KillProcessByName(instanceController.MainFile);

            PortCheck(controller.Socks5LocalPort(), "Socks5");

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", controller.Name));

            controller.Start(in server, mode);
            if (controller is Guard {Instance: { }} guard)
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    Global.Job.AddProcess(guard.Instance!);
                });

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
            ModeController = ModeHelper.GetModeControllerByType(mode.Type, out var port, out var portName, out var portType);

            if (ModeController == null)
                return;

            if (port != null)
                PortCheck((ushort) port, portName, portType);

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", ModeController.Name));

            ModeController.Start(mode);
            if (ModeController is Guard {Instance: { }} guard)
                Global.Job.AddProcess(guard.Instance!);
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
                Logging.Error(e.ToString());
                Utils.Utils.Open(Logging.LogFile);
            }

            ModeController = null;
            ServerController = null;
        }

        public static void PortCheck(ushort port, string portName, PortType portType = PortType.Both)
        {
            try
            {
                CheckPort(port, portType);
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
    }

    public class MessageException : Exception
    {
        public MessageException()
        {
        }

        public MessageException(string message) : base(message)
        {
        }
    }
}
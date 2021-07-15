using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;
using Serilog;
using Serilog.Events;

namespace Netch.Controllers
{
    public static class MainController
    {
        public static Mode? Mode { get; private set; }

        public static readonly NTTController NTTController = new();

        public static IServerController? ServerController { get; private set; }

        public static IModeController? ModeController { get; private set; }

        public static async Task StartAsync(Server server, Mode mode)
        {
            Log.Information("Start MainController: {Server} {Mode}", $"{server.Type}", $"[{(int)mode.Type}]{mode.Remark}");

            if (await DnsUtils.LookupAsync(server.Hostname) == null)
                throw new MessageException(i18N.Translate("Lookup Server hostname failed"));

            Mode = mode;

            await Task.WhenAll(
                Task.Run(NativeMethods.RefreshDNSCache),
                Task.Run(Firewall.AddNetchFwRules)
            );

            if (Log.IsEnabled(LogEventLevel.Debug))
                Task.Run(() =>
                    {
                        // TODO log level setting
                        Log.Debug("Running Processes: \n{Processes}", string.Join("\n", SystemInfo.Processes(false)));
                    })
                    .Forget();

            try
            {
                if (!ModeHelper.SkipServerController(server, mode))
                    server = await StartServerAsync(server);

                await StartModeAsync(server, mode);
            }
            catch (Exception e)
            {
                await StopAsync();

                switch (e)
                {
                    case DllNotFoundException:
                    case FileNotFoundException:
                        throw new Exception(e.Message + "\n\n" + i18N.Translate("Missing File or runtime components"));
                    case MessageException:
                        throw;
                    default:
                        Log.Error(e, "Unhandled Exception When Start MainController");
                        Utils.Utils.Open(Constants.LogFile);
                        throw new MessageException($"{i18N.Translate("Unhandled Exception")}\n{e.Message}");
                }
            }
        }

        private static async Task<Server> StartServerAsync(Server server)
        {
            ServerController = ServerHelper.GetUtilByTypeName(server.Type).GetController();

            TryReleaseTcpPort(ServerController.Socks5LocalPort(), "Socks5");

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", ServerController.Name));

            Log.Debug("Server Information: {Data}", $"{server.Type} {server.MaskedData()}");
            var socks5 = await ServerController.StartAsync(server);

            StatusPortInfoText.Socks5Port = socks5.Port;
            StatusPortInfoText.UpdateShareLan();

            return socks5;
        }

        private static async Task StartModeAsync(Server server, Mode mode)
        {
            ModeController = ModeHelper.GetModeControllerByType(mode.Type, out var port, out var portName);

            if (port != null)
                TryReleaseTcpPort((ushort)port, portName);

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", ModeController.Name));

            await ModeController.StartAsync(server, mode);
        }

        public static async Task StopAsync()
        {
            if (ServerController == null && ModeController == null)
                return;

            Log.Information("Stop Main Controller");
            StatusPortInfoText.Reset();

            Task.Run(() => NTTController.StopAsync()).Forget();

            var tasks = new[]
            {
                Task.Run(() => ServerController?.StopAsync()),
                Task.Run(() => ModeController?.StopAsync())
            };

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Log.Error(e, "MainController Stop Error");
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
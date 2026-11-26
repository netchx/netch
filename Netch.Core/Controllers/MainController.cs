using System.ComponentModel;
using Microsoft.VisualStudio.Threading;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;
using System.Diagnostics;

namespace Netch.Controllers;

public static class MainController
{
    public static SocksServer? Socks5Server { get; private set; }

    public static Server? Server { get; private set; }

    public static Mode? Mode { get; private set; }

    public static IServerController? ServerController { get; private set; }

    public static IModeController? ModeController { get; private set; }

    private static readonly AsyncSemaphore Lock = new(1);

    public static void EnsureUdpCompatibility(Server server, Mode? mode)
    {
        if (!ModeRequiresUdp(mode) || ServerSupportsUdp(server))
            return;

        throw new MessageException(i18N.TranslateFormat(
            "The selected {0} node does not support UDP relay required by this mode. Enable UDP over TCP or switch to a UDP-capable node.",
            ServerHelper.GetDisplayName(server.ConfigType)));
    }

    private static bool ServerSupportsUdp(Server server) =>
        server.ConfigType switch
        {
            EConfigType.HTTP or EConfigType.SSH or EConfigType.Naive => false,
            EConfigType.ShadowTLS => server.ProtoExtra.Uot == true,
            _ => true
        };

    private static bool ModeRequiresUdp(Mode? mode) =>
        mode switch
        {
            null => false,
            Models.Modes.ProcessMode.Redirector pm =>
                (pm.FilterUDP ?? Global.Settings.Redirector.FilterUDP)
                || ((pm.FilterDNS ?? Global.Settings.Redirector.FilterDNS)
                    && (pm.DNSProxy ?? Global.Settings.Redirector.DNSProxy)),
            Models.Modes.TunMode.TunMode or Models.Modes.ShareMode.ShareMode => true,
            _ => false
        };

    public static async Task StartAsync(Server server, Mode? mode = null)
    {
        using var releaser = await Lock.EnterAsync();

        Log.Information("Start MainController: {Server} {Mode}", $"{server.ConfigType}", mode == null ? "Null" : $"[{(int)mode.Type}]{mode.i18NRemark}");

        EnsureUdpCompatibility(server, mode);

        if (await DnsUtils.LookupAsync(server.Address) == null)
            throw new MessageException(i18N.Translate("Lookup Server hostname failed"));

        // TODO Disable NAT Type Test setting
        // cache STUN Server ip to prevent "Wrong STUN Server"
        DnsUtils.LookupAsync(Global.Settings.STUN_Server).Forget();

        try
        {
            await StopCoreAsync();

            Server = server;
            Mode = mode;

            await Task.WhenAll(Task.Run(NativeMethods.RefreshDNSCache), Task.Run(Firewall.AddNetchFwRules));

            if (mode != null)
            {
                ModeController = ModeService.GetModeControllerByType(mode.Type, out var modePort, out var portName);


                if (modePort != null)
                    TryReleaseTcpPort((ushort)modePort, portName);
            }

            //如果是 Socks5 服务器且没有密码
            //或者如果是 Socks5 服务器，且模式控制器支持 Socks5（且不需要本地 HTTP 混合端口）则直接使用该服务器
            var requireLocalHttp = ModeController?.Features.HasFlag(ModeFeature.RequireLocalHttpProxy) == true;
            if (!requireLocalHttp && Server is SocksServer socks5 && socks5.ProtoExtra.Uot != true &&
                (ModeController == null ? !socks5.Auth() : (!socks5.Auth() || ModeController.Features.HasFlag(ModeFeature.SupportSocks5Auth))))
            {

                Socks5Server = socks5;
            }
            else
            {
                // Start Server Controller to get a local socks5 server
                Log.Debug("Server Information: {Data}", $"{server.ConfigType} {server.MaskedData()}");

                ServerController = CoreSelector.CreateController(server);
                AppEvents.ReportStatus(i18N.TranslateFormat("Starting {0}", ServerController.Name));

                TryReleaseTcpPort(ServerController.Socks5LocalPort(), "Socks5");
                Socks5Server = await ServerController.StartAsync(server);

                StatusPortInfoText.Socks5Port = (ushort)Socks5Server.Port;
                StatusPortInfoText.UpdateShareLan();
            }

            // Start Mode Controller
            if (mode != null && ModeController != null && Socks5Server != null)
            {
                AppEvents.ReportStatus(i18N.TranslateFormat("Starting {0}", ModeController.Name));
                await ModeController.StartAsync(Socks5Server, mode);

                // Process mode forwards game traffic on Redirector threads inside
                // this process; keep it responsive while games load the CPU.
                using var current = Process.GetCurrentProcess();
                ProcessTuning.Boost(current, "Netch");
            }
        }
        catch (Exception e)
        {
            await StopCoreAsync();

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

    public static async Task StopAsync()
    {
        using var _ = await Lock.EnterAsync();
        await StopCoreAsync();
    }

    private static async Task StopCoreAsync()
    {
        if (ServerController == null && ModeController == null && Socks5Server == null && Server == null && Mode == null)
            return;

        Log.Information("Stop Main Controller");
        StatusPortInfoText.Reset();

        // Stop mode/driver consumers before tearing down the local proxy core.
        if (ModeController != null)
        {
            try
            {
                await ModeController.StopAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "MainController Stop Mode Error");
            }
        }

        if (ServerController != null)
        {
            try
            {
                await ServerController.StopAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "MainController Stop Server Error");
            }
        }

        ModeController = null;
        ServerController = null;
        Socks5Server = null;
        Server = null;
        Mode = null;

        using var current = Process.GetCurrentProcess();
        ProcessTuning.Restore(current);
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
        var netchRoot = Path.GetFullPath(Global.NetchDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var p in PortHelper.GetProcessByUsedTcpPort(port))
        {
            using (p)
            {
                if (p.Id == Environment.ProcessId)
                    continue;

                string? fileName = null;
                try
                {
                    fileName = p.MainModule?.FileName;
                }
                catch (Win32Exception)
                {
                    // System/protected processes may deny MainModule access.
                }
                catch (InvalidOperationException)
                {
                    // Process exited between enumeration and MainModule access.
                    continue;
                }

                if (fileName == null)
                    continue;

                var fullFileName = Path.GetFullPath(fileName);
                var relative = Path.GetRelativePath(netchRoot, fullFileName);
                var isOwned = !Path.IsPathRooted(relative)
                              && relative != ".."
                              && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                              && !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
                if (isOwned)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill(entireProcessTree: true);
                            p.WaitForExit(5000);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process already exited.
                    }
                }
                else
                {
                    throw new MessageException(i18N.TranslateFormat("The {0} port is used by {1}.", $"{portName} ({port})", $"({p.Id}){fileName}"));
                }
            }
        }

        PortCheck(port, portName, PortType.TCP);
    }

    public static Task<NatTypeTestResult> DiscoveryNatTypeAsync(CancellationToken ctx = default)
    {
        Debug.Assert(Socks5Server != null, nameof(Socks5Server) + " != null");
        return Socks5ServerTestUtils.DiscoveryNatTypeAsync(Socks5Server, ctx);
    }

    public static async Task<int?> HttpConnectAsync(CancellationToken ctx = default)
    {
        Debug.Assert(Socks5Server != null, nameof(Socks5Server) + " != null");
        try
        {
            // Await here: returning the task directly meant the catch blocks
            // below never saw asynchronous failures.
            return await Socks5ServerTestUtils.HttpConnectAsync(Socks5Server, ctx);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            Log.Warning(e, "Unhandled Socks5ServerTestUtils.HttpConnectAsync Exception");
        }

        return null;
    }
}
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public static IReadOnlyList<Mode> Modes { get; private set; } = Array.Empty<Mode>();

    public static IReadOnlyList<Mode> SourceModes { get; private set; } = Array.Empty<Mode>();

    public static IServerController? ServerController { get; private set; }

    public static IModeController? ModeController { get; private set; }

    public static IReadOnlyList<IModeController> ModeControllers { get; private set; } = Array.Empty<IModeController>();

    private static readonly AsyncSemaphore Lock = new(1);

    [OverloadResolutionPriority(1)]
    public static void EnsureUdpCompatibility(Server server, Mode? mode) =>
        EnsureUdpCompatibility(server, mode == null ? Array.Empty<Mode>() : [mode]);

    public static void EnsureUdpCompatibility(Server server, IReadOnlyList<Mode> modes)
    {
        if (!modes.Any(ModeRequiresUdp) || ServerSupportsUdp(server))
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

    [OverloadResolutionPriority(1)]
    public static Task StartAsync(Server server, Mode? mode = null) =>
        StartAsync(server, mode == null ? Array.Empty<Mode>() : [mode]);

    public static async Task StartAsync(Server server, IReadOnlyList<Mode> modes)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(modes);

        using var releaser = await Lock.EnterAsync();

        ModeComposition? composition = null;
        if (modes.Count > 0)
        {
            composition = ModeComposer.Compose(modes);
            foreach (var warning in composition.Warnings)
                Log.Warning("Mode composition warning: {Warning}", warning);
        }

        var sourceModes = composition?.SourceModes ?? Array.Empty<Mode>();
        var effectiveModes = composition?.EffectiveModes ?? Array.Empty<Mode>();

        Log.Information(
            "Start MainController: {Server} {Modes}",
            $"{server.ConfigType}",
            effectiveModes.Count == 0
                ? "Null"
                : string.Join(" + ", effectiveModes.Select(m => $"[{(int)m.Type}]{m.i18NRemark}")));

        EnsureUdpCompatibility(server, effectiveModes);
        EnvironmentChecker.EnsureReady(server, effectiveModes);

        if (await DnsUtils.LookupAsync(server.Address) == null)
            throw new MessageException(i18N.Translate("Lookup Server hostname failed"));

        // TODO Disable NAT Type Test setting
        // cache STUN Server ip to prevent "Wrong STUN Server"
        DnsUtils.LookupAsync(Global.Settings.STUN_Server).Forget();

        try
        {
            await StopCoreAsync();

            Server = server;
            SourceModes = sourceModes;
            Modes = effectiveModes;
            Mode = effectiveModes.FirstOrDefault();

            await Task.WhenAll(Task.Run(NativeMethods.RefreshDNSCache), Task.Run(Firewall.AddNetchFwRules));

            var requireLocalHttp = effectiveModes.Any(m =>
                ModeService.GetModeFeatures(m.Type).HasFlag(ModeFeature.RequireLocalHttpProxy));
            var allSupportSocks5Auth = effectiveModes.Count == 0 || effectiveModes.All(m =>
                ModeService.GetModeFeatures(m.Type).HasFlag(ModeFeature.SupportSocks5Auth));

            //如果是 Socks5 服务器且没有密码
            //或者如果是 Socks5 服务器，且模式控制器支持 Socks5（且不需要本地 HTTP 混合端口）则直接使用该服务器
            if (!requireLocalHttp && Server is SocksServer socks5 && socks5.ProtoExtra.Uot != true &&
                CustomCoreHelper.ResolveForServer(Server) == null &&
                (!socks5.Auth() || allSupportSocks5Auth))
            {
                Socks5Server = socks5;
            }
            else
            {
                // Start Server Controller to get a local socks5 server
                Log.Debug("Server Information: {Data}", $"{server.ConfigType} {server.MaskedData()}");

                ServerController = CoreSelector.CreateController(server);
                if (ServerController is ExternalCliController externalCliController)
                    externalCliController.RequireLocalHttpProxy = requireLocalHttp;
                AppEvents.ReportStatus(i18N.TranslateFormat("Starting {0}", ServerController.Name));

                TryReleaseTcpPort(ServerController.Socks5LocalPort(), "Socks5");
                Socks5Server = await ServerController.StartAsync(server);

                StatusPortInfoText.Socks5Port = (ushort)Socks5Server.Port;
                StatusPortInfoText.UpdateShareLan();
            }

            // Start Mode Controllers
            if (effectiveModes.Count > 0 && Socks5Server != null)
            {
                var startedControllers = new List<IModeController>(effectiveModes.Count);
                foreach (var effectiveMode in effectiveModes)
                {
                    var controller = ModeService.GetModeControllerByType(effectiveMode.Type, out var modePort, out var portName);
                    startedControllers.Add(controller);
                    ModeControllers = startedControllers.ToArray();
                    ModeController ??= controller;

                    if (modePort != null)
                        TryReleaseTcpPort((ushort)modePort, portName);

                    AppEvents.ReportStatus(i18N.TranslateFormat("Starting {0}", controller.Name));
                    await controller.StartAsync(Socks5Server, effectiveMode);
                }

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
        if (ServerController == null &&
            ModeController == null &&
            ModeControllers.Count == 0 &&
            Socks5Server == null &&
            Server == null &&
            Mode == null &&
            Modes.Count == 0 &&
            SourceModes.Count == 0)
        {
            return;
        }

        Log.Information("Stop Main Controller");
        StatusPortInfoText.Reset();

        var controllersToStop = new List<IModeController>();
        for (var i = ModeControllers.Count - 1; i >= 0; i--)
            controllersToStop.Add(ModeControllers[i]);
        if (ModeController != null && !controllersToStop.Contains(ModeController))
            controllersToStop.Add(ModeController);

        // Stop mode/driver consumers before tearing down the local proxy core.
        foreach (var controller in controllersToStop)
        {
            try
            {
                await controller.StopAsync();
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

        ModeControllers = Array.Empty<IModeController>();
        ModeController = null;
        Modes = Array.Empty<Mode>();
        SourceModes = Array.Empty<Mode>();
        Mode = null;
        ServerController = null;
        Socks5Server = null;
        Server = null;

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
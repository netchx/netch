using System.Diagnostics;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Netch.Controllers;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Services;

namespace Netch.Utils;

public static class Bandwidth
{
    public static ulong received;
    private static ulong _sent;
    public static TraceEventSession? tSession;

    /// <summary>
    ///     Bumped by <see cref="Stop" />. A restart passes through Stopped too quickly for the old polling
    ///     loop to notice the state, so each loop runs until its generation ends instead.
    /// </summary>
    private static int _generation;

    private static readonly string[] Suffix = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };

    /// <summary>
    ///     计算流量
    /// </summary>
    /// <param name="d"></param>
    /// <returns>带单位的流量字符串</returns>
    public static string Compute(ulong d)
    {
        const double step = 1024.00;

        byte level = 0;
        double? size = null;
        while ((size ?? d) > step)
        {
            if (level >= 6) // Suffix.Length - 1
                break;

            level++;
            size = (size ?? d) / step;
        }

        return $@"{size ?? d:0.##} {Suffix[level]}";
    }

    /// <summary>
    ///     根据程序名统计流量
    /// </summary>
    public static async Task NetTrafficAsync()
    {
        var generation = Volatile.Read(ref _generation);
        bool Current() => generation == Volatile.Read(ref _generation);

        var activeControllers = MainController.ModeControllers.Count > 0
            ? MainController.ModeControllers
            : MainController.ModeController is { } single
                ? new IModeController[] { single }
                : Array.Empty<IModeController>();

        // Process mode: the redirector already counts every forwarded byte.
        // Avoid a system-wide kernel network trace, which raises an event for
        // every packet on the machine and costs CPU while gaming.
        if (activeControllers.Count == 1 && activeControllers[0] is NFController)
        {
            Interlocked.Exchange(ref received, 0UL);
            AppEvents.SetBandwidthAvailable(true);

            while (Current())
            {
                await Task.Delay(1000);
                if (Current() && AppEvents.State == State.Started)
                {
                    var (download, upload) = Interops.Redirector.GetTrafficBytes();
                    AppEvents.ReportBandwidth(download + upload, download, upload);
                }
            }

            return;
        }

        if (!Flags.IsWindows10Upper)
            return;

        var processes = new List<Process>();
        switch (MainController.ServerController)
        {
            case null:
                break;
            case Guard guard:
                processes.Add(guard.Instance);
                break;
        }

        if (!processes.Any())
        {
            foreach (var controller in activeControllers)
            {
                switch (controller)
                {
                    case TUNController when Interops.TUN2Socks.ActiveProcess is { } tun:
                        processes.Add(tun);
                        break;
                    case Guard guard:
                        processes.Add(guard.Instance);
                        break;
                }
            }
        }

        var pidHastSet = processes.Select(instance => instance.Id).ToHashSet();

        Log.Information("Net traffic processes: {Processes}", string.Join(',', processes.Select(v => $"({v.Id}){v.ProcessName}")));

        Interlocked.Exchange(ref received, 0UL);
        Interlocked.Exchange(ref _sent, 0UL);

        if (!processes.Any())
            return;

        AppEvents.SetBandwidthAvailable(true);

        Task.Run(() =>
            {
                try
                {
                    tSession = new TraceEventSession("KernelAndClrEventsSession");
                    tSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                    tSession.Source.Kernel.TcpIpRecv += data =>
                    {
                        if (pidHastSet.Contains(data.ProcessID))
                            Interlocked.Add(ref received, (ulong)data.size);
                    };
                    tSession.Source.Kernel.UdpIpRecv += data =>
                    {
                        if (pidHastSet.Contains(data.ProcessID))
                            Interlocked.Add(ref received, (ulong)data.size);
                    };
                    tSession.Source.Kernel.TcpIpSend += data =>
                    {
                        if (pidHastSet.Contains(data.ProcessID))
                            Interlocked.Add(ref _sent, (ulong)data.size);
                    };
                    tSession.Source.Kernel.UdpIpSend += data =>
                    {
                        if (pidHastSet.Contains(data.ProcessID))
                            Interlocked.Add(ref _sent, (ulong)data.size);
                    };

                    tSession.Source.Process();
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Bandwidth ETW trace session failed");
                }
            })
            .Forget();

        // Poll without pinning a thread-pool thread for the whole session.
        while (Current())
        {
            await Task.Delay(1000);
            if (Current())
            {
                var download = Interlocked.Read(ref received);
                AppEvents.ReportBandwidth(download, download, Interlocked.Read(ref _sent));
            }
        }
    }

    public static void Stop()
    {
        Interlocked.Increment(ref _generation);
        tSession?.Dispose();
        Interlocked.Exchange(ref received, 0UL);
        Interlocked.Exchange(ref _sent, 0UL);
    }
}

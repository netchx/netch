using System.Collections.Concurrent;
using System.Net;
using Microsoft.VisualStudio.Threading;
using Netch.Enums;
using Netch.Models;
using Netch.Services;
using Timer = System.Timers.Timer;

namespace Netch.Utils;

public static class DelayTestHelper
{
    private static readonly Timer Timer;

    private static readonly SemaphoreSlim Lock = new(1, 1);

    private static readonly object StateLock = new();

    private static CancellationTokenSource? _activeCts;

    private static long _manualGeneration;

    private static long _cancelGeneration;

    public static readonly NumberRange Range = new(0, int.MaxValue / 1000);

    private static bool _enabled = true;

    private static volatile bool _isTesting;

    private static int _activeMode = -1;

    static DelayTestHelper()
    {
        Timer = new Timer
        {
            Interval = 10000,
            AutoReset = true
        };

        Timer.Elapsed += (_, _) => PerformCoreAsync(null, waitFinish: false, fromTimer: true, CancellationToken.None, modeOverride: null).Forget();
    }

    public static bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            UpdateTick();
        }
    }

    public static bool IsTesting => _isTesting;

    public static DelayTestMode? ActiveMode
    {
        get
        {
            var raw = Volatile.Read(ref _activeMode);
            return raw >= 0 ? (DelayTestMode)raw : null;
        }
    }

    public static int CurrentCompleted { get; private set; }

    public static int CurrentTotal { get; private set; }

    public static int LastReachableCount { get; private set; }

    public static bool LastRunCancelled { get; private set; }

    public static void Cancel() => CancelInternal();

    private static long CancelInternal()
    {
        CancellationTokenSource? cts;
        long gen;
        lock (StateLock)
        {
            gen = ++_cancelGeneration;
            cts = _activeCts;
        }

        if (cts != null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Active test run already finished.
            }
        }

        return gen;
    }

    /// <param name="waitFinish">if does not get lock, block until last release</param>
    /// <param name="ct">cancellation token for the test run</param>
    /// <param name="modeOverride">optional delay test mode for this run without modifying saved settings</param>
    public static Task PerformTestAsync(bool waitFinish = false, CancellationToken ct = default, DelayTestMode? modeOverride = null) =>
        PerformCoreAsync(null, waitFinish, fromTimer: false, ct, modeOverride);

    public static Task PerformTestAsync(IEnumerable<Server> targets, bool waitFinish = false, CancellationToken ct = default, DelayTestMode? modeOverride = null)
    {
        var servers = targets as Server[] ?? targets.ToArray();
        if (servers.Length == 0)
            return Task.CompletedTask;

        return PerformCoreAsync(servers, waitFinish, fromTimer: false, ct, modeOverride);
    }

    private static async Task PerformCoreAsync(Server[]? explicitTargets, bool waitFinish, bool fromTimer, CancellationToken ct, DelayTestMode? modeOverride)
    {
        long myGen = 0;
        long expectedCancelGen;
        bool acquired;

        if (fromTimer)
        {
            lock (StateLock)
                expectedCancelGen = _cancelGeneration;
            acquired = await Lock.WaitAsync(0, ct);
            if (!acquired)
                return;
        }
        else
        {
            myGen = Interlocked.Increment(ref _manualGeneration);
            if (!waitFinish)
            {
                lock (StateLock)
                    expectedCancelGen = _cancelGeneration;
                acquired = await Lock.WaitAsync(0, ct);
                if (!acquired)
                    return;
            }
            else
            {
                expectedCancelGen = CancelInternal();
                await Lock.WaitAsync(ct);
                acquired = true;
            }
        }

        try
        {
            if (fromTimer && !Enabled)
                return;

            if (!fromTimer && myGen != Volatile.Read(ref _manualGeneration))
                return;

            var servers = (explicitTargets ?? Global.Settings.Server.ToArray()).Distinct().ToArray();
            if (servers.Length == 0)
                return;

            await RunTestsAsync(servers, fromTimer, myGen, expectedCancelGen, modeOverride, ct);
        }
        finally
        {
            if (acquired)
                Lock.Release();
        }
    }

    private static async Task RunTestsAsync(Server[] servers, bool fromTimer, long myGen, long expectedCancelGen, DelayTestMode? modeOverride, CancellationToken ct)
    {
        var settings = Global.Settings;
        var mode = modeOverride ?? settings.DelayTestMode;
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        lock (StateLock)
        {
            if ((fromTimer && !Enabled) || (!fromTimer && myGen != Volatile.Read(ref _manualGeneration)))
                return;

            if (_cancelGeneration != expectedCancelGen || ct.IsCancellationRequested)
            {
                if (!fromTimer && myGen == Volatile.Read(ref _manualGeneration))
                    LastRunCancelled = true;
                return;
            }

            _activeCts = runCts;
            Volatile.Write(ref _activeMode, (int)mode);
            _isTesting = true;
        }

        var previousDelays = new Dictionary<Server, int>(servers.Length);
        foreach (var s in servers)
        {
            previousDelays[s] = s.Delay;
            s.SetDelay(DelayStatus.Testing);
        }

        var total = servers.Length;
        var completed = 0;
        CurrentTotal = total;
        CurrentCompleted = 0;
        long lastNotifyTicks = 0;
        var notifyLock = new object();

        AppEvents.RaiseNodeDelaysChanged();
        AppEvents.RaiseDelayTestProgress(true, 0, total);

        void ReportResult(Server s, int delay)
        {
            s.SetDelay(delay);
            var done = Interlocked.Increment(ref completed);
            CurrentCompleted = done;
            var now = Environment.TickCount64;
            bool shouldNotify;
            lock (notifyLock)
            {
                if (now - lastNotifyTicks >= 120 || done >= total)
                {
                    lastNotifyTicks = now;
                    shouldNotify = true;
                }
                else
                {
                    shouldNotify = false;
                }
            }

            if (shouldNotify)
            {
                AppEvents.RaiseNodeDelaysChanged();
                AppEvents.RaiseDelayTestProgress(true, done, total);
            }
        }

        try
        {
            var token = runCts.Token;
            var timeoutMs = Math.Clamp(settings.DelayTestTimeout, Constants.MinDelayTestTimeout, Constants.MaxDelayTestTimeout);
            var steadySecond = settings.DelayTestSteadySecondRequest;
            var outboundDns = settings.OutboundDNS;
            var testUri = Uri.TryCreate(settings.DelayTestUrl, UriKind.Absolute, out var parsedUri) &&
                          (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps)
                ? parsedUri
                : new Uri(Constants.DefaultDelayTestUrl);

            // Step A: Concurrent DNS resolution per unique hostname.
            var testableServers = new List<Server>(servers.Length);
            foreach (var s in servers)
            {
                if (s.ConfigType == EConfigType.ExternalCli)
                    ReportResult(s, DelayStatus.Untested);
                else
                    testableServers.Add(s);
            }

            if (testableServers.Count == 0)
                return;

            var dnsResults = new ConcurrentDictionary<string, IPAddress[]>(StringComparer.OrdinalIgnoreCase);
            var uniqueHosts = testableServers
                .Select(s => s.Address?.Trim() ?? string.Empty)
                .Where(h => h.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            using (var dnsSem = new SemaphoreSlim(16))
            {
                var dnsTasks = uniqueHosts.Select(async host =>
                {
                    await dnsSem.WaitAsync(token);
                    try
                    {
                        dnsResults[host] = await DnsUtils.LookupAllAsync(host, dns: outboundDns, timeout: Math.Min(timeoutMs, 3000), ct: token);
                    }
                    finally
                    {
                        dnsSem.Release();
                    }
                });
                await Task.WhenAll(dnsTasks);
            }

            var resolved = new List<(Server Server, IPAddress[] Ips)>(testableServers.Count);
            foreach (var s in testableServers)
            {
                var host = s.Address?.Trim() ?? string.Empty;
                if (host.Length == 0 || !dnsResults.TryGetValue(host, out var ips) || ips.Length == 0)
                    ReportResult(s, DelayStatus.DnsFailed);
                else
                    resolved.Add((s, ips));
            }

            if (resolved.Count == 0)
                return;

            // Step B / C / D dispatch
            switch (mode)
            {
                case DelayTestMode.TCPing:
                case DelayTestMode.ICMPing:
                {
                    var useIcmp = mode == DelayTestMode.ICMPing;
                    var groups = resolved.GroupBy(x => (Ip: x.Ips[0], Port: useIcmp ? 0 : x.Server.Port));
                    using var sem = new SemaphoreSlim(16);
                    var tasks = groups.Select(async group =>
                    {
                        await sem.WaitAsync(token);
                        try
                        {
                            var delay = await ProbeRawEndpointAsync(group.First().Ips, group.Key.Port, timeoutMs, useIcmp, token);
                            foreach (var item in group)
                                ReportResult(item.Server, delay);
                        }
                        finally
                        {
                            sem.Release();
                        }
                    });
                    await Task.WhenAll(tasks);
                    break;
                }
                case DelayTestMode.UrlTest:
                {
                    using var sem = new SemaphoreSlim(16);
                    var targets = resolved.Select(x => (x.Server, x.Ips[0])).ToList();
                    await CoreDelayTester.TestBatchAsync(targets, testUri, timeoutMs, steadySecond, sem, ReportResult, token);
                    break;
                }
                default: // DelayTestMode.Hybrid
                {
                    var tcpCandidates = new List<(Server Server, IPAddress[] Ips)>();
                    var udpDirect = new List<(Server Server, IPAddress[] Ips)>();
                    foreach (var item in resolved)
                    {
                        if (item.Server.SupportsTcpPreScreen())
                            tcpCandidates.Add(item);
                        else
                            udpDirect.Add(item);
                    }

                    var tcpAlive = new ConcurrentBag<(Server Server, IPAddress PrimaryIp)>();
                    var tcpRttFallback = new ConcurrentDictionary<Server, int>();

                    if (tcpCandidates.Count > 0)
                    {
                        var preScreenTimeout = Math.Min(timeoutMs, 1500);
                        var preGroups = tcpCandidates.GroupBy(x => (Ip: x.Ips[0], x.Server.Port));
                        using var preSem = new SemaphoreSlim(24);
                        var preTasks = preGroups.Select(async group =>
                        {
                            await preSem.WaitAsync(token);
                            try
                            {
                                var ips = group.First().Ips;
                                var chosenIp = ips[0];
                                var tcpResult = await Utils.TCPingAsync(chosenIp, group.Key.Port, preScreenTimeout, token);
                                if (!DelayStatus.IsValid(tcpResult) && ips.Length > 1)
                                {
                                    var alt = await Utils.TCPingAsync(ips[1], group.Key.Port, preScreenTimeout, token);
                                    if (DelayStatus.IsValid(alt))
                                    {
                                        chosenIp = ips[1];
                                        tcpResult = alt;
                                    }
                                    else if (tcpResult == DelayStatus.Unreachable && alt == DelayStatus.Timeout)
                                    {
                                        tcpResult = DelayStatus.Timeout;
                                    }
                                }

                                if (DelayStatus.IsValid(tcpResult))
                                {
                                    foreach (var item in group)
                                    {
                                        tcpRttFallback[item.Server] = tcpResult;
                                        tcpAlive.Add((item.Server, chosenIp));
                                    }
                                }
                                else
                                {
                                    foreach (var item in group)
                                        ReportResult(item.Server, tcpResult);
                                }
                            }
                            finally
                            {
                                preSem.Release();
                            }
                        });
                        await Task.WhenAll(preTasks);
                    }

                    var coreTargets = new List<(Server Server, IPAddress PrimaryIp)>(tcpAlive.Count + udpDirect.Count);
                    coreTargets.AddRange(tcpAlive);
                    foreach (var item in udpDirect)
                        coreTargets.Add((item.Server, item.Ips[0]));

                    if (coreTargets.Count > 0)
                    {
                        using var coreSem = new SemaphoreSlim(16);
                        await CoreDelayTester.TestBatchAsync(
                            coreTargets,
                            testUri,
                            timeoutMs,
                            steadySecond,
                            coreSem,
                            ReportResult,
                            token,
                            missingCoreFallback: s => tcpRttFallback.TryGetValue(s, out var tcpMs) ? tcpMs : null);
                    }
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled cleanly; remaining Testing nodes are restored below.
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Delay test run failed");
        }
        finally
        {
            foreach (var s in servers)
            {
                if (s.Delay == DelayStatus.Testing && previousDelays.TryGetValue(s, out var prev))
                    s.SetDelay(prev);
            }

            LastRunCancelled = runCts.IsCancellationRequested;
            LastReachableCount = servers.Count(s => DelayStatus.IsValid(s.Delay));
            CurrentCompleted = Volatile.Read(ref completed);

            lock (StateLock)
            {
                if (ReferenceEquals(_activeCts, runCts))
                    _activeCts = null;
                Volatile.Write(ref _activeMode, -1);
                _isTesting = false;
            }

            AppEvents.RaiseNodeDelaysChanged();
            AppEvents.RaiseDelayTestProgress(false, Volatile.Read(ref completed), total);
        }
    }

    internal static async Task<int> ProbeRawEndpointAsync(
        IPAddress[] ips,
        int port,
        int timeoutMs,
        bool useIcmp,
        CancellationToken ct)
    {
        if (ips.Length == 0)
            return DelayStatus.DnsFailed;

        var chosenIp = ips[0];
        var first = await ProbeOnceAsync(chosenIp, port, timeoutMs, useIcmp, ct);
        if (!DelayStatus.IsValid(first) && ips.Length > 1)
        {
            var alt = await ProbeOnceAsync(ips[1], port, timeoutMs, useIcmp, ct);
            if (DelayStatus.IsValid(alt))
            {
                chosenIp = ips[1];
                first = alt;
            }
            else if (first == DelayStatus.Unreachable && alt == DelayStatus.Timeout)
            {
                first = DelayStatus.Timeout;
            }
        }

        if (first == DelayStatus.Unreachable)
            return DelayStatus.Unreachable;

        if (first == DelayStatus.Timeout)
        {
            await Task.Delay(40, ct);
            return await ProbeOnceAsync(chosenIp, port, timeoutMs, useIcmp, ct);
        }

        var samples = new List<int>(3) { first };
        for (var i = 0; i < 2; i++)
        {
            await Task.Delay(40, ct);
            var next = await ProbeOnceAsync(chosenIp, port, timeoutMs, useIcmp, ct);
            if (DelayStatus.IsValid(next))
                samples.Add(next);
            else if (next == DelayStatus.Unreachable)
                break;
        }

        samples.Sort();
        return samples.Count == 2 ? (samples[0] + samples[1]) / 2 : samples[samples.Count / 2];
    }

    private static Task<int> ProbeOnceAsync(IPAddress ip, int port, int timeoutMs, bool useIcmp, CancellationToken ct) =>
        useIcmp ? Utils.ICMPingAsync(ip, timeoutMs, ct) : Utils.TCPingAsync(ip, port, timeoutMs, ct);

    public static void UpdateTick(bool performTestAtOnce = false)
    {
        UpdateTick(Global.Settings.DetectionTick, performTestAtOnce);
    }

    /// <param name="interval">interval(seconds), 0 disable, MaxValue <c>int.MaxValue/1000</c></param>
    /// <param name="performTestAtOnce"></param>
    private static void UpdateTick(int interval, bool performTestAtOnce = false)
    {
        Timer.Stop();

        var enable = Enabled && interval > 0 && Range.InRange(interval);
        if (enable)
        {
            Timer.Interval = interval * 1000.0;
            Timer.Start();
            if (performTestAtOnce)
                PerformTestAsync().Forget();
        }
    }
}

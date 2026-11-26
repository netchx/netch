using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Netch.Models;

namespace Netch.Services;

public sealed class EtwNetworkMonitor : IDisposable
{
    public const ulong EtwIdBase = 1UL << 62;
    public const string AdminRequiredError = "Administrator privileges are required to monitor unmatched process traffic.";

    private const string SessionName = "NetchUnmatchedNetworkMonitor";
    private const int MaxActiveFlows = 1024;
    private const int MaxClosedFlows = 500;
    private const int MaxPendingEvents = 500;
    private const int MaxProcessCacheEntries = 512;
    private const long UdpIdleTimeoutMs = 45_000;
    private const long TcpIdleTimeoutMs = 90_000;
    private const long ProcessCacheTtlMs = 60_000;
    private const uint ProcessQueryLimitedInformation = 0x1000;

    internal readonly record struct FlowKey(uint Pid, bool IsTcp, string RemoteIp, ushort RemotePort);

    private sealed class EtwFlowRecord
    {
        public ulong Id { get; init; }
        public FlowKey Key { get; init; }
        public string ProcessName { get; init; } = string.Empty;
        public string ProcessPath { get; init; } = string.Empty;
        public ulong UpBytes { get; set; }
        public ulong DownBytes { get; set; }
        public ulong StartUnixMs { get; init; }
        public ulong EndUnixMs { get; set; }
        public long LastActivityTickMs { get; set; }
        public string CloseReason { get; set; } = string.Empty;
        public HashSet<ulong>? ActiveTcpConnIds { get; set; }
    }

    private readonly record struct CachedProcess(string Name, string Path, bool Excluded, long CachedAtTickMs);

    private readonly object _lock = new();
    private readonly SemaphoreSlim _transitionGate = new(1, 1);
    private readonly Dictionary<FlowKey, EtwFlowRecord> _activeFlows = new();
    private readonly Dictionary<(uint Pid, ulong ConnId), FlowKey> _tcpConnIndex = new();
    private readonly Queue<EtwFlowRecord> _closedFlows = new();
    private readonly List<ProcessConnectionEvent> _pendingNewEvents = new();
    private readonly Dictionary<uint, CachedProcess> _processCache = new();
    private readonly Func<uint, (string Name, string Path, bool Excluded)>? _processResolverOverride;
    private readonly string _netchDirPrefix;
    private readonly uint _currentPid;
    private readonly bool _subscribedNetworkChange;

    private volatile HashSet<IPAddress> _localAddresses;
    private volatile HashSet<IPAddress> _broadcastAddresses;
    private TraceEventSession? _activeSession;
    private Thread? _traceThread;
    private bool _wantRunning;
    private int _disposed;
    private ulong _nextFlowSeq;

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public EtwNetworkMonitor()
        : this(null, null, null)
    {
    }

    internal EtwNetworkMonitor(
        Func<uint, (string Name, string Path, bool Excluded)>? processResolverOverride,
        IReadOnlySet<IPAddress>? localAddressesOverride,
        IReadOnlySet<IPAddress>? broadcastAddressesOverride = null)
    {
        _processResolverOverride = processResolverOverride;
        _netchDirPrefix = BuildNetchDirPrefix();
        _currentPid = (uint)Environment.ProcessId;
        if (localAddressesOverride != null)
        {
            _localAddresses = new HashSet<IPAddress>(localAddressesOverride.Select(NormalizeAddress));
            _broadcastAddresses = broadcastAddressesOverride != null
                ? new HashSet<IPAddress>(broadcastAddressesOverride.Select(NormalizeAddress))
                : new HashSet<IPAddress>();
        }
        else
        {
            (_localAddresses, _broadcastAddresses) = BuildNetworkAddresses();
            if (broadcastAddressesOverride != null)
                _broadcastAddresses = new HashSet<IPAddress>(broadcastAddressesOverride.Select(NormalizeAddress));
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            _subscribedNetworkChange = true;
        }
    }

    public event Action? StateChanged;

    public bool IsRunning { get; private set; }

    public string? LastError { get; private set; }

    public void Start()
    {
        if (IsDisposed)
            return;

        Volatile.Write(ref _wantRunning, true);
        LastError = null;
        _ = Task.Run(ReconcileStateAsync);
    }

    public void Stop()
    {
        Volatile.Write(ref _wantRunning, false);
        _ = Task.Run(ReconcileStateAsync);
    }

    public void Shutdown(int timeoutMs = 1500)
    {
        Volatile.Write(ref _wantRunning, false);

        var session = Interlocked.Exchange(ref _activeSession, null);
        if (session != null)
        {
            try
            {
                session.Dispose();
            }
            catch (Exception e)
            {
                Log.Debug(e, "ETW session fast dispose error");
            }
        }

        try
        {
            if (_transitionGate.Wait(timeoutMs))
            {
                try
                {
                    StopSessionLocked(timeoutMs);
                }
                finally
                {
                    try
                    {
                        _transitionGate.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _activeFlows.Clear();
            _tcpConnIndex.Clear();
            _closedFlows.Clear();
            _pendingNewEvents.Clear();
        }
    }

    public void ClearClosed()
    {
        lock (_lock)
            _closedFlows.Clear();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_subscribedNetworkChange)
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;

        Shutdown(1000);
        _transitionGate.Dispose();
    }

    internal static IPAddress NormalizeAddress(IPAddress address)
    {
        var normalized = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        if (normalized.AddressFamily == AddressFamily.InterNetworkV6 && normalized.ScopeId != 0)
            normalized = new IPAddress(normalized.GetAddressBytes());

        return normalized;
    }

    internal static IPAddress? TryComputeDirectedBroadcast(IPAddress ipv4Address, IPAddress ipv4Mask)
    {
        var addr = NormalizeAddress(ipv4Address);
        if (addr.AddressFamily != AddressFamily.InterNetwork || ipv4Mask.AddressFamily != AddressFamily.InterNetwork)
            return null;

        var ipBytes = addr.GetAddressBytes();
        var maskBytes = ipv4Mask.GetAddressBytes();
        if ((maskBytes[0] == 255 && maskBytes[1] == 255 && maskBytes[2] == 255 && maskBytes[3] == 255) ||
            (maskBytes[0] == 0 && maskBytes[1] == 0 && maskBytes[2] == 0 && maskBytes[3] == 0))
        {
            return null;
        }

        var broadcast = new byte[4];
        for (var i = 0; i < 4; i++)
            broadcast[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

        return new IPAddress(broadcast);
    }

    internal static bool ShouldIgnoreRemoteAddress(
        IPAddress remoteAddress,
        int remotePort,
        IReadOnlySet<IPAddress>? broadcastAddresses = null)
    {
        if (remotePort is <= 0 or > 65535)
            return true;

        var addr = NormalizeAddress(remoteAddress);
        if (IPAddress.IsLoopback(addr) ||
            addr.Equals(IPAddress.Any) ||
            addr.Equals(IPAddress.IPv6Any) ||
            addr.Equals(IPAddress.Broadcast) ||
            addr.IsIPv6Multicast ||
            (broadcastAddresses != null && broadcastAddresses.Contains(addr)))
        {
            return true;
        }

        if (addr.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = addr.GetAddressBytes();
            if (bytes[0] >= 224)
                return true;
        }

        return false;
    }

    internal static (IPAddress RemoteAddress, int RemotePort) ResolveRemoteEndpoint(
        IPAddress saddr,
        int sport,
        IPAddress daddr,
        int dport,
        bool isTcp,
        bool isSend,
        IReadOnlySet<IPAddress> localAddresses)
    {
        var src = NormalizeAddress(saddr);
        var dst = NormalizeAddress(daddr);

        var srcLocal = IsLocalOrUnspecified(src, localAddresses);
        var dstLocal = IsLocalOrUnspecified(dst, localAddresses);

        if (srcLocal && !dstLocal)
            return (dst, dport);

        if (dstLocal && !srcLocal)
            return (src, sport);

        if (isSend)
            return (dst, dport);

        return isTcp ? (dst, dport) : (src, sport);
    }

    internal void RecordTrafficSample(
        int processId,
        ulong connId,
        bool isTcp,
        bool isSend,
        IPAddress? saddr,
        int sport,
        IPAddress? daddr,
        int dport,
        int bytes,
        DateTimeOffset nowUtc,
        long nowTickMs,
        string? processNameHint = null)
    {
        if (processId <= 0 || saddr == null || daddr == null)
            return;

        var pid = (uint)processId;
        var (procName, procPath, excluded) = ResolveProcess(pid, nowTickMs, processNameHint);
        if (excluded)
            return;

        var byteCount = bytes > 0 ? (ulong)bytes : 0UL;

        lock (_lock)
        {
            if (isTcp && connId != 0 &&
                _tcpConnIndex.TryGetValue((pid, connId), out var existingKey) &&
                _activeFlows.TryGetValue(existingKey, out var existingFlow))
            {
                if (isSend)
                    existingFlow.UpBytes += byteCount;
                else
                    existingFlow.DownBytes += byteCount;
                existingFlow.LastActivityTickMs = nowTickMs;
                return;
            }

            var (remoteAddr, remotePort) = ResolveRemoteEndpoint(saddr, sport, daddr, dport, isTcp, isSend, _localAddresses);
            if (ShouldIgnoreRemoteAddress(remoteAddr, remotePort, _broadcastAddresses))
                return;

            var remoteIp = NormalizeAddress(remoteAddr).ToString();
            var port = (ushort)remotePort;
            var flowKey = new FlowKey(pid, isTcp, remoteIp, port);

            if (!_activeFlows.TryGetValue(flowKey, out var flow))
            {
                if (_activeFlows.Count >= MaxActiveFlows)
                    EvictOldestActiveLocked(nowUtc);

                var id = EtwIdBase + (++_nextFlowSeq);
                var nowMs = (ulong)Math.Max(0L, nowUtc.ToUnixTimeMilliseconds());
                flow = new EtwFlowRecord
                {
                    Id = id,
                    Key = flowKey,
                    ProcessName = procName,
                    ProcessPath = procPath,
                    StartUnixMs = nowMs,
                    LastActivityTickMs = nowTickMs
                };
                _activeFlows[flowKey] = flow;

                if (_pendingNewEvents.Count < MaxPendingEvents)
                {
                    _pendingNewEvents.Add(new ProcessConnectionEvent
                    {
                        TimestampMs = nowMs,
                        ConnectionId = id,
                        Action = "DIRECT",
                        Protocol = isTcp ? "TCP" : "UDP",
                        Pid = pid,
                        ProcessName = procName,
                        ProcessPath = procPath,
                        RemoteIp = remoteIp,
                        RemotePort = port,
                        MatchedRule = "DIRECT",
                        RuleSource = "ETW.Direct"
                    });
                }
            }

            if (isTcp && connId != 0)
            {
                _tcpConnIndex[(pid, connId)] = flowKey;
                flow.ActiveTcpConnIds ??= new HashSet<ulong>();
                flow.ActiveTcpConnIds.Add(connId);
            }

            if (isSend)
                flow.UpBytes += byteCount;
            else
                flow.DownBytes += byteCount;
            flow.LastActivityTickMs = nowTickMs;
        }
    }

    internal void RecordTcpDisconnect(
        int processId,
        ulong connId,
        IPAddress? saddr,
        int sport,
        IPAddress? daddr,
        int dport,
        DateTimeOffset nowUtc)
    {
        if (processId <= 0)
            return;

        var pid = (uint)processId;
        lock (_lock)
        {
            if (connId != 0 && _tcpConnIndex.Remove((pid, connId), out var indexedKey))
            {
                if (_activeFlows.TryGetValue(indexedKey, out var indexedFlow))
                {
                    indexedFlow.ActiveTcpConnIds?.Remove(connId);
                    if (indexedFlow.ActiveTcpConnIds == null || indexedFlow.ActiveTcpConnIds.Count == 0)
                        MoveToClosedLocked(indexedKey, indexedFlow, nowUtc, "TCP disconnected");
                    return;
                }
            }

            if (saddr != null && daddr != null)
            {
                var (remoteAddr, remotePort) = ResolveRemoteEndpoint(saddr, sport, daddr, dport, true, true, _localAddresses);
                if (!ShouldIgnoreRemoteAddress(remoteAddr, remotePort, _broadcastAddresses))
                {
                    var key = new FlowKey(pid, true, NormalizeAddress(remoteAddr).ToString(), (ushort)remotePort);
                    if (_activeFlows.TryGetValue(key, out var flow))
                    {
                        if (connId != 0)
                        {
                            _tcpConnIndex.Remove((pid, connId));
                            flow.ActiveTcpConnIds?.Remove(connId);
                        }

                        if (connId == 0 || flow.ActiveTcpConnIds == null || flow.ActiveTcpConnIds.Count == 0)
                            MoveToClosedLocked(key, flow, nowUtc, "TCP disconnected");
                    }
                }
            }
        }
    }

    internal (List<ProcessConnectionSnapshot> Active, List<ProcessConnectionSnapshot> Closed, List<ProcessConnectionEvent> NewEvents) TakeSnapshot(
        DateTimeOffset nowUtc,
        long nowTickMs)
    {
        lock (_lock)
        {
            SweepExpiredFlowsLocked(nowUtc, nowTickMs);

            var active = new List<ProcessConnectionSnapshot>(_activeFlows.Count);
            foreach (var flow in _activeFlows.Values)
                active.Add(ToSnapshot(flow, closed: false));
            active.Sort((a, b) => b.Id.CompareTo(a.Id));

            var closed = new List<ProcessConnectionSnapshot>(_closedFlows.Count);
            foreach (var flow in _closedFlows.Reverse())
                closed.Add(ToSnapshot(flow, closed: true));

            var newEvents = _pendingNewEvents.Count > 0
                ? new List<ProcessConnectionEvent>(_pendingNewEvents)
                : new List<ProcessConnectionEvent>();
            _pendingNewEvents.Clear();

            return (active, closed, newEvents);
        }
    }

    private async Task ReconcileStateAsync()
    {
        try
        {
            await _transitionGate.WaitAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            while (!IsDisposed)
            {
                var want = Volatile.Read(ref _wantRunning);
                if (want && !IsRunning)
                {
                    await StartSessionLockedAsync().ConfigureAwait(false);
                }
                else if (!want && (IsRunning || _activeSession != null || _traceThread != null))
                {
                    StopSessionLocked(1500);
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            try
            {
                _transitionGate.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async Task StartSessionLockedAsync()
    {
        if (IsRunning || IsDisposed)
            return;

        (_localAddresses, _broadcastAddresses) = BuildNetworkAddresses();

        try
        {
            using var stale = TraceEventSession.GetActiveSession(SessionName);
            stale?.Stop(true);
        }
        catch (Exception e)
        {
            Log.Debug(e, "Failed to clean stale ETW session {Session}", SessionName);
        }

        var ready = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            TraceEventSession? session = null;
            try
            {
                session = new TraceEventSession(SessionName)
                {
                    StopOnDispose = true
                };
                session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
                SubscribeNetworkEvents(session);

                Interlocked.Exchange(ref _activeSession, session);
                IsRunning = true;
                LastError = null;
                ready.TrySetResult(null);
                StateChanged?.Invoke();

                session.Source.Process();
            }
            catch (UnauthorizedAccessException)
            {
                LastError = AdminRequiredError;
                ready.TrySetResult(AdminRequiredError);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Log.Warning(e, "ETW unmatched network monitor failed");
                ready.TrySetResult(e.Message);
            }
            finally
            {
                IsRunning = false;
                var current = Interlocked.Exchange(ref _activeSession, null);
                if (current != null)
                {
                    try
                    {
                        current.Dispose();
                    }
                    catch
                    {
                    }
                }
                else
                {
                    session?.Dispose();
                }

                ready.TrySetResult(LastError ?? "ETW session stopped during startup.");
                StateChanged?.Invoke();
            }
        })
        {
            IsBackground = true,
            Name = "Netch.EtwNetworkMonitor"
        };

        _traceThread = thread;
        thread.Start();

        using var timeoutCts = new CancellationTokenSource();
        var delayTask = Task.Delay(3000, timeoutCts.Token);
        var completed = await Task.WhenAny(ready.Task, delayTask).ConfigureAwait(false);
        await timeoutCts.CancelAsync().ConfigureAwait(false);

        var startupError = completed == ready.Task
            ? await ready.Task.ConfigureAwait(false)
            : "Timed out starting ETW monitor.";
        if (IsDisposed || completed != ready.Task || startupError != null)
        {
            if (completed != ready.Task && !IsDisposed)
                LastError ??= startupError;
            Volatile.Write(ref _wantRunning, false);
            StopSessionLocked(500);
        }
    }

    private void StopSessionLocked(int joinTimeoutMs)
    {
        var session = Interlocked.Exchange(ref _activeSession, null);
        if (session != null)
        {
            try
            {
                session.Dispose();
            }
            catch (Exception e)
            {
                Log.Debug(e, "ETW session dispose failed");
            }
        }

        var thread = _traceThread;
        _traceThread = null;
        if (thread != null && thread.IsAlive)
        {
            try
            {
                thread.Join(joinTimeoutMs);
            }
            catch
            {
            }
        }

        IsRunning = false;
        Clear();
    }

    private void SubscribeNetworkEvents(TraceEventSession session)
    {
        var kernel = session.Source.Kernel;

        kernel.TcpIpSend += data => RecordTrafficSample(
            data.ProcessID, data.connid, true, true, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.TcpIpRecv += data => RecordTrafficSample(
            data.ProcessID, data.connid, true, false, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.TcpIpConnect += data => RecordTrafficSample(
            data.ProcessID, data.connid, true, true, data.saddr, data.sport, data.daddr, data.dport, 0,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.TcpIpDisconnect += data => RecordTcpDisconnect(
            data.ProcessID, data.connid, data.saddr, data.sport, data.daddr, data.dport, DateTimeOffset.UtcNow);

        kernel.TcpIpSendIPV6 += data => RecordTrafficSample(
            data.ProcessID, data.connid, true, true, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.TcpIpRecvIPV6 += data => RecordTrafficSample(
            data.ProcessID, data.connid, true, false, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.TcpIpConnectIPV6 += data => RecordTrafficSample(
            data.ProcessID, data.connid, true, true, data.saddr, data.sport, data.daddr, data.dport, 0,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.TcpIpDisconnectIPV6 += data => RecordTcpDisconnect(
            data.ProcessID, data.connid, data.saddr, data.sport, data.daddr, data.dport, DateTimeOffset.UtcNow);

        kernel.UdpIpSend += data => RecordTrafficSample(
            data.ProcessID, 0, false, true, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.UdpIpRecv += data => RecordTrafficSample(
            data.ProcessID, 0, false, false, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);

        kernel.UdpIpSendIPV6 += data => RecordTrafficSample(
            data.ProcessID, 0, false, true, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
        kernel.UdpIpRecvIPV6 += data => RecordTrafficSample(
            data.ProcessID, 0, false, false, data.saddr, data.sport, data.daddr, data.dport, data.size,
            DateTimeOffset.UtcNow, Environment.TickCount64, data.ProcessName);
    }

    private (string Name, string Path, bool Excluded) ResolveProcess(uint pid, long nowTickMs, string? processNameHint)
    {
        if (pid is 0 or 4 || pid == _currentPid)
            return (string.Empty, string.Empty, true);

        if (_processResolverOverride != null)
            return _processResolverOverride(pid);

        lock (_processCache)
        {
            if (_processCache.TryGetValue(pid, out var cached) &&
                nowTickMs - cached.CachedAtTickMs < ProcessCacheTtlMs)
            {
                return (cached.Name, cached.Path, cached.Excluded);
            }
        }

        var fullPath = QueryProcessPath(pid);
        var procName = string.Empty;
        if (!string.IsNullOrEmpty(fullPath))
        {
            procName = Path.GetFileName(fullPath);
        }
        else
        {
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                procName = proc.ProcessName + ".exe";
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(processNameHint))
                    procName = processNameHint.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? processNameHint
                        : processNameHint + ".exe";
            }
        }

        var excluded = !string.IsNullOrEmpty(fullPath) &&
            fullPath.StartsWith(_netchDirPrefix, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(procName) || excluded)
        {
            lock (_processCache)
            {
                if (_processCache.Count >= MaxProcessCacheEntries)
                    PruneProcessCacheLocked(nowTickMs);
                _processCache[pid] = new CachedProcess(procName, fullPath, excluded, nowTickMs);
            }
        }

        return (procName, fullPath, excluded);
    }

    private void PruneProcessCacheLocked(long nowTickMs)
    {
        foreach (var stalePid in _processCache
                     .Where(kv => nowTickMs - kv.Value.CachedAtTickMs >= ProcessCacheTtlMs)
                     .Select(kv => kv.Key)
                     .ToArray())
        {
            _processCache.Remove(stalePid);
        }

        if (_processCache.Count >= MaxProcessCacheEntries)
            _processCache.Clear();
    }

    private void SweepExpiredFlowsLocked(DateTimeOffset nowUtc, long nowTickMs)
    {
        List<FlowKey>? expired = null;
        foreach (var kv in _activeFlows)
        {
            var timeout = kv.Key.IsTcp ? TcpIdleTimeoutMs : UdpIdleTimeoutMs;
            if (nowTickMs - kv.Value.LastActivityTickMs >= timeout)
            {
                expired ??= new List<FlowKey>();
                expired.Add(kv.Key);
            }
        }

        if (expired == null)
            return;

        foreach (var key in expired)
        {
            if (_activeFlows.TryGetValue(key, out var flow))
                MoveToClosedLocked(key, flow, nowUtc, key.IsTcp ? "TCP idle timeout" : "UDP idle timeout");
        }
    }

    private void EvictOldestActiveLocked(DateTimeOffset nowUtc)
    {
        EtwFlowRecord? victim = null;
        foreach (var flow in _activeFlows.Values)
        {
            if (victim == null || flow.LastActivityTickMs < victim.LastActivityTickMs)
                victim = flow;
        }

        if (victim != null)
            MoveToClosedLocked(victim.Key, victim, nowUtc, "Active tracker limit reached");
    }

    private void MoveToClosedLocked(FlowKey key, EtwFlowRecord flow, DateTimeOffset nowUtc, string reason)
    {
        _activeFlows.Remove(key);
        if (key.IsTcp && flow.ActiveTcpConnIds is { Count: > 0 } connIds)
        {
            foreach (var connId in connIds)
                _tcpConnIndex.Remove((key.Pid, connId));
            connIds.Clear();
        }

        flow.EndUnixMs = (ulong)Math.Max(0L, nowUtc.ToUnixTimeMilliseconds());
        flow.CloseReason = reason;
        _closedFlows.Enqueue(flow);
        while (_closedFlows.Count > MaxClosedFlows)
            _closedFlows.Dequeue();
    }

    private static ProcessConnectionSnapshot ToSnapshot(EtwFlowRecord flow, bool closed) => new()
    {
        Id = flow.Id,
        Protocol = flow.Key.IsTcp ? "TCP" : "UDP",
        State = closed ? "Closed" : "Active",
        Action = "DIRECT",
        Pid = flow.Key.Pid,
        ProcessName = flow.ProcessName,
        ProcessPath = flow.ProcessPath,
        RemoteIp = flow.Key.RemoteIp,
        RemotePort = flow.Key.RemotePort,
        UpBytes = flow.UpBytes,
        DownBytes = flow.DownBytes,
        StartUnixMs = flow.StartUnixMs,
        EndUnixMs = flow.EndUnixMs,
        MatchedRule = "DIRECT",
        RuleSource = "ETW.Direct",
        CloseReason = flow.CloseReason
    };

    private static bool IsLocalOrUnspecified(IPAddress addr, IReadOnlySet<IPAddress> localAddresses) =>
        IPAddress.IsLoopback(addr) ||
        addr.Equals(IPAddress.Any) ||
        addr.Equals(IPAddress.IPv6Any) ||
        localAddresses.Contains(addr);

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        (_localAddresses, _broadcastAddresses) = BuildNetworkAddresses();
    }

    private static (HashSet<IPAddress> Local, HashSet<IPAddress> Broadcast) BuildNetworkAddresses()
    {
        var local = new HashSet<IPAddress> { IPAddress.Loopback, IPAddress.IPv6Loopback };
        var broadcast = new HashSet<IPAddress>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var ip in nic.GetIPProperties().UnicastAddresses)
                {
                    local.Add(NormalizeAddress(ip.Address));
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.IPv4Mask != null)
                    {
                        var directed = TryComputeDirectedBroadcast(ip.Address, ip.IPv4Mask);
                        if (directed != null)
                            broadcast.Add(directed);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Debug(e, "Failed to enumerate local addresses for ETW monitor");
        }

        return (local, broadcast);
    }

    private static string BuildNetchDirPrefix()
    {
        var dir = Path.GetFullPath(Global.NetchDir);
        return dir.EndsWith(Path.DirectorySeparatorChar)
            ? dir
            : dir + Path.DirectorySeparatorChar;
    }

    private static string QueryProcessPath(uint pid)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, pid);
        if (handle == 0)
            return string.Empty;

        try
        {
            var buffer = new char[1024];
            var size = (uint)buffer.Length;
            return QueryFullProcessImageNameW(handle, 0, buffer, ref size) && size > 0
                ? new string(buffer, 0, (int)size)
                : string.Empty;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageNameW(nint hProcess, uint dwFlags, [Out] char[] lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);
}

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Netch.Controllers;
using Netch.Models;
using Netch.Models.Modes.ProcessMode;
using Netch.Utils;

namespace Netch.Services;

public enum ProcessRuleSafety
{
    Normal,
    Caution,
    Blocked
}

public sealed record FilteredConnectionEventRow(ProcessConnectionEvent Event, int Count);

public sealed class ConnectionTrackerService
{
    private const int MaxBufferedEvents = 5000;
    private const int MaxDrainBatchesPerTick = 20;

    private static readonly HashSet<string> BlockedSystemProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Idle",
        "System",
        "Unknown",
        "Registry",
        "Memory Compression",
        "smss",
        "smss.exe",
        "csrss",
        "csrss.exe",
        "wininit",
        "wininit.exe",
        "services",
        "services.exe",
        "lsass",
        "lsass.exe",
        "winlogon",
        "winlogon.exe"
    };

    private static readonly HashSet<string> CautionSystemProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "svchost",
        "svchost.exe"
    };

    public static ConnectionTrackerService Instance { get; } = new();

    private readonly EtwNetworkMonitor _etwMonitor = new();
    private readonly string? _logFilePathOverride;
    private readonly object _stateLock = new();
    private readonly object _pollExecLock = new();
    private readonly Dictionary<ulong, (ulong Up, ulong Down)> _prevBytesByConnId = new();
    private readonly Dictionary<(uint Pid, string Protocol, string RemoteIp, ushort RemotePort), DateTimeOffset> _recentNativeClosed = new();
    private readonly Dictionary<(uint Pid, string Protocol, string RemoteIp, ushort RemotePort), DateTimeOffset> _recentNativeEvents = new();
    private readonly List<ProcessConnectionEvent> _events = new();
    private ProcessConnectionSnapshot[] _nativeActiveConnections = Array.Empty<ProcessConnectionSnapshot>();
    private ProcessConnectionSnapshot[] _nativeClosedConnections = Array.Empty<ProcessConnectionSnapshot>();
    private ProcessConnectionSnapshot[] _etwActiveConnections = Array.Empty<ProcessConnectionSnapshot>();
    private ProcessConnectionSnapshot[] _etwClosedConnections = Array.Empty<ProcessConnectionSnapshot>();
    private ProcessConnectionSnapshot[] _activeConnections = Array.Empty<ProcessConnectionSnapshot>();
    private ProcessConnectionSnapshot[] _closedConnections = Array.Empty<ProcessConnectionSnapshot>();

    private NFController? _nfController;
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
    private DateTimeOffset _lastPollUtc = DateTimeOffset.UtcNow;
    private ulong _lastTotalUp;
    private ulong _lastTotalDown;
    private ulong _eventCursor;
    private ulong _lastAssignedSeq;

    private readonly object _logWriterLock = new();
    private Channel<string>? _logChannel;
    private Task? _logWriterTask;

    public ConnectionTrackerService(string? logFilePathOverride = null)
    {
        _logFilePathOverride = logFilePathOverride;
        _etwMonitor.StateChanged += () => StateChanged?.Invoke();
    }

    public event Action? ConnectionsUpdated;

    public event Action<IReadOnlyList<ProcessConnectionEvent>>? EventsAppended;

    public event Action? StateChanged;

    public HotRuleOverlay PendingHotRules { get; } = new();

    public bool IsProcessModeActive { get; private set; }

    public bool IsPaused { get; set; }

    public bool TrackUnmatched { get; private set; }

    public string? EtwError => _etwMonitor.LastError;

    public Redirector? ActiveMode => _nfController?.CurrentMode;

    public IReadOnlyList<Redirector> ActiveSourceProcessModes =>
        MainController.SourceModes.OfType<Redirector>().ToArray();

    public bool HasUnsavedHotRules => PendingHotRules.HasChanges;

    public string LogFilePath => _logFilePathOverride ?? Path.Combine(Global.NetchDir, "logging", "nf-connections.log");

    public ulong TotalUpBytes { get; private set; }

    public ulong TotalDownBytes { get; private set; }

    public ulong TotalUpSpeedBps { get; private set; }

    public ulong TotalDownSpeedBps { get; private set; }

    public ulong DirectUpSpeedBps { get; private set; }

    public ulong DirectDownSpeedBps { get; private set; }

    public IReadOnlyList<ProcessConnectionSnapshot> ActiveConnections
    {
        get
        {
            lock (_stateLock)
                return _activeConnections;
        }
    }

    public IReadOnlyList<ProcessConnectionSnapshot> ClosedConnections
    {
        get
        {
            lock (_stateLock)
                return _closedConnections;
        }
    }

    public IReadOnlyList<ProcessConnectionEvent> Events
    {
        get
        {
            lock (_stateLock)
                return _events.ToArray();
        }
    }

    public void OnSessionStarted(NFController controller, Redirector mode)
    {
        _ = mode;
        lock (_stateLock)
        {
            _nfController = controller;
            RemovePrevBytesLocked(id => id < EtwNetworkMonitor.EtwIdBase);
            _recentNativeClosed.Clear();
            _recentNativeEvents.Clear();
            _lastTotalUp = 0;
            _lastTotalDown = 0;
            _lastPollUtc = DateTimeOffset.UtcNow;
            TotalUpBytes = 0;
            TotalDownBytes = 0;
            TotalUpSpeedBps = 0;
            TotalDownSpeedBps = 0;
            _nativeActiveConnections = Array.Empty<ProcessConnectionSnapshot>();
            MergeAndComputeSpeedsLocked(1.0, _lastPollUtc);
        }

        IsProcessModeActive = true;
        EnsureLogWriterState(Global.Settings.Redirector.LogConnectionsToFile);
        if (TrackUnmatched)
            _etwMonitor.Start();

        RestartPollingLoop();
        StateChanged?.Invoke();
    }

    public Task PausePollingAsync() => StopPollingAsync();

    public async Task OnSessionStoppedAsync()
    {
        if (IsProcessModeActive)
        {
            try
            {
                PollOnce(pollNative: true, pollEtw: TrackUnmatched);
            }
            catch (Exception e)
            {
                Log.Warning(e, "Final connection tracker drain failed");
            }
        }

        lock (_stateLock)
        {
            _nfController = null;
            RemovePrevBytesLocked(id => id < EtwNetworkMonitor.EtwIdBase);
            _nativeActiveConnections = Array.Empty<ProcessConnectionSnapshot>();
            TotalUpBytes = 0;
            TotalDownBytes = 0;
            TotalUpSpeedBps = 0;
            TotalDownSpeedBps = 0;
            MergeAndComputeSpeedsLocked(1.0, DateTimeOffset.UtcNow);
        }

        IsProcessModeActive = false;
        if (TrackUnmatched)
        {
            RestartPollingLoop();
        }
        else
        {
            await StopPollingAsync();
            await FlushAndStopLogWriterAsync();
        }

        ConnectionsUpdated?.Invoke();
        StateChanged?.Invoke();
    }

    public void SetTrackUnmatched(bool enabled)
    {
        TrackUnmatched = enabled;
        if (IsProcessModeActive)
            NFController.ApplyTrackingOptions(true, enabled);

        if (enabled)
        {
            EnsureLogWriterState(Global.Settings.Redirector.LogConnectionsToFile);
            _etwMonitor.Start();
            EnsurePollingLoop();
        }
        else
        {
            _etwMonitor.Stop();
            lock (_stateLock)
            {
                _etwActiveConnections = Array.Empty<ProcessConnectionSnapshot>();
                _etwClosedConnections = Array.Empty<ProcessConnectionSnapshot>();
                RemovePrevBytesLocked(id => id >= EtwNetworkMonitor.EtwIdBase);
                DirectUpSpeedBps = 0;
                DirectDownSpeedBps = 0;
                MergeAndComputeSpeedsLocked(1.0, DateTimeOffset.UtcNow);
            }

            if (!IsProcessModeActive)
                _ = StopWhenIdleAsync();
        }

        ConnectionsUpdated?.Invoke();
        StateChanged?.Invoke();
    }

    public void Shutdown()
    {
        TrackUnmatched = false;
        IsProcessModeActive = false;
        _etwMonitor.Shutdown();

        CancellationTokenSource? cts;
        lock (_stateLock)
        {
            cts = _pollCts;
            _pollCts = null;
            _pollTask = null;
        }

        if (cts != null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _ = FlushAndStopLogWriterAsync();
    }

    public void SetLogToFile(bool enabled)
    {
        Global.Settings.Redirector.LogConnectionsToFile = enabled;
        EnsureLogWriterState(enabled);
        StateChanged?.Invoke();
    }

    public async Task FlushAndStopLogWriterAsync()
    {
        Channel<string>? channel;
        Task? writerTask;
        lock (_logWriterLock)
        {
            channel = _logChannel;
            writerTask = _logWriterTask;
            _logChannel = null;
            _logWriterTask = null;
        }

        if (channel != null)
        {
            channel.Writer.TryComplete();
            if (writerTask != null)
            {
                try
                {
                    await writerTask;
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Connection log writer flush error");
                }
            }
        }
    }

    public bool CloseConnection(ulong connId)
    {
        if (connId >= EtwNetworkMonitor.EtwIdBase || !IsProcessModeActive)
            return false;

        var closed = Interops.Redirector.CloseConnection(connId);
        RefreshConnectionsNow();
        return closed;
    }

    public void CloseAllConnections()
    {
        if (!IsProcessModeActive)
            return;

        Interops.Redirector.CloseAllConnections();
        RefreshConnectionsNow();
    }

    public void ClearClosedConnections()
    {
        if (IsProcessModeActive)
            Interops.Redirector.ClearClosedConnections();
        _etwMonitor.ClearClosed();

        lock (_stateLock)
        {
            _nativeClosedConnections = Array.Empty<ProcessConnectionSnapshot>();
            _etwClosedConnections = Array.Empty<ProcessConnectionSnapshot>();
            _closedConnections = Array.Empty<ProcessConnectionSnapshot>();
        }

        ConnectionsUpdated?.Invoke();
    }

    public void ClearEvents()
    {
        lock (_stateLock)
            _events.Clear();

        StateChanged?.Invoke();
    }

    public void RefreshConnectionsNow()
    {
        if (!IsProcessModeActive && !TrackUnmatched)
            return;

        try
        {
            PollOnce(IsProcessModeActive, TrackUnmatched);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Connection tracker manual refresh failed");
        }
    }

    public static ProcessRuleSafety ClassifyProcessRuleSafety(string? processNameOrPattern)
    {
        if (string.IsNullOrWhiteSpace(processNameOrPattern))
            return ProcessRuleSafety.Blocked;

        var candidate = processNameOrPattern.Trim();
        if (candidate.StartsWith('^'))
            candidate = candidate[1..];
        if (candidate.EndsWith('$'))
            candidate = candidate[..^1];

        candidate = candidate.Replace(@"\\", @"\", StringComparison.Ordinal).Replace(@"\.", ".", StringComparison.Ordinal);
        var lastSlash = candidate.LastIndexOfAny(['\\', '/']);
        if (lastSlash >= 0 && lastSlash + 1 < candidate.Length)
            candidate = candidate[(lastSlash + 1)..];

        if (string.IsNullOrWhiteSpace(candidate))
            return ProcessRuleSafety.Blocked;

        if (BlockedSystemProcessNames.Contains(candidate))
            return ProcessRuleSafety.Blocked;

        if (CautionSystemProcessNames.Contains(candidate))
            return ProcessRuleSafety.Caution;

        return ProcessRuleSafety.Normal;
    }

    public bool TryApplyHotRule(HotRuleKind kind, string rawInput, string displayLabel, out string? errorMessage)
    {
        errorMessage = null;
        string normalizedPattern;

        if (kind is HotRuleKind.HandleProcess or HotRuleKind.BypassProcess)
        {
            normalizedPattern = rawInput.Trim();
            if (string.IsNullOrEmpty(normalizedPattern))
            {
                errorMessage = "Empty process rule.";
                return false;
            }

            if (kind == HotRuleKind.HandleProcess &&
                (ClassifyProcessRuleSafety(displayLabel) == ProcessRuleSafety.Blocked ||
                 ClassifyProcessRuleSafety(normalizedPattern) == ProcessRuleSafety.Blocked))
            {
                errorMessage = i18N.Translate("Core Windows system processes cannot be added to proxy rules.");
                return false;
            }

            if (!NFController.CheckRules([normalizedPattern], out _))
            {
                errorMessage = NFController.GenerateInvalidRulesMessage([normalizedPattern]);
                return false;
            }
        }
        else
        {
            if (!RouteUtils.TryNormalizeIpOrCidr(rawInput, out var normalizedIp))
            {
                errorMessage = NFController.GenerateInvalidIpRulesMessage([rawInput]);
                return false;
            }

            normalizedPattern = normalizedIp;
        }

        PendingHotRules.Add(kind, normalizedPattern, displayLabel);
        HotReloadActiveController();
        StateChanged?.Invoke();
        return true;
    }

    public bool RemoveHotRule(HotRuleEntry entry)
    {
        if (!PendingHotRules.Remove(entry))
            return false;

        HotReloadActiveController();
        StateChanged?.Invoke();
        return true;
    }

    public void DiscardHotRules()
    {
        if (!PendingHotRules.Clear())
            return;

        HotReloadActiveController();
        StateChanged?.Invoke();
    }

    public void CommitSavedBaseMode(Redirector updatedBaseMode)
    {
        PendingHotRules.Clear();
        _nfController?.CommitBaseModeUpdate(updatedBaseMode);
        HotReloadActiveController();
        StateChanged?.Invoke();
    }

    public void ApplyConnectionSnapshot(ProcessConnectionsDump dump, double elapsedSeconds)
    {
        var active = dump.Active ?? new List<ProcessConnectionSnapshot>();
        var closed = dump.Closed ?? new List<ProcessConnectionSnapshot>();

        lock (_stateLock)
        {
            _nativeActiveConnections = active.ToArray();
            _nativeClosedConnections = closed.ToArray();
            MergeAndComputeSpeedsLocked(elapsedSeconds, DateTimeOffset.UtcNow);
        }
    }

    internal void ApplyEtwSnapshot(
        IReadOnlyList<ProcessConnectionSnapshot> etwActive,
        IReadOnlyList<ProcessConnectionSnapshot> etwClosed,
        double elapsedSeconds,
        DateTimeOffset nowUtc)
    {
        lock (_stateLock)
        {
            _etwActiveConnections = etwActive.ToArray();
            _etwClosedConnections = etwClosed.ToArray();
            MergeAndComputeSpeedsLocked(elapsedSeconds, nowUtc);
        }
    }

    public IReadOnlyList<ProcessConnectionEvent> IngestEvents(ProcessConnectionEventBatch batch, bool writeToFileLog)
    {
        if (batch.NextSeq > _eventCursor)
            _eventCursor = batch.NextSeq;

        if (batch.Events == null || batch.Events.Count == 0)
            return Array.Empty<ProcessConnectionEvent>();

        var nowUtc = DateTimeOffset.UtcNow;
        lock (_stateLock)
        {
            PruneRecentNativeEventsLocked(nowUtc);
            foreach (var ev in batch.Events)
            {
                if (ev.Seq <= _lastAssignedSeq)
                    ev.Seq = ++_lastAssignedSeq;
                else
                    _lastAssignedSeq = ev.Seq;

                _recentNativeEvents[NormalizeFlowTuple(ev.Pid, ev.Protocol, ev.RemoteIp, ev.RemotePort)] = nowUtc;
            }

            _events.AddRange(batch.Events);
            if (_events.Count > MaxBufferedEvents)
                _events.RemoveRange(0, _events.Count - MaxBufferedEvents);
        }

        WriteLogEvents(batch.Events, writeToFileLog);
        return batch.Events;
    }

    internal IReadOnlyList<ProcessConnectionEvent> IngestDirectEvents(
        IReadOnlyList<ProcessConnectionEvent> directEvents,
        bool writeToFileLog,
        DateTimeOffset? nowUtcOverride = null)
    {
        if (directEvents.Count == 0)
            return Array.Empty<ProcessConnectionEvent>();

        var nowUtc = nowUtcOverride ?? DateTimeOffset.UtcNow;
        var accepted = new List<ProcessConnectionEvent>(directEvents.Count);

        lock (_stateLock)
        {
            PruneRecentNativeEventsLocked(nowUtc);
            var activeNativeKeys = BuildNativeActiveTupleSetLocked();

            foreach (var ev in directEvents)
            {
                var key = NormalizeFlowTuple(ev.Pid, ev.Protocol, ev.RemoteIp, ev.RemotePort);
                if (activeNativeKeys.Contains(key))
                    continue;

                if (_recentNativeEvents.TryGetValue(key, out var seenAt) &&
                    (nowUtc - seenAt).TotalSeconds < 10)
                {
                    continue;
                }

                ev.Seq = ++_lastAssignedSeq;
                accepted.Add(ev);
            }

            if (accepted.Count > 0)
            {
                _events.AddRange(accepted);
                if (_events.Count > MaxBufferedEvents)
                    _events.RemoveRange(0, _events.Count - MaxBufferedEvents);
            }
        }

        if (accepted.Count > 0)
            WriteLogEvents(accepted, writeToFileLog);

        return accepted;
    }

    public static ulong ComputeSpeedBps(ulong currentBytes, ulong previousBytes, double elapsedSeconds)
    {
        if (currentBytes <= previousBytes)
            return 0;

        var dt = Math.Max(elapsedSeconds, 0.25);
        return (ulong)((currentBytes - previousBytes) / dt);
    }

    public static IReadOnlyList<ProcessConnectionSnapshot> FilterConnections(
        IEnumerable<ProcessConnectionSnapshot> items,
        string? query,
        string? protocolFilter) =>
        FilterConnections(items, query, protocolFilter, null);

    public static IReadOnlyList<ProcessConnectionSnapshot> FilterConnections(
        IEnumerable<ProcessConnectionSnapshot> items,
        string? query,
        string? protocolFilter,
        string? routeFilter)
    {
        var filtered = items.Where(conn =>
            (string.IsNullOrEmpty(protocolFilter) ||
             string.Equals(protocolFilter, "ALL", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(conn.Protocol, protocolFilter, StringComparison.OrdinalIgnoreCase)) &&
            MatchesRouteGroup(conn, routeFilter));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            filtered = filtered.Where(conn => MatchesSnapshotQuery(conn, term));
        }

        return filtered.ToArray();
    }

    public static IReadOnlyList<FilteredConnectionEventRow> FilterEvents(
        IEnumerable<ProcessConnectionEvent> items,
        string? query,
        bool useRegex,
        string? actionFilter,
        bool deduplicateConsecutive,
        out string? regexError)
    {
        regexError = null;
        Regex? compiledRegex = null;
        var trimmedQuery = query?.Trim() ?? string.Empty;

        if (trimmedQuery.Length > 0 && useRegex)
        {
            try
            {
                compiledRegex = new Regex(trimmedQuery, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch (ArgumentException e)
            {
                regexError = e.Message;
                return Array.Empty<FilteredConnectionEventRow>();
            }
        }

        var result = new List<FilteredConnectionEventRow>();
        foreach (var ev in items)
        {
            if (!MatchesActionGroup(ev.Action, actionFilter))
                continue;

            if (trimmedQuery.Length > 0)
            {
                if (compiledRegex != null)
                {
                    if (!MatchesEventRegex(ev, compiledRegex))
                        continue;
                }
                else if (!MatchesEventSubstring(ev, trimmedQuery))
                {
                    continue;
                }
            }

            if (deduplicateConsecutive && result.Count > 0 && IsSameConsecutiveEvent(result[^1].Event, ev))
            {
                var prev = result[^1];
                result[^1] = new FilteredConnectionEventRow(ev, prev.Count + 1);
            }
            else
            {
                result.Add(new FilteredConnectionEventRow(ev, 1));
            }
        }

        return result;
    }

    public static string BuildProcessRegexFromName(string processName)
    {
        var fileName = Path.GetFileName(processName);
        if (string.IsNullOrEmpty(fileName))
            fileName = processName;
        return Regex.Escape(fileName) + "$";
    }

    private void HotReloadActiveController()
    {
        NFController? controller;
        lock (_stateLock)
            controller = _nfController;

        if (controller != null && IsProcessModeActive)
            controller.HotReloadRules(PendingHotRules);
    }

    private void EnsurePollingLoop()
    {
        lock (_stateLock)
        {
            if (_pollTask is { IsCompleted: false })
                return;

            StartPollingLoopLocked();
        }
    }

    private void RestartPollingLoop()
    {
        CancellationTokenSource? oldCts;
        lock (_stateLock)
        {
            oldCts = _pollCts;
            _pollCts = null;
            _pollTask = null;
            StartPollingLoopLocked();
        }

        if (oldCts != null)
        {
            try
            {
                oldCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private void StartPollingLoopLocked()
    {
        var cts = new CancellationTokenSource();
        _pollCts = cts;
        _lastPollUtc = DateTimeOffset.UtcNow;
        _pollTask = Task.Run(() => PollLoopAsync(cts));
    }

    private async Task StopPollingAsync()
    {
        CancellationTokenSource? cts;
        Task? pollTask;
        lock (_stateLock)
        {
            cts = _pollCts;
            pollTask = _pollTask;
            _pollCts = null;
            _pollTask = null;
        }

        if (cts != null)
        {
            try
            {
                await cts.CancelAsync();
            }
            catch (ObjectDisposedException)
            {
            }

            if (pollTask != null)
            {
                try
                {
                    await pollTask;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Log.Warning(e, "ConnectionTracker poll loop stop error");
                }
            }
        }
    }

    private async Task StopWhenIdleAsync()
    {
        await StopPollingAsync();
        if (!IsProcessModeActive && !TrackUnmatched)
            await FlushAndStopLogWriterAsync();
    }

    private async Task PollLoopAsync(CancellationTokenSource cts)
    {
        using (cts)
        {
            var ct = cts.Token;
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        PollOnce(IsProcessModeActive, TrackUnmatched, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e, "ConnectionTracker poll error");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private void PollOnce(bool pollNative, bool pollEtw, CancellationToken ct = default)
    {
        List<ProcessConnectionEvent> appended;
        bool shouldNotifyConnections;

        lock (_pollExecLock)
        {
            if (ct.IsCancellationRequested)
                return;

            var now = DateTimeOffset.UtcNow;
            var dt = Math.Max((now - _lastPollUtc).TotalSeconds, 0.25);
            _lastPollUtc = now;

            appended = new List<ProcessConnectionEvent>();
            var writeToFile = Global.Settings.Redirector.LogConnectionsToFile;

            if (pollNative)
            {
                var (down, up) = Interops.Redirector.GetTrafficBytes();
                TotalDownBytes = down;
                TotalUpBytes = up;
                TotalDownSpeedBps = ComputeSpeedBps(down, _lastTotalDown, dt);
                TotalUpSpeedBps = ComputeSpeedBps(up, _lastTotalUp, dt);
                _lastTotalDown = down;
                _lastTotalUp = up;

                if (!IsPaused)
                {
                    var json = Interops.Redirector.GetConnectionsJson();
                    if (!string.IsNullOrEmpty(json))
                    {
                        var dump = JsonSerializer.Deserialize<ProcessConnectionsDump>(json);
                        if (dump != null)
                        {
                            lock (_stateLock)
                            {
                                _nativeActiveConnections = (dump.Active ?? new List<ProcessConnectionSnapshot>()).ToArray();
                                _nativeClosedConnections = (dump.Closed ?? new List<ProcessConnectionSnapshot>()).ToArray();
                            }
                        }
                    }
                }

                for (var i = 0; i < MaxDrainBatchesPerTick; i++)
                {
                    var evJson = Interops.Redirector.DrainConnectionEventsJson(_eventCursor);
                    if (string.IsNullOrEmpty(evJson))
                        break;

                    var batch = JsonSerializer.Deserialize<ProcessConnectionEventBatch>(evJson);
                    if (batch == null)
                        break;

                    var added = IngestEvents(batch, writeToFile);
                    if (added.Count > 0)
                        appended.AddRange(added);

                    if (!batch.HasMore)
                        break;
                }
            }

            if (pollEtw)
            {
                var (etwActive, etwClosed, etwEvents) = _etwMonitor.TakeSnapshot(now, Environment.TickCount64);
                if (!IsPaused)
                {
                    lock (_stateLock)
                    {
                        _etwActiveConnections = etwActive.ToArray();
                        _etwClosedConnections = etwClosed.ToArray();
                    }
                }

                var addedDirect = IngestDirectEvents(etwEvents, writeToFile, now);
                if (addedDirect.Count > 0)
                    appended.AddRange(addedDirect);
            }

            shouldNotifyConnections = !IsPaused && (pollNative || pollEtw);
            if (shouldNotifyConnections)
            {
                lock (_stateLock)
                    MergeAndComputeSpeedsLocked(dt, now);
            }
        }

        if (shouldNotifyConnections)
            ConnectionsUpdated?.Invoke();

        if (appended.Count > 0)
            EventsAppended?.Invoke(appended);
    }

    private void MergeAndComputeSpeedsLocked(double elapsedSeconds, DateTimeOffset nowUtc)
    {
        PruneRecentNativeClosedLocked(nowUtc);
        foreach (var closed in _nativeClosedConnections)
        {
            if (closed.EndUnixMs != 0 &&
                (nowUtc - DateTimeOffset.FromUnixTimeMilliseconds((long)closed.EndUnixMs)).TotalSeconds <= 3)
            {
                _recentNativeClosed[NormalizeFlowTuple(closed.Pid, closed.Protocol, closed.RemoteIp, closed.RemotePort)] = nowUtc;
            }
        }

        var suppressedKeys = BuildNativeActiveTupleSetLocked();
        foreach (var recent in _recentNativeClosed.Keys)
            suppressedKeys.Add(recent);

        var filteredEtwActive = new List<ProcessConnectionSnapshot>(_etwActiveConnections.Length);
        foreach (var conn in _etwActiveConnections)
        {
            if (!suppressedKeys.Contains(NormalizeFlowTuple(conn.Pid, conn.Protocol, conn.RemoteIp, conn.RemotePort)))
                filteredEtwActive.Add(conn);
        }

        var mergedActive = new List<ProcessConnectionSnapshot>(_nativeActiveConnections.Length + filteredEtwActive.Count);
        mergedActive.AddRange(_nativeActiveConnections);
        mergedActive.AddRange(filteredEtwActive);

        var seenIds = new HashSet<ulong>(mergedActive.Count);
        ulong directUpBps = 0;
        ulong directDownBps = 0;
        foreach (var conn in mergedActive)
        {
            seenIds.Add(conn.Id);
            if (_prevBytesByConnId.TryGetValue(conn.Id, out var prev))
            {
                conn.UpSpeedBps = ComputeSpeedBps(conn.UpBytes, prev.Up, elapsedSeconds);
                conn.DownSpeedBps = ComputeSpeedBps(conn.DownBytes, prev.Down, elapsedSeconds);
            }
            else
            {
                conn.UpSpeedBps = ComputeSpeedBps(conn.UpBytes, 0, elapsedSeconds);
                conn.DownSpeedBps = ComputeSpeedBps(conn.DownBytes, 0, elapsedSeconds);
            }

            _prevBytesByConnId[conn.Id] = (conn.UpBytes, conn.DownBytes);

            if (conn.IsDirect)
            {
                directUpBps += conn.UpSpeedBps;
                directDownBps += conn.DownSpeedBps;
            }
        }

        DirectUpSpeedBps = directUpBps;
        DirectDownSpeedBps = directDownBps;

        foreach (var staleId in _prevBytesByConnId.Keys.Where(id => !seenIds.Contains(id)).ToArray())
            _prevBytesByConnId.Remove(staleId);

        var mergedClosed = new List<ProcessConnectionSnapshot>(_nativeClosedConnections.Length + _etwClosedConnections.Length);
        mergedClosed.AddRange(_nativeClosedConnections);
        mergedClosed.AddRange(_etwClosedConnections);
        mergedClosed.Sort((a, b) =>
        {
            var endCmp = b.EndUnixMs.CompareTo(a.EndUnixMs);
            return endCmp != 0 ? endCmp : b.Id.CompareTo(a.Id);
        });

        _activeConnections = mergedActive.ToArray();
        _closedConnections = mergedClosed.ToArray();
    }

    private HashSet<(uint Pid, string Protocol, string RemoteIp, ushort RemotePort)> BuildNativeActiveTupleSetLocked()
    {
        var keys = new HashSet<(uint, string, string, ushort)>(_nativeActiveConnections.Length);
        foreach (var conn in _nativeActiveConnections)
            keys.Add(NormalizeFlowTuple(conn.Pid, conn.Protocol, conn.RemoteIp, conn.RemotePort));
        return keys;
    }

    private void PruneRecentNativeClosedLocked(DateTimeOffset nowUtc)
    {
        foreach (var key in _recentNativeClosed
                     .Where(kv => (nowUtc - kv.Value).TotalSeconds > 3)
                     .Select(kv => kv.Key)
                     .ToArray())
        {
            _recentNativeClosed.Remove(key);
        }
    }

    private void PruneRecentNativeEventsLocked(DateTimeOffset nowUtc)
    {
        foreach (var key in _recentNativeEvents
                     .Where(kv => (nowUtc - kv.Value).TotalSeconds > 10)
                     .Select(kv => kv.Key)
                     .ToArray())
        {
            _recentNativeEvents.Remove(key);
        }
    }

    private void RemovePrevBytesLocked(Func<ulong, bool> predicate)
    {
        foreach (var key in _prevBytesByConnId.Keys.Where(predicate).ToArray())
            _prevBytesByConnId.Remove(key);
    }

    private void WriteLogEvents(IReadOnlyList<ProcessConnectionEvent> events, bool writeToFileLog)
    {
        if (!writeToFileLog)
            return;

        EnsureLogWriterState(true);
        Channel<string>? channel;
        lock (_logWriterLock)
            channel = _logChannel;

        if (channel != null)
        {
            foreach (var ev in events)
                channel.Writer.TryWrite(ev.FormatLogLine());
        }
    }

    private static (uint Pid, string Protocol, string RemoteIp, ushort RemotePort) NormalizeFlowTuple(
        uint pid,
        string protocol,
        string remoteIp,
        ushort remotePort) =>
        (pid, protocol.ToUpperInvariant(), remoteIp.ToLowerInvariant(), remotePort);

    private void EnsureLogWriterState(bool enabled)
    {
        if (!enabled)
        {
            _ = FlushAndStopLogWriterAsync();
            return;
        }

        lock (_logWriterLock)
        {
            if (_logChannel != null)
                return;

            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(8192)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            _logChannel = channel;
            _logWriterTask = Task.Run(() => LogWriterLoopAsync(channel.Reader, LogFilePath));
        }
    }

    private static async Task LogWriterLoopAsync(ChannelReader<string> reader, string filePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 8192, useAsync: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));

            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var line))
                    await writer.WriteLineAsync(line);

                await writer.FlushAsync();
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Connection log writer failed for {Path}", filePath);
        }
    }

    private static bool MatchesSnapshotQuery(ProcessConnectionSnapshot conn, string term) =>
        conn.ProcessName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        conn.ProcessPath.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        conn.ParentName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        conn.Endpoint.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        conn.MatchedRule.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        conn.RuleSource.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        conn.Pid.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesRouteGroup(ProcessConnectionSnapshot conn, string? routeFilter) =>
        routeFilter?.ToUpperInvariant() switch
        {
            "PROXY" => !conn.IsDirect,
            "DIRECT" => conn.IsDirect,
            _ => true
        };

    private static bool MatchesActionGroup(string action, string? filter)
    {
        if (string.IsNullOrEmpty(filter) || string.Equals(filter, "ALL", StringComparison.OrdinalIgnoreCase))
            return true;

        return filter.ToUpperInvariant() switch
        {
            "PROXY" => action is "PROXY" or "DNS_PROXY",
            "BYPASS" => action is "BYPASS_IP" or "BYPASS_PROC" or "DNS_DIRECT" or "DIRECT",
            "CLOSE" => action is "CLOSE",
            "ERROR" => action is "ERROR",
            _ => string.Equals(action, filter, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool MatchesEventSubstring(ProcessConnectionEvent ev, string term) =>
        ev.Action.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.Protocol.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.ProcessName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.ProcessPath.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.ParentName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.Endpoint.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.MatchedRule.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.RuleSource.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.Detail.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        ev.Pid.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesEventRegex(ProcessConnectionEvent ev, Regex regex) =>
        regex.IsMatch(ev.Action) ||
        regex.IsMatch(ev.Protocol) ||
        regex.IsMatch(ev.ProcessName) ||
        regex.IsMatch(ev.ProcessPath) ||
        regex.IsMatch(ev.Endpoint) ||
        regex.IsMatch(ev.MatchedRule) ||
        regex.IsMatch(ev.Detail);

    private static bool IsSameConsecutiveEvent(ProcessConnectionEvent a, ProcessConnectionEvent b) =>
        a.Pid == b.Pid &&
        a.RemotePort == b.RemotePort &&
        string.Equals(a.Action, b.Action, StringComparison.Ordinal) &&
        string.Equals(a.Protocol, b.Protocol, StringComparison.Ordinal) &&
        string.Equals(a.RemoteIp, b.RemoteIp, StringComparison.Ordinal) &&
        string.Equals(a.MatchedRule, b.MatchedRule, StringComparison.Ordinal) &&
        string.Equals(a.ProcessName, b.ProcessName, StringComparison.Ordinal);
}

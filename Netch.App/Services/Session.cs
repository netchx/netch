using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Netch.Controllers;
using Netch.Enums;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.App.Services;

/// <summary>
///     The running scene and its live statistics, shared by the home page and the tray. Lives on the UI
///     thread; core events arrive on other threads and are queued onto it.
/// </summary>
public sealed partial class Session : ObservableObject
{
    public const int RateHistoryLength = 30;

    public static Session Current { get; } = new();

    private readonly Queue<double> _rateHistory = new(Enumerable.Repeat(0d, RateHistoryLength));
    private DispatcherQueue _dispatcher = null!;
    private DispatcherQueueTimer _clock = null!;
    private CancellationTokenSource? _probeCts;
    private Task<string?>? _natTest;
    private TaskCompletionSource? _coreReplacement;
    private ulong _lastUploadedBytes;
    private DateTime _startedAt;
    private Scene? _resumeScene;

    private Session()
    {
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsBusy), nameof(IsRunning))]
    public partial State State { get; private set; }

    /// <summary>The scene that is running, or was started last.</summary>
    [ObservableProperty]
    public partial Scene? ActiveScene { get; private set; }

    /// <summary>Core progress text while starting, e.g. "Starting sing-box".</summary>
    [ObservableProperty]
    public partial string? Progress { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial TimeSpan Elapsed { get; private set; }

    /// <summary>HTTP round trip through the proxy in ms; null while testing or after a timeout.</summary>
    [ObservableProperty]
    public partial int? Latency { get; private set; }

    [ObservableProperty]
    public partial bool LatencyTesting { get; private set; }

    [ObservableProperty]
    public partial string? NatResult { get; private set; }

    [ObservableProperty]
    public partial bool NatTesting { get; private set; }

    [ObservableProperty]
    public partial bool TrafficAvailable { get; private set; }

    [ObservableProperty]
    public partial ulong TotalBytes { get; private set; }

    [ObservableProperty]
    public partial ulong BytesPerSecond { get; private set; }

    [ObservableProperty]
    public partial ulong UploadBytesPerSecond { get; private set; }

    /// <summary>Bytes per second over the last <see cref="RateHistoryLength" /> seconds, oldest first.</summary>
    public IReadOnlyCollection<double> RateHistory => _rateHistory;

    public bool IsIdle => State is State.Waiting or State.Stopped;

    public bool IsBusy => _coreReplacement != null || State is State.Starting or State.Stopping;

    public bool IsRunning => State == State.Started;

    /// <summary>Must run on the UI thread before anything else uses the session.</summary>
    public void Attach(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
        _clock = dispatcher.CreateTimer();
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick += (_, _) => Elapsed = DateTime.Now - _startedAt;

        AppEvents.StatusReported += text => _dispatcher.TryEnqueue(() =>
        {
            if (IsBusy)
                Progress = text;
        });
        AppEvents.BandwidthAvailabilityChanged += available => _dispatcher.TryEnqueue(() => TrafficAvailable = available);
        AppEvents.BandwidthDirectionsUpdated += (downloaded, uploaded) => _dispatcher.TryEnqueue(() => OnTrafficUpdated(downloaded, uploaded));
        SystemEvents.PowerModeChanged += (_, e) => _dispatcher.TryEnqueue(() => OnPowerModeChanged(e.Mode));
    }

    /// <summary>The active or most recently used scene, with the stored selection as the initial default.</summary>
    public Scene? CurrentScene
    {
        get
        {
            var scenes = SceneService.Scenes;
            if (ActiveScene is { } active && scenes.Contains(active))
                return active;

            var recent = scenes.MaxBy(scene => scene.LastUsed ?? DateTime.MinValue);
            return recent?.LastUsed != null ? recent
                : scenes.Find(scene => scene.Id == Global.Settings.StartupSceneId) ?? recent;
        }
    }

    public Task ToggleAsync(Scene scene) => IsRunning && ActiveScene == scene ? StopAsync() : StartAsync(scene);

    /// <summary>Starts <paramref name="scene" />, stopping the running scene first.</summary>
    public async Task StartAsync(Scene scene)
    {
        if (IsBusy || App.IsExiting)
            return;

        if (IsRunning && ConnectionTrackerService.Instance.HasUnsavedHotRules &&
            !await HotRulePersistence.ConfirmBeforeLeaveAsync())
            return;

        if (IsBusy || App.IsExiting)
            return;

        await StartCoreAsync(scene);
    }

    internal async Task StartCoreAsync(Scene scene)
    {
        var server = SceneService.FindServer(scene);
        var mode = SceneService.FindMode(scene);
        if (server == null || mode == null)
        {
            ErrorMessage = i18N.Translate(server == null
                ? "The node of this scene no longer exists. Choose another node."
                : "The mode of this scene no longer exists. Edit the scene to choose another mode.");
            return;
        }

        if (IsRunning)
            await StopCoreAsync();

        ErrorMessage = null;
        ActiveScene = scene;

        State = State.Starting;
        try
        {
            await MainController.StartAsync(server, mode);
        }
        catch (Exception e)
        {
            Progress = null;
            State = State.Stopped;
            ErrorMessage = e.Message;
            return;
        }

        scene.LastUsed = DateTime.Now;
        Configuration.SaveAsync().Forget();
        Progress = null;
        ResetStatistics();
        _startedAt = DateTime.Now;
        Elapsed = TimeSpan.Zero;
        _clock.Start();
        State = State.Started;

        Task.Run(Bandwidth.NetTrafficAsync).Forget();

        _probeCts = new CancellationTokenSource();
        TestLatencyAsync(_probeCts.Token).Forget();
        if (mode is not Models.Modes.WebProxyMode.WebProxyMode)
            TestNatAsync(_probeCts.Token).Forget();
        PingNodeAsync(server, _probeCts.Token).Forget();
    }

    public async Task StopAsync()
    {
        if (IsBusy)
            return;

        if (IsRunning && ConnectionTrackerService.Instance.HasUnsavedHotRules &&
            !await HotRulePersistence.ConfirmBeforeLeaveAsync())
            return;

        if (IsBusy)
            return;

        await StopCoreAsync();
    }

    private static InvalidOperationException SessionBusyError() =>
        new(Helpers.Ui.T("The session is busy. Try again when it is ready.", "场景正在切换，请稍后重试。"));

    private static async Task<bool> RunWithActiveProxyAsync(Func<string, Task> action)
    {
        var activeSocks = MainController.Socks5Server
            ?? throw new InvalidOperationException(Helpers.Ui.T("The temporary proxy could not start.", "临时代理未能启动。"));
        await action(WebUtil.BuildSocks5ProxyUri(activeSocks));
        return true;
    }

    public async Task<bool> RunWithProxyAsync(Func<Task<Server?>> selectNodeAsync, Func<string, Task> action)
    {
        if (IsBusy || App.IsExiting)
            throw SessionBusyError();

        if (IsRunning)
            return await RunWithActiveProxyAsync(action);

        var node = await selectNodeAsync();
        if (node == null)
            return false;

        if (IsBusy || App.IsExiting)
            throw SessionBusyError();

        if (IsRunning)
            return await RunWithActiveProxyAsync(action);

        State = State.Starting;
        try
        {
            await MainController.StartAsync(node);
            return await RunWithActiveProxyAsync(action);
        }
        finally
        {
            await MainController.StopAsync();
            Progress = null;
            State = State.Stopped;
        }
    }

    internal async Task StopCoreAsync()
    {
        if (!IsRunning)
            return;

        State = State.Stopping;
        await CancelProbesAsync();
        await MainController.StopAsync();
        _clock.Stop();
        Bandwidth.Stop();
        TrafficAvailable = false;
        State = State.Stopped;
    }

    /// <summary>Stops whatever the core is running, including a start still in progress. Used on exit.</summary>
    public async Task ShutdownAsync()
    {
        ConnectionTrackerService.Instance.PendingHotRules.Clear();

        if (_coreReplacement is { } replacement)
            await replacement.Task;

        await CancelProbesAsync();
        await MainController.StopAsync();
        Bandwidth.Stop();
    }

    public async Task<string> TestNodeNatAsync(Server server)
    {
        if (_coreReplacement != null)
            throw new InvalidOperationException(Helpers.Ui.T("Wait for the core update to finish.", "请等待核心更新完成。"));

        if (IsRunning && SceneService.FindServer(ActiveScene!)?.Id == server.Id)
        {
            if (_probeCts is { } probes && await TestNatAsync(probes.Token) is { } result)
                return result;
            return i18N.Translate("NAT test cancelled.");
        }

        if (!IsIdle)
            throw new InvalidOperationException(i18N.Translate("Disconnect before testing another node."));

        State = State.Starting;
        try
        {
            await MainController.StartAsync(server);
            var result = await MainController.DiscoveryNatTypeAsync();
            return result.Result ?? "Error";
        }
        finally
        {
            await MainController.StopAsync();
            State = State.Stopped;
        }
    }

    internal async Task RunCoreReplacementAsync(Func<Task> replace)
    {
        if (IsBusy || App.IsExiting)
            throw new InvalidOperationException(Helpers.Ui.T("The session is busy. Try updating again when it is ready.",
                "场景正在切换，请在切换完成后重试更新。"));

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _coreReplacement = completion;
        try
        {
            OnPropertyChanged(nameof(IsBusy));
            await replace();
        }
        finally
        {
            _coreReplacement = null;
            completion.SetResult();
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    public async Task RetestLatencyAsync()
    {
        if (IsRunning && !LatencyTesting && _probeCts != null)
            await TestLatencyAsync(_probeCts.Token);
    }

    public async Task RetestNatAsync()
    {
        if (IsRunning && _probeCts != null)
            await TestNatAsync(_probeCts.Token);
    }

    partial void OnStateChanged(State value)
    {
        AppEvents.State = value;
        DelayTestHelper.Enabled = IsIdle;
    }

    private void ResetStatistics()
    {
        Latency = null;
        NatResult = null;
        TotalBytes = BytesPerSecond = UploadBytesPerSecond = _lastUploadedBytes = 0;
        TrafficAvailable = false;
        _rateHistory.Clear();
        for (var i = 0; i < RateHistoryLength; i++)
            _rateHistory.Enqueue(0);
        OnPropertyChanged(nameof(RateHistory));
    }

    private void OnTrafficUpdated(ulong downloaded, ulong uploaded)
    {
        if (!IsRunning)
            return;

        BytesPerSecond = downloaded >= TotalBytes ? downloaded - TotalBytes : 0;
        UploadBytesPerSecond = uploaded >= _lastUploadedBytes ? uploaded - _lastUploadedBytes : 0;
        TotalBytes = downloaded;
        _lastUploadedBytes = uploaded;

        _rateHistory.Dequeue();
        _rateHistory.Enqueue(BytesPerSecond);
        OnPropertyChanged(nameof(RateHistory));
    }

    private async Task TestLatencyAsync(CancellationToken token)
    {
        LatencyTesting = true;
        try
        {
            var latency = await MainController.HttpConnectAsync(token);
            if (!token.IsCancellationRequested)
                Latency = latency;
        }
        finally
        {
            LatencyTesting = false;
        }
    }

    private Task<string?> TestNatAsync(CancellationToken token) =>
        _natTest is { IsCompleted: false } running ? running : _natTest = MeasureNatAsync(token);

    private async Task<string?> MeasureNatAsync(CancellationToken token)
    {
        NatTesting = true;
        try
        {
            var result = await MainController.DiscoveryNatTypeAsync(token);
            if (!token.IsCancellationRequested)
                return NatResult = result.Result ?? "Error";
            return null;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception e)
        {
            Log.Warning(e, "NAT type test failed");
            if (token.IsCancellationRequested)
                return null;
            return NatResult = "Error";
        }
        finally
        {
            NatTesting = false;
        }
    }

    /// <summary>Keeps the active node's delay current while connected.</summary>
    private static async Task PingNodeAsync(Server server, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (Global.Settings.StartedPingInterval >= 0)
                {
                    var previous = server.Delay;
                    if (await server.PingAsync(token) != previous)
                        AppEvents.RaiseNodeDelaysChanged();
                    await Task.Delay(Math.Max(1, Global.Settings.StartedPingInterval) * 1000, token);
                }
                else
                {
                    await Task.Delay(5000, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task CancelProbesAsync()
    {
        if (_probeCts == null)
            return;

        await _probeCts.CancelAsync();
        if (_natTest is { } natTest)
            await natTest;
        _natTest = null;
        _probeCts.Dispose();
        _probeCts = null;
    }

    private async void OnPowerModeChanged(PowerModes mode)
    {
        switch (mode)
        {
            case PowerModes.Suspend when IsRunning:
                Log.Information("OS Suspend, Stop");
                _resumeScene = ActiveScene;
                await StopCoreAsync();
                break;
            case PowerModes.Resume when _resumeScene != null:
                Log.Information("OS Resume, Restart");
                var scene = _resumeScene;
                _resumeScene = null;
                await StartCoreAsync(scene);
                break;
        }
    }
}

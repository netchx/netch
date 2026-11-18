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
    private ulong _lastTotalBytes;
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

    /// <summary>Bytes per second over the last <see cref="RateHistoryLength" /> seconds, oldest first.</summary>
    public IReadOnlyCollection<double> RateHistory => _rateHistory;

    public bool IsIdle => State is State.Waiting or State.Stopped;

    public bool IsBusy => State is State.Starting or State.Stopping;

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
        AppEvents.BandwidthUpdated += total => _dispatcher.TryEnqueue(() => OnTrafficUpdated(total));
        SystemEvents.PowerModeChanged += (_, e) => _dispatcher.TryEnqueue(() => OnPowerModeChanged(e.Mode));
    }

    /// <summary>The scene the power button acts on: the running or last started one, else the most recently used.</summary>
    public Scene? CurrentScene => ActiveScene ?? SceneService.Scenes.MaxBy(s => s.LastUsed ?? DateTime.MinValue);

    public Task ToggleAsync(Scene scene) => IsRunning && ActiveScene == scene ? StopAsync() : StartAsync(scene);

    /// <summary>Starts <paramref name="scene" />, stopping the running scene first.</summary>
    public async Task StartAsync(Scene scene)
    {
        if (IsBusy)
            return;

        if (IsRunning)
            await StopAsync();

        var server = SceneService.FindServer(scene);
        var mode = SceneService.FindMode(scene);
        if (server == null || mode == null)
        {
            ErrorMessage = i18N.Translate(server == null
                ? "The node of this scene no longer exists. Choose another node."
                : "The mode of this scene no longer exists. Edit the scene to choose another mode.");
            return;
        }

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
        TestNatAsync(_probeCts.Token).Forget();
        PingNodeAsync(server, _probeCts.Token).Forget();
    }

    public async Task StopAsync()
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
        await CancelProbesAsync();
        await MainController.StopAsync();
        Bandwidth.Stop();
    }

    public async Task RetestLatencyAsync()
    {
        if (IsRunning && !LatencyTesting && _probeCts != null)
            await TestLatencyAsync(_probeCts.Token);
    }

    public async Task RetestNatAsync()
    {
        if (IsRunning && !NatTesting && _probeCts != null)
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
        TotalBytes = BytesPerSecond = _lastTotalBytes = 0;
        TrafficAvailable = false;
        _rateHistory.Clear();
        for (var i = 0; i < RateHistoryLength; i++)
            _rateHistory.Enqueue(0);
        OnPropertyChanged(nameof(RateHistory));
    }

    private void OnTrafficUpdated(ulong totalBytes)
    {
        if (!IsRunning)
            return;

        BytesPerSecond = totalBytes >= _lastTotalBytes ? totalBytes - _lastTotalBytes : 0;
        _lastTotalBytes = totalBytes;
        TotalBytes = totalBytes;

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

    private async Task TestNatAsync(CancellationToken token)
    {
        NatTesting = true;
        try
        {
            var result = await MainController.DiscoveryNatTypeAsync(token);
            if (!token.IsCancellationRequested)
                NatResult = result.Result ?? "Error";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Log.Warning(e, "NAT type test failed");
            NatResult = "Error";
        }
        finally
        {
            NatTesting = false;
        }
    }

    /// <summary>Keeps the node's delay current while connected, as the WinForms front end does.</summary>
    private static async Task PingNodeAsync(Server server, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (Global.Settings.StartedPingInterval >= 0)
                {
                    await server.PingAsync();
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
                await StopAsync();
                break;
            case PowerModes.Resume when _resumeScene != null:
                Log.Information("OS Resume, Restart");
                var scene = _resumeScene;
                _resumeScene = null;
                await StartAsync(scene);
                break;
        }
    }
}

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.Threading;
using Netch.App.Services;
using Netch.Enums;
using Netch.Services;
using Netch.Utils;

namespace Netch.App;

public partial class App : Application
{
    private static bool _closeHintShown;
    private static WelcomeWindow? _wizard;
    private DispatcherQueueTimer? _subscriptions;
    private bool _updatingSubscriptions;

    public App()
    {
        InitializeComponent();
        // The process lives in the tray; it ends only through ExitAsync.
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
        UnhandledException += (_, e) => Log.Error(e.Exception, "Unhandled UI exception");
    }

    public static MainWindow? MainWindow { get; private set; }

    public static bool IsExiting { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ModeService.Instance.Load(completeSceneDefaults: true);
        if (SceneService.Initialize())
            Configuration.SaveAsync().Forget();

        DelayTestHelper.UpdateTick(true);
        Session.Current.Attach(DispatcherQueue.GetForCurrentThread());
        Session.Current.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Session.State) && Session.Current.IsRunning && Global.Settings.MinimizeWhenStarted)
                HideToTray();
        };

        MainWindow = new MainWindow();
        Tray.Current.Initialize();
        Program.SingleInstance.StartListening(OnInstanceMessage);
        MainWindow.Activate();

        if (!Global.Settings.OnboardingCompleted && SceneService.Scenes.Count == 0 && Global.Settings.Server.Count == 0)
            OpenWizard();

        var startupScene = (Global.Settings.StartupSceneId is { } startupId
            ? SceneService.Scenes.FirstOrDefault(scene => scene.Id == startupId)
            : null) ?? Session.Current.CurrentScene;
        if (Global.Settings.StartWhenOpened && startupScene != null)
            Session.Current.StartAsync(startupScene).Forget();

        _subscriptions = MainWindow.DispatcherQueue.CreateTimer();
        _subscriptions.Interval = TimeSpan.FromMinutes(1);
        _subscriptions.Tick += (_, _) => UpdateSubscriptionsAsync(false).Forget();
        _subscriptions.Start();
        UpdateSubscriptionsAsync(Global.Settings.UpdateServersWhenOpened).Forget();

        if (Global.Settings.CheckUpdateWhenOpened)
            Downloads.CheckAsync().Forget();
    }

    private async Task UpdateSubscriptionsAsync(bool all)
    {
        if (_updatingSubscriptions)
            return;
        _updatingSubscriptions = true;
        try
        {
            var due = Global.Settings.Subscription.Where(s => s.Enable && (all || s.UpdateIntervalHours > 0 &&
                (s.LastChecked == null || DateTimeOffset.UtcNow - s.LastChecked.Value >= TimeSpan.FromHours(s.UpdateIntervalHours)))).ToArray();
            foreach (var item in due)
                await SubscriptionUtil.UpdateServerAsync(item);
            if (due.Length > 0)
                await Configuration.SaveAsync();
        }
        finally { _updatingSubscriptions = false; }
    }

    public static void OpenWizard()
    {
        if (_wizard == null)
        {
            _wizard = new WelcomeWindow();
            _wizard.Closed += (_, _) => _wizard = null;
        }
        _wizard.Activate();
    }

    public static void ShowMainWindow() => MainWindow?.BringToFront();

    public static void HideToTray()
    {
        if (MainWindow is not { AppWindow.IsVisible: true })
            return;

        MainWindow.AppWindow.Hide();
        if (_closeHintShown)
            return;

        _closeHintShown = true;
        Tray.Current.ShowBalloon(i18N.Translate("Netch is still running in the notification area."));
    }

    public static void ApplyTheme(AppTheme theme)
    {
        MainWindow?.ApplyTheme(theme);
        var requestedTheme = MainWindow?.Theme ?? ElementTheme.Default;
        Tray.Current.ApplyTheme(requestedTheme);
        _wizard?.ApplyTheme(requestedTheme);
    }

    /// <summary>Stops the proxy, saves the configuration and ends the process.</summary>
    public static async Task ExitAsync()
    {
        if (IsExiting)
            return;

        IsExiting = true;
        (Current as App)?._subscriptions?.Stop();
        _wizard?.Close();
        MainWindow?.AppWindow.Hide();
        await Session.Current.ShutdownAsync();
        await Configuration.SaveAsync();

        foreach (var file in new[] { Constants.TempConfig, Constants.TempRouteFile })
            if (File.Exists(file))
                File.Delete(file);

        Tray.Current.Close();
        Current.Exit();
    }

    private static void OnInstanceMessage(int command)
    {
        if (command == Constants.Parameter.Show)
            MainWindow?.DispatcherQueue.TryEnqueue(ShowMainWindow);
    }
}

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
        ModeService.Instance.Load();
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
        Program.SingleInstance.Received.Subscribe(OnInstanceMessage);
        Program.SingleInstance.StartListenServer();
        MainWindow.Activate();

        if (Global.Settings.StartWhenOpened && Session.Current.CurrentScene is { } scene)
            Session.Current.StartAsync(scene).Forget();
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
        Tray.Current.ApplyTheme(MainWindow?.Theme ?? ElementTheme.Default);
    }

    /// <summary>Stops the proxy, saves the configuration and ends the process.</summary>
    public static async Task ExitAsync()
    {
        if (IsExiting)
            return;

        IsExiting = true;
        MainWindow?.AppWindow.Hide();
        await Session.Current.ShutdownAsync();
        await Configuration.SaveAsync();

        foreach (var file in new[] { Constants.TempConfig, Constants.TempRouteFile })
            if (File.Exists(file))
                File.Delete(file);

        Tray.Current.Close();
        Current.Exit();
    }

    private static void OnInstanceMessage((string, Action<string>) message)
    {
        var (arg, reply) = message;
        if (arg == Constants.Parameter.Show)
            MainWindow?.DispatcherQueue.TryEnqueue(ShowMainWindow);

        reply(string.Empty);
    }
}

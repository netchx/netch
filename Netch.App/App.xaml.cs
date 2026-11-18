using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.Threading;
using Netch.App.Services;
using Netch.Services;
using Netch.Utils;

namespace Netch.App;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => Log.Error(e.Exception, "Unhandled UI exception");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ModeService.Instance.Load();
        if (SceneService.Initialize())
            Configuration.SaveAsync().Forget();

        DelayTestHelper.UpdateTick(true);
        Session.Current.Attach(DispatcherQueue.GetForCurrentThread());

        MainWindow = new MainWindow();
        Program.SingleInstance.Received.Subscribe(OnInstanceMessage);
        Program.SingleInstance.StartListenServer();
        MainWindow.Activate();

        if (Global.Settings.StartWhenOpened && Session.Current.CurrentScene is { } scene)
            Session.Current.StartAsync(scene).Forget();
    }

    /// <summary>Stops the proxy and saves the configuration before the process ends.</summary>
    public static async Task ShutdownAsync()
    {
        await Session.Current.ShutdownAsync();
        await Configuration.SaveAsync();

        foreach (var file in new[] { Constants.TempConfig, Constants.TempRouteFile })
            if (File.Exists(file))
                File.Delete(file);
    }

    private static void OnInstanceMessage((string, Action<string>) message)
    {
        var (arg, reply) = message;
        if (arg == Constants.Parameter.Show)
            MainWindow?.DispatcherQueue.TryEnqueue(() => MainWindow.BringToFront());

        reply(string.Empty);
    }
}

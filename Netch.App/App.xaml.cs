using Microsoft.UI.Xaml;

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
        MainWindow = new MainWindow();
        Program.SingleInstance.Received.Subscribe(OnInstanceMessage);
        Program.SingleInstance.StartListenServer();
        MainWindow.Activate();
    }

    private static void OnInstanceMessage((string, Action<string>) message)
    {
        var (arg, reply) = message;
        if (arg == Constants.Parameter.Show)
            MainWindow?.DispatcherQueue.TryEnqueue(() => MainWindow.BringToFront());

        reply(string.Empty);
    }
}

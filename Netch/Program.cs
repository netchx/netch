using System.Reflection;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.VisualStudio.Threading;
using Netch.Enums;
using Netch.Forms;
using Netch.Services;
using Netch.Utils;
using SingleInstance;
#if RELEASE
using Windows.Win32.UI.WindowsAndMessaging;
#endif

namespace Netch;

public static class Program
{
    public static readonly ISingleInstanceService SingleInstance = new SingleInstanceService($"Global\\{nameof(Netch)}");

    internal static HWND ConsoleHwnd { get; private set; }

#pragma warning disable VSTHRD002
    // VSTHRD002: Avoid problematic synchronous waits
    // Main never re-called, so we can ignore this

    [STAThread]
    public static void Main(string[] args)
    {
        // handle arguments
        if (args.Contains(Constants.Parameter.ForceUpdate))
            Flags.AlwaysShowNewVersionFound = true;

        Bootstrap.SetWorkingDirectory();

#if !DEBUG
        if (!Bootstrap.BinariesPresent)
        {
            i18N.Load("System");
            MessageBoxX.Show(i18N.Translate("Please extract all files then run the program!"));
            Environment.Exit(2);
        }
#endif
        Bootstrap.PrepareDataDirectories();

        // load configuration
        Configuration.LoadAsync().Wait();

        // check if the program is already running
        if (!SingleInstance.TryStartSingleInstance())
        {
            SingleInstance.SendMessageToFirstInstanceAsync(Constants.Parameter.Show).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        SingleInstance.Received.Subscribe(SingleInstance_ArgumentsReceived);

        InitConsole();

        Bootstrap.StartLogging();

        // load i18n
        i18N.Load(Global.Settings.Language);

        // log environment information
        Bootstrap.LogEnvironmentAsync().Forget();
        CheckClr();
        CheckOS();

        // handle exceptions
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += Application_OnException;
        Application.ApplicationExit += Application_OnExit;

        Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(MainForm.Instance);
    }

#pragma warning restore VSTHRD002

    private static void CheckClr()
    {
        var framework = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (framework == null)
        {
            Log.Warning("TargetFrameworkAttribute null");
            return;
        }

        var frameworkName = new FrameworkName(framework);

        if (frameworkName.Version.Major != Environment.Version.Major)
        {
            Log.Information("CLR: {Version}", Environment.Version);
            Flags.NoSupport = true;
            if (!Global.Settings.NoSupportDialog)
                MessageBoxX.Show(
                    i18N.TranslateFormat("{0} won't get developers' support, Please do not report any issues or seek help from developers.",
                        "CLR " + Environment.Version),
                    LogLevel.WARNING);
        }
    }

    private static void CheckOS()
    {
        if (Environment.OSVersion.Version.Build < 17763)
        {
            Flags.NoSupport = true;
            if (!Global.Settings.NoSupportDialog)
                MessageBoxX.Show(
                    i18N.TranslateFormat("{0} won't get developers' support, Please do not report any issues or seek help from developers.",
                        Environment.OSVersion),
                    LogLevel.WARNING);
        }
    }

    private static void InitConsole()
    {
        PInvoke.AllocConsole();

        ConsoleHwnd = PInvoke.GetConsoleWindow();
#if RELEASE
        // hide console window
        PInvoke.ShowWindow(ConsoleHwnd, SHOW_WINDOW_CMD.SW_HIDE);
#endif
    }

    private static void Application_OnException(object sender, ThreadExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled error");
    }

    private static void Application_OnExit(object? sender, EventArgs eventArgs)
    {
        Log.CloseAndFlush();
    }

    private static void SingleInstance_ArgumentsReceived((string, Action<string>) receive)
    {
        var (arg, endFunc) = receive;
        if (arg == Constants.Parameter.Show)
        {
            MainForm.ActivateVisibleWindows();
        }

        endFunc(string.Empty);
    }
}
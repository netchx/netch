using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Netch.Services;
using Netch.Utils;
using SingleInstance;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Netch.App;

public static class Program
{
    /// <summary>Single-instance mutex key so only one Netch instance runs at a time.</summary>
    public static readonly SingleInstanceService SingleInstance = new($"Global\\{nameof(Netch)}");

#pragma warning disable VSTHRD002 // Main runs once; blocking here is intended.
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains(Constants.Parameter.ForceUpdate))
            Flags.AlwaysShowNewVersionFound = true;

        Bootstrap.SetWorkingDirectory();

        if (!EnvironmentChecker.IsOsSupported)
        {
            i18N.Load("System");
            PInvoke.MessageBox(HWND.Null,
                i18N.Translate("Netch requires Windows 10 version 1809 (Build 17763) or Windows 11 (x64)."),
                "Netch",
                MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            Environment.Exit(3);
        }

#if !DEBUG
        if (!Bootstrap.BinariesPresent)
        {
            i18N.Load("System");
            PInvoke.MessageBox(HWND.Null, i18N.Translate("Please extract all files then run the program!"), "Netch",
                MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
            Environment.Exit(2);
        }
#endif
        if (!SingleInstance.IsFirstInstance)
        {
            try { SingleInstance.SendMessageAsync(Constants.Parameter.Show).GetAwaiter().GetResult(); }
            catch (Exception e) when (e is TimeoutException or IOException) { Log.Warning(e, "Could not activate the running instance"); }
            return;
        }

        Bootstrap.PrepareDataDirectories();
        Configuration.LoadAsync().GetAwaiter().GetResult();
        Bootstrap.StartLogging();
        Bootstrap.RecoverStaleSystemProxy();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => SystemProxyStateStore.RecoverStaleState();
        i18N.Load(Global.Settings.Language);
        Bootstrap.LogEnvironmentAsync().Forget();

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            new App();
        });

        SingleInstance.Dispose();
        Log.CloseAndFlush();
    }
#pragma warning restore VSTHRD002
}

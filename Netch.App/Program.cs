using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.Threading;
using Netch.Services;
using Netch.Utils;
using SingleInstance;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Netch.App;

public static class Program
{
    /// <summary>Same key as the WinForms front end, so the two never run at the same time.</summary>
    public static readonly SingleInstanceService SingleInstance = new($"Global\\{nameof(Netch)}");

#pragma warning disable VSTHRD002 // Main runs once; blocking here is intended.
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains(Constants.Parameter.ForceUpdate))
            Flags.AlwaysShowNewVersionFound = true;

        Bootstrap.SetWorkingDirectory();

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

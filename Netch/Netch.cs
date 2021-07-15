using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.VisualStudio.Threading;
using Netch.Controllers;
using Netch.Forms;
using Netch.Services;
using Netch.Utils;
using Serilog;
using Serilog.Events;
using SingleInstance;
#if RELEASE
using Windows.Win32.UI.WindowsAndMessaging;
#endif

namespace Netch
{
    public static class Netch
    {
        public static readonly SingleInstanceService SingleInstance = new($"Global\\{nameof(Netch)}");

        internal static HWND ConsoleHwnd { get; private set; }

        /// <summary>
        ///     应用程序的主入口点
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Contains(Constants.Parameter.ForceUpdate))
                Flags.AlwaysShowNewVersionFound = true;

            // 设置当前目录
            Directory.SetCurrentDirectory(Global.NetchDir);
            var binPath = Path.Combine(Global.NetchDir, "bin");
            Environment.SetEnvironmentVariable("PATH", $"{Environment.GetEnvironmentVariable("PATH")};{binPath}");

            Updater.CleanOld(Global.NetchDir);

            // 预创建目录
            var directories = new[] { "mode\\Custom", "data", "i18n", "logging" };
            foreach (var item in directories)
                if (!Directory.Exists(item))
                    Directory.CreateDirectory(item);

            // 加载配置
#pragma warning disable VSTHRD002
            Configuration.LoadAsync().Wait();
#pragma warning restore VSTHRD002

            if (!SingleInstance.IsFirstInstance)
            {
                SingleInstance.PassArgumentsToFirstInstance(args.Append(Constants.Parameter.Show));
                Environment.Exit(0);
                return;
            }

            SingleInstance.ArgumentsReceived.Subscribe(SingleInstance_ArgumentsReceived);

            // 清理上一次的日志文件，防止淤积占用磁盘空间
            if (Directory.Exists("logging"))
            {
                var directory = new DirectoryInfo("logging");

                foreach (var file in directory.GetFiles())
                    file.Delete();

                foreach (var dir in directory.GetDirectories())
                    dir.Delete(true);
            }

            InitConsole();

            CreateLogger();

            // 加载语言
            i18N.Load(Global.Settings.Language);

            if (!Directory.Exists("bin") || !Directory.EnumerateFileSystemEntries("bin").Any())
            {
                MessageBoxX.Show(i18N.Translate("Please extract all files then run the program!"));
                Environment.Exit(2);
            }

            Task.Run(LogEnvironment).Forget();

            // 绑定错误捕获
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_OnException;
            Application.ApplicationExit += Application_OnExit;

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(Global.MainForm);
        }

        private static void LogEnvironment()
        {
            Log.Information("Netch Version: {Version}", $"{UpdateChecker.Owner}/{UpdateChecker.Repo}@{UpdateChecker.Version}");
            Log.Information("Environment: {OSVersion}", Environment.OSVersion);
            Log.Information("SHA256: {Hash}", $"{Utils.Utils.SHA256CheckSum(Global.NetchExecutable)}");
            Log.Information("System Language: {Language}", CultureInfo.CurrentCulture.Name);
            if (Log.IsEnabled(LogEventLevel.Debug))
                Log.Debug("Third-party Drivers:\n{Drivers}", string.Join("\n", SystemInfo.SystemDrivers(false)));
        }

        private static void InitConsole()
        {
            PInvoke.AllocConsole();

            ConsoleHwnd = PInvoke.GetConsoleWindow();
#if RELEASE
            PInvoke.ShowWindow(ConsoleHwnd, SHOW_WINDOW_CMD.SW_HIDE);
#endif
        }

        public static void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Verbose()
                .WriteTo.Async(c => c.Debug(outputTemplate: Constants.OutputTemplate))
#else
                .MinimumLevel.Debug()
#endif
                .WriteTo.Async(c => c.File(Path.Combine(Global.NetchDir, Constants.LogFile),
                    outputTemplate: Constants.OutputTemplate,
                    rollOnFileSizeLimit: false))
                .MinimumLevel.Override(@"Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .CreateLogger();
        }

        private static void Application_OnException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error(e.Exception, "未处理异常");
        }

        private static void Application_OnExit(object? sender, EventArgs eventArgs)
        {
            Log.CloseAndFlush();
        }

        private static void SingleInstance_ArgumentsReceived(IEnumerable<string> args)
        {
            if (args.Contains(Constants.Parameter.Show))
            {
                Global.MainForm.ShowMainFormToolStripButton_Click(null!, null!);
            }
        }
    }
}
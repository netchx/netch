using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Forms;
using Netch.Utils;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace Netch
{
    public static class Netch
    {
        /// <summary>
        ///     应用程序的主入口点
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Contains("-console"))
                if (!NativeMethods.AttachConsole(-1))
                    NativeMethods.AllocConsole();

            // 设置当前目录
            Directory.SetCurrentDirectory(Global.NetchDir);
            Environment.SetEnvironmentVariable("PATH",
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) + ";" + Path.Combine(Global.NetchDir, "bin"),
                EnvironmentVariableTarget.Process);

            Updater.Updater.CleanOld(Global.NetchDir);

            // 预创建目录
            var directories = new[] {"mode\\Custom", "data", "i18n", "logging"};
            foreach (var item in directories)
                if (!Directory.Exists(item))
                    Directory.CreateDirectory(item);

            // 加载配置
            Configuration.Load();

            // 检查是否已经运行
            if (!Global.Mutex.WaitOne(0, false))
            {
                ShowOpened();

                // 退出进程
                Environment.Exit(1);
            }

            // 清理上一次的日志文件，防止淤积占用磁盘空间
            if (Directory.Exists("logging"))
            {
                var directory = new DirectoryInfo("logging");

                foreach (var file in directory.GetFiles())
                    file.Delete();

                foreach (var dir in directory.GetDirectories())
                    dir.Delete(true);
            }

            // 加载语言
            i18N.Load(Global.Settings.Language);

            if (!Directory.Exists("bin") || !Directory.EnumerateFileSystemEntries("bin").Any())
            {
                MessageBoxX.Show(i18N.Translate("Please extract all files then run the program!"));
                Environment.Exit(2);
            }

            Logging.Info($"版本: {UpdateChecker.Owner}/{UpdateChecker.Repo}@{UpdateChecker.Version}");
            Task.Run(() => { Logging.Info($"主程序 SHA256: {Utils.Utils.SHA256CheckSum(Global.NetchExecutable)}"); });

            // 绑定错误捕获
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_OnException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(Global.MainForm);
        }

        public static void Application_OnException(object sender, ThreadExceptionEventArgs e)
        {
            Logging.Error(e.Exception.ToString());
            Utils.Utils.Open(Logging.LogFile);
        }

        private static void ShowOpened()
        {
            HWND GetWindowHandleByPidAndTitle(int process, string title)
            {
                var sb = new StringBuilder(256);
                HWND pLast = IntPtr.Zero;
                do
                {
                    pLast = FindWindowEx(HWND.NULL, pLast, null, null);
                    GetWindowThreadProcessId(pLast, out var id);
                    if (id != process)
                        continue;

                    if (GetWindowText(pLast, sb, sb.Capacity) <= 0)
                        continue;

                    if (sb.ToString().Equals(title))
                        return pLast;
                } while (pLast != IntPtr.Zero);

                return HWND.NULL;
            }

            var self = Process.GetCurrentProcess();
            var activeProcess = Process.GetProcessesByName("Netch").Single(p => p.Id != self.Id);
            HWND handle = activeProcess.MainWindowHandle;
            if (handle.IsNull)
                handle = GetWindowHandleByPidAndTitle(activeProcess.Id, "Netch");

            if (handle.IsNull)
                return;

            ShowWindow(handle, ShowWindowCommand.SW_NORMAL);
            SwitchToThisWindow(handle, true);
        }
    }
}
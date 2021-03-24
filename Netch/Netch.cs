using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;

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
#if DEBUG
            AttachConsole();
#else
            if (args.Contains("-console"))
                AttachConsole();
#endif
            Global.LogStopwatch = new LogStopwatch("Netch");

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

            Global.LogStopwatch.Log("Clean Old, Create Directory");
            // 加载配置
            Configuration.Load();

            Global.LogStopwatch.Log("Load Configuration");

            if (!Global.SingleInstance.IsFirstInstance)
            {
                Global.SingleInstance.PassArgumentsToFirstInstance(args.Append(Global.ParameterShow));
                Environment.Exit(0);
                return;
            }

            Global.SingleInstance.ArgumentsReceived.Subscribe(SingleInstance_ArgumentsReceived);
            Global.SingleInstance.ListenForArgumentsFromSuccessiveInstances();

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

            Global.LogStopwatch.Log("Get Info, Pre-Form");

            // 绑定错误捕获
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_OnException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(Global.MainForm);
        }

        private static void AttachConsole()
        {
            if (!NativeMethods.AttachConsole(-1))
                NativeMethods.AllocConsole();
        }

        public static void Application_OnException(object sender, ThreadExceptionEventArgs e)
        {
            Logging.Error(e.Exception.ToString());
            Utils.Utils.Open(Logging.LogFile);
        }

        private static void SingleInstance_ArgumentsReceived(IEnumerable<string> args)
        {
            if (args.Contains(Global.ParameterShow))
            {
                Global.MainForm.ShowMainFormToolStripButton_Click(null!, null!);
            }
        }
    }
}
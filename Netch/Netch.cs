using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Forms;
using Netch.Utils;

namespace Netch
{
    public static class Netch
    {
        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Contains("-console"))
            {
                if (!NativeMethods.AttachConsole(-1))
                {
                    NativeMethods.AllocConsole();
                }
            }

            // 创建互斥体防止多次运行
            using (var mutex = new Mutex(false, "Global\\Netch"))
            {
                // 设置当前目录
                Directory.SetCurrentDirectory(Global.NetchDir);
                Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) + ";" + Path.Combine(Global.NetchDir, "bin"), EnvironmentVariableTarget.Process);

                // 预创建目录
                var directories = new[] {"mode", "data", "i18n", "logging"};
                foreach (var item in directories)
                {
                    if (!Directory.Exists(item))
                    {
                        Directory.CreateDirectory(item);
                    }
                }

                // 加载配置
                Configuration.Load();

                // 加载语言
                i18N.Load(Global.Settings.Language);

                // 检查是否已经运行
                if (!mutex.WaitOne(0, false))
                {
                    OnlyInstance.Send(OnlyInstance.Commands.Show);
                    Logging.Info("唤起单实例");

                    // 退出进程
                    Environment.Exit(1);
                }

                // 清理上一次的日志文件，防止淤积占用磁盘空间
                if (Directory.Exists("logging"))
                {
                    var directory = new DirectoryInfo("logging");

                    foreach (var file in directory.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (var dir in directory.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

                Logging.Info($"版本: {UpdateChecker.Owner}/{UpdateChecker.Repo}@{UpdateChecker.Version}");
                Task.Run(() =>
                {
                    Logging.Info($"主程序 SHA256: {Utils.Utils.SHA256CheckSum(Application.ExecutablePath)}");
                });
                Task.Run(() =>
                {
                    Logging.Info("启动单实例");
                    OnlyInstance.Server();
                });

                // 绑定错误捕获
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_OnException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(Global.MainForm = new MainForm());
            }
        }

        public static void Application_OnException(object sender, ThreadExceptionEventArgs e)
        {
            Logging.Error(e.Exception.ToString());
            Utils.Utils.Open(Logging.LogFile);
        }
    }
}
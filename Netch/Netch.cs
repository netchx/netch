using System;
using System.IO;
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
            // 创建互斥体防止多次运行
            using (var mutex = new Mutex(false, "Global\\Netch"))
            {
                // 设置当前目录
                Directory.SetCurrentDirectory(Global.NetchDir);

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

                // 预创建目录
                var directories = new[] {"mode", "data", "i18n", "logging"};
                foreach (var item in directories)
                {
                    // 检查是否已经存在
                    if (!Directory.Exists(item))
                    {
                        // 创建目录
                        Directory.CreateDirectory(item);
                    }
                }

                // 加载配置
                Configuration.Load();

                // 加载语言
                i18N.Load(Global.Settings.Language);

                Task.Run(() =>
                {
                    Logging.Info($"版本: {UpdateChecker.Owner}/{UpdateChecker.Repo}@{UpdateChecker.Version}");
                    Logging.Info($"主程序 SHA256: {Utils.Utils.SHA256CheckSum(Application.ExecutablePath)}");
                });

                // 检查是否已经运行
                if (!mutex.WaitOne(0, false))
                {
                    OnlyInstance.Send(OnlyInstance.Commands.Show);

                    // 退出进程
                    Environment.Exit(1);
                }

                Task.Run(OnlyInstance.Server);

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
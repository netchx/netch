using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
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
                Directory.SetCurrentDirectory(Application.StartupPath);

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
                var directories = new[] { "mode", "data", "i18n", "logging" };
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

                // 记录当前系统语言
                Logging.Info($"当前语言：{Global.Settings.Language}");

                // 检查是否已经运行
                if (!mutex.WaitOne(0, false))
                {
                    // 弹出提示
                    MessageBoxX.Show(i18N.Translate("Netch is already running"));

                    // 退出进程
                    Environment.Exit(1);
                }

                var OS = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                var PROC = Environment.Is64BitProcess ? "x64" : "x86";

                // 如果系统位数与程序位数不一致
                if (OS != PROC)
                {

                    // 弹出提示
                    MessageBoxX.Show($"{i18N.Translate("Netch is not compatible with your system.")}\n{i18N.Translate("Current arch of Netch:")} {PROC}\n{i18N.Translate("Current arch of system:")} {OS}");

                    // 退出进程
                    Environment.Exit(1);
                }

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
            if (!e.Exception.ToString().Contains("ComboBox"))
            {
                MessageBox.Show(e.Exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //Application.Exit();
        }
    }
}

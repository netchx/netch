using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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
                    DirectoryInfo directory = new DirectoryInfo("logging");

                    foreach (FileInfo file in directory.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (DirectoryInfo dir in directory.GetDirectories())
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

                // 得到当前线程语言代码
                var culture = CultureInfo.CurrentCulture.Name;

                // 如果命令行参数只有一个，且传入有效语言代码，那么覆盖掉已得到的语言代码
                if (args.Length == 1)
                {
                    try
                    {
                        culture = CultureInfo.GetCultureInfo(args[0]).Name;
                    }
                    catch (CultureNotFoundException)
                    {
                        // 跳过
                    }
                }

                // 记录当前系统语言
                Utils.Logging.Info($"当前系统语言：{culture}");

                // 尝试加载内置中文语言
                if (culture == "zh-CN")
                {
                    // 加载语言
                    Utils.i18N.Load(Encoding.UTF8.GetString(Properties.Resources.zh_CN));
                }

                // 记录当前程序语言
                Utils.Logging.Info($"当前程序语言：{culture}");

                // 从外置文件中加载语言
                if (File.Exists($"i18n\\{culture}"))
                {
                    // 加载语言
                    Utils.i18N.Load(File.ReadAllText($"i18n\\{culture}"));
                }

                // 检查是否已经运行
                if (!mutex.WaitOne(0, false))
                {
                    // 弹出提示
                    MessageBox.Show(Utils.i18N.Translate("Netch is already running"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 退出进程
                    Environment.Exit(1);
                }

                var OS = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                var PROC = Environment.Is64BitProcess ? "x64" : "x86";

                // 如果系统位数与程序位数不一致
                if (OS != PROC)
                {

                    // 弹出提示
                    MessageBox.Show($"{Utils.i18N.Translate("Netch is not compatible with your system.")}\n{Utils.i18N.Translate("Current arch of Netch:")} {PROC}\n{Utils.i18N.Translate("Current arch of system:")} {OS}", Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 退出进程
                    Environment.Exit(1);
                }

                // 绑定错误捕获
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_OnException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(Global.MainForm = new Forms.MainForm());
            }
        }

        public static void Application_OnException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Netch
{
    public static class Netch
    {
        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        [STAThread]
        public static void Main()
        {
            // 设置当前目录
            Directory.SetCurrentDirectory(Application.StartupPath);

            // 检查日志目录
            if (!Directory.Exists("logging"))
            {
                Directory.CreateDirectory("logging");
            }
            else
            {
                // 清理上一次的日志文件，防止淤积占用磁盘空间
                if (File.Exists("logging\\application.log"))
                {
                    File.Delete("logging\\application.log");
                }
            }

            // 检查模式目录
            if (!Directory.Exists("mode"))
            {
                Directory.CreateDirectory("mode");
            }

            Utils.Logging.Info($"当前语言：{CultureInfo.InstalledUICulture.Name}");
            if (CultureInfo.InstalledUICulture.Name == "zh-CN")
            {
                Utils.i18N.Load(Encoding.UTF8.GetString(Properties.Resources.zh_CN));
            }

            if (Directory.Exists("i18n"))
            {
                if (File.Exists($"i18n\\{CultureInfo.InstalledUICulture.Name}"))
                {
                    Utils.i18N.Load(File.ReadAllText($"i18n\\{CultureInfo.InstalledUICulture.Name}"));
                }
            }
            else
            {
                Directory.CreateDirectory("i18n");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(Global.MainForm = new Forms.MainForm());
        }
    }
}

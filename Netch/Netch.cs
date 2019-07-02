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
        public static void Main(string[] args)
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
            if (!Directory.Exists(Global.MODE_DIR))
            {
                Directory.CreateDirectory(Global.MODE_DIR);
            }

            // 得到当前线程语言代码
            var CultureCodeName = CultureInfo.CurrentCulture.Name;

            // 如果命令行参数只有一个，且传入有效语言代码，那么覆盖掉已得到的语言代码
            if (args.Length == 1)
            {
                try
                {
                    CultureCodeName = CultureInfo.GetCultureInfo(args[0]).Name;
                }
                catch (CultureNotFoundException)
                {
                    // 跳过
                }
            }

            // 加载内置资源中的语言
            if (CultureCodeName == "zh-CN")
            {
                Utils.i18N.Load(Encoding.UTF8.GetString(Properties.Resources.zh_CN));
                // 记录日志
                Utils.Logging.Info($"当前语言：{CultureCodeName}");
            }
            else if (Directory.Exists("i18n")) // 如果当前语言不是内置资源中的语言，将符合当前语言的外部文件加载进来作为翻译
            {
                // 如果符合条件的语言文件存在，进行加载
                if (File.Exists($"i18n\\{CultureCodeName}"))
                {
                    Utils.i18N.Load(File.ReadAllText($"i18n\\{CultureCodeName}"));
                    // 记录日志
                    Utils.Logging.Info($"当前语言：{CultureCodeName}");
                }
                // 如果符合条件的语言文件不存在，使用默认语言en-US
                Utils.Logging.Info($"当前语言：en-US");
            }
            else // 如果外部文件均不存在，只是创建目录
            {
                Directory.CreateDirectory("i18n");
                // 记录日志
                Utils.Logging.Info($"当前语言：en-US");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(Global.MainForm = new Forms.MainForm());
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Netch.Properties;
using Newtonsoft.Json;

namespace Netch.Utils
{
    public static class i18N
    {
        /// <summary>
        ///     数据
        /// </summary>
        public static Hashtable Data = new Hashtable();

        public static string LangCode { get; private set; }

        /// <summary>
        ///     加载
        /// </summary>
        /// <param name="langCode">语言代码</param>
        public static void Load(string langCode)
        {
            LangCode = langCode;

            var text = "";
            if (langCode.Equals("System"))
            {
                // 加载系统语言
                langCode = CultureInfo.CurrentCulture.Name;
            }

            if (langCode == "zh-CN")
            {
                // 尝试加载内置中文语言
                text = Encoding.UTF8.GetString(Resources.zh_CN);
            }
            else if (langCode.Equals("en-US"))
            {
                // 清除得到英文
                Data.Clear();
                return;
            }
            else if (File.Exists($"i18n\\{langCode}"))
            {
                // 从外置文件中加载语言
                text = File.ReadAllText($"i18n\\{langCode}");
            }
            else
            {
                Logging.Error($"无法找到语言 {langCode}, 使用系统语言");
                // 加载系统语言
                LangCode = CultureInfo.CurrentCulture.Name;
            }

            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);

            if (data == null) return;

            Data = new Hashtable();
            foreach (var v in data)
            {
                Data.Add(v.Key, v.Value);
            }
        }

        /// <summary>
        ///     翻译
        /// </summary>
        /// <param name="text">需要翻译的文本</param>
        /// <returns>翻译完毕的文本</returns>
        public static string Translate(params object[] text)
        {
            var a = new StringBuilder();
            foreach (var t in text)
                if (t is string)
                    a.Append(Data.Contains(t) ? Data[t].ToString() : t);
                else
                    a.Append(t);
            return a.ToString();
        }

        public static string TranslateFormat(string format, params object[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] is string)
                {
                    args[i] = Translate((string) args[i]);
                }
            }

            return string.Format(Translate(format), args);
        }

        public static List<string> GetTranslateList()
        {
            var translateFile = new List<string> {"System", "zh-CN", "en-US"};

            if (!Directory.Exists("i18n")) return translateFile;
            translateFile.AddRange(Directory.GetFiles("i18n", "*").Select(fileName => fileName.Substring(5)));
            return translateFile;
        }

        public static void TranslateForm(in Control c)
        {
            Utils.ComponentIterator(c, component =>
            {
                switch (component)
                {
                    case TextBoxBase _:
                    case ListControl _:
                        break;
                    case Control c:
                        c.Text = Translate(c.Text);
                        break;
                    case ToolStripItem c:
                        c.Text = Translate(c.Text);
                        break;
                    case ColumnHeader c:
                        c.Text = Translate(c.Text);
                        break;
                }
            });
        }
    }
}
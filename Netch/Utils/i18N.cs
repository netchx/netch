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
#if NET
        static i18N()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
#endif

        /// <summary>
        ///     数据
        /// </summary>
        public static Hashtable Data = new();

        public static string LangCode { get; private set; }

        /// <summary>
        ///     加载
        /// </summary>
        /// <param name="value">语言代码</param>
        public static void Load(string value)
        {
            string text;
            var languages = GetTranslateList().Skip(1).ToList();

            LangCode = value.Equals("System") ? CultureInfo.CurrentCulture.Name : value;

            if (!languages.Contains(LangCode))
            {
                var oldLangCode = LangCode;
                LangCode = languages.FirstOrDefault(s => GetLanguage(s).Equals(GetLanguage(LangCode))) ?? "en-US";
                Logging.Info($"找不到语言 {oldLangCode}, 使用 {LangCode}");
            }

            switch (LangCode)
            {
                case "en-US":
                    Data.Clear();
                    return;
                case "zh-CN":
                    text = Encoding.UTF8.GetString(Resources.zh_CN);
                    break;
                default:
                    text = File.ReadAllText($"i18n\\{LangCode}");
                    break;
            }

            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);

            if (dictionary == null)
                return;

            Data = new Hashtable();
            foreach (var v in dictionary)
                Data.Add(v.Key, v.Value);
        }

        private static string GetLanguage(string culture)
        {
            if (!culture.Contains('-'))
                return "";

            return culture.Substring(0, culture.IndexOf('-'));
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
                if (args[i] is string)
                    args[i] = Translate((string) args[i]);

            return string.Format(Translate(format), args);
        }

        public static List<string> GetTranslateList()
        {
            var translateFile = new List<string> {"System", "zh-CN", "en-US"};

            if (!Directory.Exists("i18n"))
                return translateFile;

            translateFile.AddRange(Directory.GetFiles("i18n", "*").Select(fileName => fileName.Substring(5)));
            return translateFile;
        }

        public static void TranslateForm(in Control c)
        {
            Utils.ComponentIterator(c,
                component =>
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
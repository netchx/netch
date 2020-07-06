using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        public static string Translate(string text)
        {
            return Data.Contains(text) ? Data[text].ToString() : text;
        }
        public static string Translate(params string[] text)
        {
            var a = new StringBuilder();
            foreach (var t in text)
                a.Append(Data.Contains(t) ? Data[t].ToString() : t);
            return a.ToString();
        }
        
        public static List<string> GetTranslateList()
        {
            var translateFile = new List<string> {"System", "zh-CN", "en-US"};

            if (!Directory.Exists("i18n")) return translateFile;
            translateFile.AddRange(Directory.GetFiles("i18n", "*").Select(fileName => fileName.Substring(5)));
            return translateFile;
        }
    }
}
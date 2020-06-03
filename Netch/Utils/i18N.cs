using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Netch.Utils
{
    public static class i18N
    {
        /// <summary>
        ///     数据
        /// </summary>
        public static Hashtable Data = new Hashtable();

        /// <summary>
        ///     加载
        /// </summary>
        /// <param name="text">语言文件</param>
        public static void Load(string text)
        {
            if (text.Equals("en-US"))
            {
                Data.Clear();
                return;
            }
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(text);

            if (data != null)
            {
                Data = new Hashtable();
                foreach (var v in data)
                {
                    Data.Add(v.Key, v.Value);
                }
            }
        }

        /// <summary>
        ///     翻译
        /// </summary>
        /// <param name="text">需要翻译的文本</param>
        /// <returns>翻译完毕的文本</returns>
        public static string Translate(string text)
        {
            if (Data.Contains(text))
            {
                return Data[text].ToString();
            }

            return text;
        }

        /// <summary>
        ///     获取可使用的语言
        /// </summary>
        /// <returns></returns>
        public static List<string> GetTranslateList()
        {
            List<string> TranslateFile = new List<string>();
            TranslateFile.Add("System");
            TranslateFile.Add("zh-CN");
            TranslateFile.Add("en-US");

            if (Directory.Exists("i18n"))
            {
                foreach (var fileName in Directory.GetFiles("i18n", "*"))
                {
                    TranslateFile.Add(fileName.Substring(5));
                }
            }
            return TranslateFile;
        }
    }
}

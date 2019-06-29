using System.Collections;
using System.Collections.Generic;

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
    }
}

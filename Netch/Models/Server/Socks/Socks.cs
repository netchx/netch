using System;
using System.Collections.Generic;

namespace Netch.Models.Server.Socks
{
    public class Socks : Server
    {
        public Socks()
        {
            this.Type = ServerType.Socks;
        }

        /// <summary>
        ///     账号
        /// </summary>
        [Newtonsoft.Json.JsonProperty("username")]
        public string Username;

        /// <summary>
        ///     密码
        /// </summary>
        [Newtonsoft.Json.JsonProperty("password")]
        public string Password;

        /// <summary>
        ///     解析链接
        /// </summary>
        /// <param name="link">链接</param>
        /// <returns>是否成功</returns>
        public bool ParseLink(string link)
        {
            var list = link
                .Replace("tg://socks?", "")
                .Replace("https://t.me/socks?", "")
                .Split('&');

            var dict = new Dictionary<string, string>();
            for (int i = 0; i < list.Length; i++)
            {
                var s = list[i].Split('=');
                if (s.Length != 2)
                {
                    continue;
                }

                dict[s[0]] = s[1];
            }

            if (!dict.ContainsKey("server") || !dict.ContainsKey("port") || !ushort.TryParse(dict["port"], out _))
            {
                return false;
            }

            this.Host = dict["server"];
            this.Port = ushort.Parse(dict["port"]);

            if (dict.ContainsKey("user") && !String.IsNullOrEmpty(dict["user"]))
            {
                this.Username = dict["user"];
            }

            if (dict.ContainsKey("pass") && !String.IsNullOrEmpty(dict["pass"]))
            {
                this.Username = dict["pass"];
            }

            return true;
        }
    }
}

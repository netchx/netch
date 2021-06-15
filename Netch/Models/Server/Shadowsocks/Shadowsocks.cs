using System;
using System.Text.RegularExpressions;
using System.Web;

namespace Netch.Models.Server.Shadowsocks
{
    public class Shadowsocks : Server
    {
        public Shadowsocks()
        {
            this.Type = ServerType.Shadowsocks;
        }

        /// <summary>
        ///     密码
        /// </summary>
        [Newtonsoft.Json.JsonProperty("passwd")]
        public string Passwd;

        /// <summary>
        ///     加密
        /// </summary>
        [Newtonsoft.Json.JsonProperty("method")]
        public string Method;

        /// <summary>
        ///     插件
        /// </summary>
        [Newtonsoft.Json.JsonProperty("obfs")]
        public string OBFS;

        /// <summary>
        ///     插件参数
        /// </summary>
        [Newtonsoft.Json.JsonProperty("obfsparam")]
        public string OBFSParam;

        /// <summary>
        ///     解析链接
        /// </summary>
        /// <param name="link">链接</param>
        /// <returns>是否成功</returns>
        public bool ParseLink(string link)
        {
            if (link.Contains("#"))
            {
                this.Remark = HttpUtility.UrlDecode(link.Split('#')[1]);

                link = link.Split('#')[0];
            }

            if (link.Contains("?"))
            {
                var finder = new Regex(@"^(?<data>.+?)\?(.+)$");

                var matches = finder.Match(link);
                if (matches.Success)
                {
                    var plugin = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(link).Query).Get("plugin"));
                    if (plugin != null)
                    {
                        var obfs = plugin.Substring(0, plugin.IndexOf(";"));
                        var opts = plugin.Substring(plugin.IndexOf(";") + 1);
                        switch (obfs)
                        {
                            case "obfs-local":
                            case "simple-obfs":
                            case "simple-obfs-tls":
                                obfs = "simple-obfs";
                                break;
                        }

                        this.OBFS = obfs;
                        this.OBFSParam = opts;
                    }

                    link = matches.Groups["data"].Value;
                }
                else
                {
                    return false;
                }
            }

            if (link.Contains("@"))
            {
                var finder = new Regex(@"^ss://(?<base64>.+?)@(?<server>.+):(?<port>\d+)");
                var parser = new Regex(@"^(?<method>.+?):(?<password>.+)$");

                var matches = finder.Match(link);
                if (!matches.Success)
                {
                    return false;
                }

                this.Host = matches.Groups["server"].Value;

                if (ushort.TryParse(matches.Groups["port"].Value, out var result))
                {
                    this.Port = result;
                }
                else
                {
                    return false;
                }

                matches = parser.Match(Utils.Base64.Decode.URLSafe(matches.Groups["base64"].Value));
                if (!matches.Success)
                {
                    return false;
                }

                this.Passwd = matches.Groups["password"].Value;
                this.Method = matches.Groups["method"].Value;
            }
            else
            {
                return false;
            }

            this.Method = this.Method.ToLower();
            return Global.Methods.Contains(this.Method);
        }
    }
}

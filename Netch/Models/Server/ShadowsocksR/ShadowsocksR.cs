using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Netch.Models.Server.ShadowsocksR
{
    public class ShadowsocksR : Server
    {
        public ShadowsocksR()
        {
            this.Type = ServerType.ShadowsocksR;
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
        ///     协议
        /// </summary>
        [Newtonsoft.Json.JsonProperty("prot")]
        public string Prot;

        /// <summary>
        ///     协议参数
        /// </summary>
        [Newtonsoft.Json.JsonProperty("protparam")]
        public string ProtParam;

        /// <summary>
        ///     混淆
        /// </summary>
        [Newtonsoft.Json.JsonProperty("obfs")]
        public string OBFS;

        /// <summary>
        ///     混淆参数
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
            try
            {
                var ssr = new Regex(@"ssr://([A-Za-z0-9+/=_-]+)", RegexOptions.IgnoreCase).Match(link);
                if (!ssr.Success)
                {
                    return false;
                }

                var data = Utils.Base64.Decode.URLSafe(ssr.Groups[1].Value);
                var dict = new Dictionary<string, string>();

                var offset = data.IndexOf(@"?", StringComparison.Ordinal);
                if (offset > 0)
                {
                    dict = ParseParam(data.Substring(offset + 1));
                    data = data.Substring(0, offset);
                }

                if (data.IndexOf("/", StringComparison.Ordinal) >= 0)
                {
                    data = data.Substring(0, data.LastIndexOf("/", StringComparison.Ordinal));
                }

                var matches = new Regex(@"^(.+):([^:]+):([^:]*):([^:]+):([^:]*):([^:]+)").Match(data);
                if (!matches.Success)
                {
                    return false;
                }

                if (dict.ContainsKey("remarks"))
                {
                    this.Remark = Utils.Base64.Decode.URLSafe(dict["remarks"]);
                }

                this.Host = matches.Groups[1].Value;
                if (!ushort.TryParse(matches.Groups[2].Value, out this.Port))
                {
                    return false;
                }
                this.Passwd = Utils.Base64.Decode.URLSafe(matches.Groups[6].Value);
                this.Method = matches.Groups[4].Value.ToLower();

                this.Prot = (matches.Groups[3].Value.Length == 0 ? "origin" : matches.Groups[3].Value).Replace("_compatible", String.Empty).ToLower();
                if (dict.ContainsKey("protoparam"))
                {
                    this.ProtParam = Utils.Base64.Decode.URLSafe(dict["protoparam"]);
                }

                this.OBFS = (matches.Groups[5].Value.Length == 0 ? @"plain" : matches.Groups[5].Value).Replace("_compatible", String.Empty).ToLower();
                if (dict.ContainsKey("obfsparam"))
                {
                    this.OBFSParam = Utils.Base64.Decode.URLSafe(dict["obfsparam"]);
                }
            }
            catch (Exception e)
            {
                global::Netch.Global.Logger.Warning(e.ToString());

                return false;
            }

            return true;
        }

        private static Dictionary<string, string> ParseParam(string str)
        {
            var dict = new Dictionary<string, string>();
            var obfs = str.Split('&');
            for (int i = 0; i < str.Length; i++)
            {
                if (obfs[i].IndexOf('=') > 0)
                {
                    var index = obfs[i].IndexOf('=');

                    var k = obfs[i].Substring(0, index);
                    var v = obfs[i].Substring(index + 1);
                    dict[k] = v;
                }
            }

            return dict;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Netch.ServerEx.Shadowsocks;
using Netch.ServerEx.Shadowsocks.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server = Netch.Models.Server;

namespace Netch.Utils
{
    public static class ShareLink
    {
        #region Utils

        /// <summary>
        ///		URL 传输安全的 Base64 解码
        /// </summary>
        /// <param name="text">需要解码的字符串</param>
        /// <returns>解码后的字符串</returns>
        public static string URLSafeBase64Decode(string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text.Replace("-", "+").Replace("_", "/").PadRight(text.Length + (4 - text.Length % 4) % 4, '=')));
        }

        /// <summary>
        /// URL 传输安全的 Base64 加密
        /// </summary>
        /// <param name="text">需要加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string URLSafeBase64Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private static string RemoveEmoji(string text)
        {
            byte[] emojiBytes = {240, 159};
            var remark = Encoding.UTF8.GetBytes(text);
            var startIndex = 0;
            while (remark.Length > startIndex + 1 && remark[startIndex] == emojiBytes[0] && remark[startIndex + 1] == emojiBytes[1])
                startIndex += 4;
            return Encoding.UTF8.GetString(remark.Skip(startIndex).ToArray()).Trim();
        }


        public static string UnBase64String(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string ToBase64String(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public static Dictionary<string, string> ParseParam(string paramStr)
        {
            var paramsDict = new Dictionary<string, string>();
            var obfsParams = paramStr.Split('&');
            foreach (var p in obfsParams)
            {
                if (p.IndexOf('=') > 0)
                {
                    var index = p.IndexOf('=');
                    var key = p.Substring(0, index);
                    var val = p.Substring(index + 1);
                    paramsDict[key] = val;
                }
            }

            return paramsDict;
        }

        public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = true)
        {
            using var sr = new StringReader(str);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                yield return line;
            }
        }

        #endregion

        /// <summary>
        ///		根据服务器生成分享链接
        /// </summary>
        /// <param name="server">需要获取分享链接的服务器</param>
        /// <returns>解码后的字符串</returns>
        public static string GetShareLink(Server server)
        {
            return Servers.GetUtilByTypeOrFullName(server.Type).GetShareLink(server);
        }

        public static List<Server> ParseText(string text)
        {
            try
            {
                text = URLSafeBase64Decode(text);
            }
            catch
            {
                // ignored
            }

            var list = new List<Server>();
            try
            {
                try
                {
                    list.AddRange(JsonConvert.DeserializeObject<List<ShadowsocksConfig>>(text).Select(server => new Shadowsocks
                    {
                        Hostname = server.server,
                        Port = server.server_port,
                        EncryptMethod = server.method,
                        Password = server.password,
                        Remark = server.remarks,
                        Plugin = server.plugin,
                        PluginOption = server.plugin_opts
                    }));
                }
                catch (JsonReaderException)
                {
                    foreach (var line in text.GetLines())
                    {
                        var servers = ParseUri(line);
                        if (servers != null)
                        {
                            list.AddRange(servers);
                        }
                    }
                }

                if (list.Count == 0)
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                return null;
            }

            return list;
        }

        private static IEnumerable<Server> ParseUri(string text)
        {
            var list = new List<Server>();

            try
            {
                if (text.StartsWith("tg://socks?") || text.StartsWith("https://t.me/socks?"))
                {
                    list.AddRange(Servers.GetUtilByTypeOrFullName("Socks5").ParseUri(text));
                }
                else if (text.StartsWith("Netch://"))
                {
                    list.Add(ParseNetchUri(text));
                }
                else
                {
                    var scheme = GetUriScheme(text);
                    var util = Servers.GetUtilByUriScheme(scheme);
                    if (util == null)
                    {
                        Logging.Warning($"无法处理 {scheme} 协议订阅链接");
                        return null;
                    }

                    list.AddRange(util.ParseUri(text));
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                return null;
            }

            foreach (var node in list)
            {
                node.Remark = RemoveEmoji(node.Remark);
            }

            return list.Where(s => s != null);
        }

        private static string GetUriScheme(string text)
        {
            var endIndex = text.IndexOf("://", StringComparison.Ordinal);
            if (endIndex == -1)
                throw new UriFormatException("Text is not a URI");
            return text.Substring(0, endIndex);
        }

        private static Server ParseNetchUri(string text)
        {
            text = text.Substring(8);
            var NetchLink = (JObject) JsonConvert.DeserializeObject(URLSafeBase64Decode(text));
            if (NetchLink == null || !string.IsNullOrEmpty((string) NetchLink["Hostname"]) || !ushort.TryParse((string) NetchLink["Port"], out _))
            {
                return null;
            }

            var type = (string) NetchLink["Type"];
            var s = Servers.GetUtilByTypeOrFullName(type).ParseUri(text).First();
            return Servers.GetUtilByTypeOrFullName(s.Type).CheckServer(s) ? s : null;
        }
    }
}
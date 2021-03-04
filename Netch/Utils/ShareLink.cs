using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Netch.Models;
using Netch.Servers.Shadowsocks;
using Netch.Servers.Shadowsocks.Models;

namespace Netch.Utils
{
    public static class ShareLink
    {
        public static string GetShareLink(Server server)
        {
            return ServerHelper.GetUtilByTypeName(server.Type).GetShareLink(server);
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
                list.AddRange(JsonSerializer.Deserialize<List<ShadowsocksConfig>>(text)
                    .Select(server => new Shadowsocks
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
            catch (JsonException)
            {
                var errorFlag = false;
                foreach (var line in text.GetLines())
                {
                    try
                    {
                        list.AddRange(ParseUri(line));
                    }
                    catch (Exception e)
                    {
                        errorFlag = true;
                        Logging.Error(e.ToString());
                    }
                }

                if (errorFlag)
                    Utils.Open(Logging.LogFile);
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }

            return list;
        }

        private static IEnumerable<Server> ParseUri(in string text)
        {
            var list = new List<Server>();

            if (text.StartsWith("tg://socks?") || text.StartsWith("https://t.me/socks?"))
            {
                list.AddRange(ServerHelper.GetUtilByTypeName("Socks5").ParseUri(text));
            }
            else if (text.StartsWith("Netch://"))
            {
                list.Add(ParseNetchUri(text));
            }
            else
            {
                var scheme = GetUriScheme(text);
                var util = ServerHelper.GetUtilByUriScheme(scheme);
                if (util != null)
                    list.AddRange(util.ParseUri(text));
                else
                    Logging.Warning($"无法处理 {scheme} 协议订阅链接");
            }

            foreach (var node in list.Where(node => !node.Remark.IsNullOrWhiteSpace()))
                node.Remark = RemoveEmoji(node.Remark);

            return list;
        }

        public static string GetUriScheme(string text)
        {
            var endIndex = text.IndexOf("://", StringComparison.Ordinal);
            if (endIndex == -1)
                throw new UriFormatException("Text is not a URI");

            return text.Substring(0, endIndex);
        }

        private static Server ParseNetchUri(string text)
        {
            text = URLSafeBase64Decode(text.Substring(8));

            var NetchLink = JsonSerializer.Deserialize<JsonElement>(text);

            if (string.IsNullOrEmpty(NetchLink.GetProperty("Hostname").GetString()))
                throw new FormatException();

            if (!ushort.TryParse(NetchLink.GetProperty("Port").GetString(), out _))
                throw new FormatException();

            return JsonSerializer.Deserialize<Server>(text,
                new JsonSerializerOptions
                {
                    Converters = {new ServerConverterWithTypeDiscriminator()}
                })!;
        }

        public static string GetNetchLink(Server s)
        {
            var jsonSerializerOptions = Global.NewDefaultJsonSerializerOptions;
            jsonSerializerOptions.WriteIndented = false;
            return "Netch://" + URLSafeBase64Encode(JsonSerializer.Serialize(s, jsonSerializerOptions));
        }

        #region Utils

        /// <summary>
        ///     URL 传输安全的 Base64 解码
        /// </summary>
        /// <param name="text">需要解码的字符串</param>
        /// <returns>解码后的字符串</returns>
        public static string URLSafeBase64Decode(string text)
        {
            return Encoding.UTF8.GetString(
                Convert.FromBase64String(text.Replace("-", "+").Replace("_", "/").PadRight(text.Length + (4 - text.Length % 4) % 4, '=')));
        }

        /// <summary>
        ///     URL 传输安全的 Base64 加密
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
                return "";

            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string ToBase64String(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public static Dictionary<string, string> ParseParam(string paramStr)
        {
            var paramsDict = new Dictionary<string, string>();
            var obfsParams = paramStr.Split('&');
            foreach (var p in obfsParams)
                if (p.IndexOf('=') > 0)
                {
                    var index = p.IndexOf('=');
                    var key = p.Substring(0, index);
                    var val = p.Substring(index + 1);
                    paramsDict[key] = val;
                }

            return paramsDict;
        }

        public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = true)
        {
            using var sr = new StringReader(str);
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
                    continue;

                yield return line;
            }
        }

        #endregion
    }
}
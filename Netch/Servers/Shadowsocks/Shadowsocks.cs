using Netch.Models;
using System.Collections.Generic;

namespace Netch.Servers.Shadowsocks
{
    public class Shadowsocks : Server
    {
        public override string Type { get; } = "SS";

        /// <summary>
        ///     加密方式
        /// </summary>
        public string EncryptMethod { get; set; } = SSGlobal.EncryptMethods[0];

        /// <summary>
        ///     密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        ///     插件
        /// </summary>
        public string? Plugin { get; set; }

        /// <summary>
        ///     插件参数
        /// </summary>
        public string? PluginOption { get; set; }

        public bool HasPlugin()
        {
            return !string.IsNullOrWhiteSpace(Plugin) && !string.IsNullOrWhiteSpace(PluginOption);
        }
    }

    public static class SSGlobal
    {
        /// <summary>
        ///     SS 加密列表
        /// </summary>
        public static readonly List<string> EncryptMethods = new()
        {
            "rc4-md5",
            "aes-128-gcm",
            "aes-192-gcm",
            "aes-256-gcm",
            "aes-128-cfb",
            "aes-192-cfb",
            "aes-256-cfb",
            "aes-128-ctr",
            "aes-192-ctr",
            "aes-256-ctr",
            "camellia-128-cfb",
            "camellia-192-cfb",
            "camellia-256-cfb",
            "bf-cfb",
            "chacha20-ietf-poly1305",
            "xchacha20-ietf-poly1305",
            "salsa20",
            "chacha20",
            "chacha20-ietf"
        };
    }
}
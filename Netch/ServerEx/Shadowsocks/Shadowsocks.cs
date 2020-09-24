using System.Collections.Generic;
using Netch.Models;

namespace Netch.ServerEx.Shadowsocks
{
    public class Shadowsocks : Server
    {
        /// <summary>
        ///     加密方式（SS、SSR、VMess）
        /// </summary>
        public string EncryptMethod = SSGlobal.EncryptMethods[0];

        /// <summary>
        ///     密码（Socks5、SS、SSR）
        /// </summary>
        public string Password;

        /// <summary>
        ///     插件（SS）
        /// </summary>
        public string Plugin;

        /// <summary>
        ///     插件参数（SS）
        /// </summary>
        public string PluginOption;

        public Shadowsocks()
        {
            Type = "SS";
        }
    }

    public static class SSGlobal
    {
        /// <summary>
        ///     SS 加密列表
        /// </summary>
        public static List<string> EncryptMethods = new List<string>
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
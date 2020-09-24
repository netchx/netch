using System.Collections.Generic;
using Netch.Models;

namespace Netch.ServerEx.ShadowsocksR
{
    public class ShadowsocksR : Server
    {
        /// <summary>
        ///     加密方式（SS、SSR、VMess）
        /// </summary>
        public string EncryptMethod = SSRGlobal.EncryptMethods[0];

        /// <summary>
        ///     混淆（SSR）
        /// </summary>
        public string OBFS = SSRGlobal.OBFSs[0];

        /// <summary>
        ///     混淆参数（SSR）
        /// </summary>
        public string OBFSParam;

        /// <summary>
        ///     密码（Socks5、SS、SSR）
        /// </summary>
        public string Password;

        /// <summary>
        ///     协议（SSR）
        /// </summary>
        public string Protocol = SSRGlobal.Protocols[0];

        /// <summary>
        ///     协议参数（SSR）
        /// </summary>
        public string ProtocolParam;

        public ShadowsocksR()
        {
            Type = "SSR";
        }
    }

    public class SSRGlobal
    {
        /// <summary>
        ///     SSR 协议列表
        /// </summary>
        public static List<string> Protocols = new List<string>
        {
            "origin",
            "verify_deflate",
            "auth_sha1_v4",
            "auth_aes128_md5",
            "auth_aes128_sha1",
            "auth_chain_a"
        };

        /// <summary>
        ///     SSR 混淆列表
        /// </summary>
        public static List<string> OBFSs = new List<string>
        {
            "plain",
            "http_simple",
            "http_post",
            "tls1.2_ticket_auth"
        };

        /// <summary>
        ///     SS/SSR 加密方式
        /// </summary>
        public static List<string> EncryptMethods = new List<string>
        {
            "none",
            "table",
            "rc4",
            "rc4-md5",
            "rc4-md5-6",
            "aes-128-cfb",
            "aes-192-cfb",
            "aes-256-cfb",
            "aes-128-ctr",
            "aes-192-ctr",
            "aes-256-ctr",
            "bf-cfb",
            "camellia-128-cfb",
            "camellia-192-cfb",
            "camellia-256-cfb",
            "cast5-cfb",
            "des-cfb",
            "idea-cfb",
            "rc2-cfb",
            "seed-cfb",
            "salsa20",
            "chacha20",
            "chacha20-ietf"
        };
    }
}
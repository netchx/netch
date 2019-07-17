using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace Netch
{
    public static class Global
    {
        /// <summary>
        ///     主窗体
        /// </summary>
        public static Forms.MainForm MainForm;

        public static Forms.SettingForm SettingForm;

        /// <summary>
        ///     杂项配置
        /// </summary>
        public static Dictionary<string, int> Settings = new Dictionary<string, int>();

        /// <summary>
        ///     服务器列表
        /// </summary>
        public static List<Objects.Server> Server = new List<Objects.Server>();

        /// <summary>
        ///     订阅链接列表
        /// </summary>
        public static List<Objects.SubscribeLink> SubscribeLink = new List<Objects.SubscribeLink>();

        /// <summary>
		///		SS/SSR 加密方式
		/// </summary>
		public static class EncryptMethods
        {
            /// <summary>
            ///		SS 加密列表
            /// </summary>
            public static List<string> SS = new List<string>()
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

            /// <summary>
            ///		SSR 加密列表
            /// </summary>
            public static List<string> SSR = new List<string>()
            {
                "none",
                "table",
                "rc4",
                "rc4-md5",
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
            /// <summary>
            ///		VMess 解密列表
            /// </summary>
            public static List<string> VMess = new List<string>()
            {
                "none",
                "auto",
                "aes-128-gcm",
                "chacha20-poly1305"
            };

            /// <summary>
            ///		VMess QUIC 加密列表
            /// </summary>
            public static List<string> VMessQUIC = new List<string>()
            {
                "none",
                "aes-128-gcm",
                "chacha20-poly1305"
            };
        }

        /// <summary>
		///		SSR 协议列表
		/// </summary>
		public static List<string> Protocols = new List<string>()
        {
            "origin",
            "verify_deflate",
            "auth_sha1_v4",
            "auth_aes128_md5",
            "auth_aes128_sha1",
            "auth_chain_a"
        };

        /// <summary>
        ///		SSR 混淆列表
        /// </summary>
        public static List<string> OBFSs = new List<string>()
        {
            "plain",
            "http_simple",
            "http_post",
            "tls1.2_ticket_auth"
        };

        /// <summary>
        ///		V2Ray 传输协议
        /// </summary>
        public static List<string> TransferProtocols = new List<string>()
        {
            "tcp",
            "kcp",
            "ws",
            "h2",
            "quic"
        };

        /// <summary>
        ///		V2Ray 伪装类型
        /// </summary>
        public static List<string> FakeTypes = new List<string>()
        {
            "none",
            "http",
            "srtp",
            "utp",
            "wechat-video",
            "dtls",
            "wireguard"
        };

        /// <summary>
        ///		适配器
        /// </summary>
        public static class Adapter
        {
            /// <summary>
            ///		索引
            /// </summary>
            public static int Index = -1;

            /// <summary>
            ///		地址
            /// </summary>
            public static IPAddress Address;

            /// <summary>
            ///		网关
            /// </summary>
            public static IPAddress Gateway;
        }

        /// <summary>
        ///		TUN/TAP 适配器
        /// </summary>
        public static class TUNTAP
        {
            /// <summary>
            ///		适配器
            /// </summary>
            public static NetworkInterface Adapter;

            /// <summary>
            ///		索引
            /// </summary>
            public static int Index = -1;

            /// <summary>
            ///		组件 ID
            /// </summary>
            public static string ComponentID = String.Empty;

            /// <summary>
            ///		地址
            /// </summary>
            public static IPAddress Address = IPAddress.Parse("10.0.236.10");

            /// <summary>
            ///		掩码
            /// </summary>
            public static IPAddress Netmask = IPAddress.Parse("255.255.255.0");

            /// <summary>
            ///		网关
            /// </summary>
            public static IPAddress Gateway = IPAddress.Parse("10.0.236.1");

            /// <summary>
            ///		DNS
            /// </summary>
            public static List<IPAddress> DNS = new List<IPAddress>()
            {
                IPAddress.Parse("1.1.1.1")
            };

            /// <summary>
            ///		使用伪装 DNS
            /// </summary>
            public static bool UseFakeDNS = false;

            /// <summary>
            ///		使用自定义 DNS 设置
            /// </summary>
            public static bool UseCustomDNS = false;
        }

        /// <summary>
        ///		全局绕过 IP 列表
        /// </summary>
        public static List<string> BypassIPs = new List<string>();

    }
}

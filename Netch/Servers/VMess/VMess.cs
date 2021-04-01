using Netch.Models;
using System.Collections.Generic;

namespace Netch.Servers.VMess
{
    public class VMess : Server
    {
        private string _tlsSecureType = VMessGlobal.TLSSecure[0];

        public override string Type { get; } = "VMess";

        /// <summary>
        ///     用户 ID
        /// </summary>
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        ///     额外 ID
        /// </summary>
        public int AlterID { get; set; }

        /// <summary>
        ///     加密方式
        /// </summary>
        public virtual string EncryptMethod { get; set; } = VMessGlobal.EncryptMethods[0];

        /// <summary>
        ///     传输协议
        /// </summary>
        public virtual string TransferProtocol { get; set; } = VMessGlobal.TransferProtocols[0];

        /// <summary>
        ///     伪装类型
        /// </summary>
        public virtual string FakeType { get; set; } = VMessGlobal.FakeTypes[0];

        /// <summary>
        ///     伪装域名
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        ///     传输路径
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        ///     QUIC 加密方式
        /// </summary>
        public string? QUICSecure { get; set; } = VMessGlobal.QUIC[0];

        /// <summary>
        ///     QUIC 加密密钥
        /// </summary>
        public string? QUICSecret { get; set; } = string.Empty;

        /// <summary>
        ///     TLS 底层传输安全
        /// </summary>
        public string TLSSecureType
        {
            get => _tlsSecureType;
            set
            {
                if (value == "")
                    value = "none";

                _tlsSecureType = value;
            }
        }

        /// <summary>
        ///     Mux 多路复用
        /// </summary>
        public bool? UseMux { get; set; } = false;
    }

    public class VMessGlobal
    {
        public static readonly List<string> EncryptMethods = new()
        {
            "auto",
            "none",
            "aes-128-gcm",
            "chacha20-poly1305"
        };

        public static readonly List<string> QUIC = new()
        {
            "none",
            "aes-128-gcm",
            "chacha20-poly1305"
        };

        /// <summary>
        ///     V2Ray 传输协议
        /// </summary>
        public static readonly List<string> TransferProtocols = new()
        {
            "tcp",
            "kcp",
            "ws",
            "h2",
            "quic"
        };

        /// <summary>
        ///     V2Ray 伪装类型
        /// </summary>
        public static readonly List<string> FakeTypes = new()
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
        ///     TLS 安全类型
        /// </summary>
        public static readonly List<string> TLSSecure = new()
        {
            "none",
            "tls"
        };
    }
}
using System.Collections.Generic;
using Netch.Models;

namespace Netch.ServerEx.VMess
{
    public class VMess : Server
    {
        public VMess()
        {
            Type = "VMess";
        }

        public string EncryptMethod { get; set; } = VMessGlobal.EncryptMethods[0];


        /// <summary>
        ///		传输协议（VMess）
        /// </summary>
        public string TransferProtocol = VMessGlobal.TransferProtocols[0];

        /// <summary>
        ///		伪装类型（VMess）
        /// </summary>
        public string FakeType = VMessGlobal.FakeTypes[0];

        /// <summary>
        ///     QUIC
        /// </summary>
        public string QUIC { get; set; } = VMessGlobal.QUIC[0];

        public string Host { get; set; }
        public string Path { get; set; }

        /// <summary>
        ///		QUIC 加密方式（VMess）
        /// </summary>
        public string QUICSecure = VMessGlobal.QUIC[0];

        /// <summary>
        ///		QUIC 加密密钥（VMess）
        /// </summary>
        public string QUICSecret = string.Empty;

        /// <summary>
        ///		TLS 底层传输安全（VMess）
        /// </summary>
        public bool TLSSecure = false;

        /// <summary>
        ///		Mux 多路复用（VMess）
        /// </summary>
        public bool UseMux = true;

        public string UserID;
        public int AlterID;
    }

    public class VMessGlobal
    {
        public static List<string> EncryptMethods = new List<string>
        {
            "auto",
            "none",
            "aes-128-gcm",
            "chacha20-poly1305"
        };

        public static List<string> QUIC = new List<string>
        {
            "none",
            "aes-128-gcm",
            "chacha20-poly1305"
        };


        /// <summary>
        ///		V2Ray 传输协议
        /// </summary>
        public static List<string> TransferProtocols = new List<string>
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
        public static List<string> FakeTypes = new List<string>
        {
            "none",
            "http",
            "srtp",
            "utp",
            "wechat-video",
            "dtls",
            "wireguard"
        };
    }
}
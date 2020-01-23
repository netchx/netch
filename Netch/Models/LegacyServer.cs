namespace Netch.Models
{
    public class LegacyServer
    {
        /// <summary>
        ///     备注
        /// </summary>
        public string Remark;

        /// <summary>
        ///     组
        /// </summary>
        public string Group = "None";

        /// <summary>
        ///     类型（Socks5、Shadowsocks、ShadowsocksR、VMess）
        /// </summary>
        public string Type;

        /// <summary>
        ///     地址
        /// </summary>
        public string Address;

        /// <summary>
        ///     端口
        /// </summary>
        public int Port;

        /// <summary>
        ///     用户名
        /// </summary>
        public string Username;

        /// <summary>
        ///     密码
        /// </summary>
        public string Password;

        /// <summary>
        ///		用户 ID（V2）
        /// </summary>
        public string UserID = string.Empty;

        /// <summary>
        ///		额外 ID（V2）
        /// </summary>
        public int AlterID = 0;

        /// <summary>
        ///     加密方式
        /// </summary>
        public string EncryptMethod;

        /// <summary>
        ///     协议
        /// </summary>
        public string Protocol;

        /// <summary>
        ///     协议参数
        /// </summary>
        public string ProtocolParam;

        /// <summary>
        ///     混淆（SSR）/ 插件（SS）
        /// </summary>
        public string OBFS;

        /// <summary>
        ///     混淆参数（SSR）/ 插件参数（SS）
        /// </summary>
        public string OBFSParam;

        /// <summary>
        ///		传输协议（V2）
        /// </summary>
        public string TransferProtocol = "tcp";

        /// <summary>
        ///		伪装类型（V2）
        /// </summary>
        public string FakeType = string.Empty;

        /// <summary>
        ///		伪装域名（V2：HTTP、WebSocket、HTTP/2）
        /// </summary>
        public string Host = string.Empty;

        /// <summary>
        ///		传输路径（V2：WebSocket、HTTP/2）
        /// </summary>
        public string Path = string.Empty;

        /// <summary>
        ///		QUIC 加密方式（V2）
        /// </summary>
        public string QUICSecurity = "none";

        /// <summary>
        ///		QUIC 加密密钥（V2）
        /// </summary>
        public string QUICSecret = string.Empty;

        /// <summary>
        ///		TLS 底层传输安全（V2）
        /// </summary>
        public bool TLSSecure = false;

        /// <summary>
        ///     延迟
        /// </summary>
        public int Delay = -1;
    }
}

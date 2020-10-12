namespace Netch.Servers.VMess.Models
{
    /// <summary>
    ///     使用 v2rayN 定义的 VMess 链接格式
    /// </summary>
    public class V2rayNSharing
    {
        /// <summary>
        ///     链接版本
        /// </summary>
        public string v = string.Empty;

        /// <summary>
        ///     备注
        /// </summary>
        public string ps = string.Empty;

        /// <summary>
        ///     地址
        /// </summary>
        public string add = string.Empty;

        /// <summary>
        ///     端口
        /// </summary>
        public string port = string.Empty;

        /// <summary>
        ///     用户 ID
        /// </summary>
        public string id = string.Empty;

        /// <summary>
        ///     额外 ID
        /// </summary>
        public string aid = string.Empty;

        /// <summary>
        ///     传输协议
        /// </summary>
        public string net = string.Empty;

        /// <summary>
        ///     伪装类型
        /// </summary>
        public string type = string.Empty;

        /// <summary>
        ///     伪装域名（HTTP，WS）
        /// </summary>
        public string host = string.Empty;

        /// <summary>
        ///     伪装路径
        /// </summary>
        public string path = string.Empty;

        /// <summary>
        ///     是否使用 TLS
        /// </summary>
        public string tls = string.Empty;
    }
}
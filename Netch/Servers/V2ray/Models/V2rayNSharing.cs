namespace Netch.Servers.V2ray.Models
{
    /// <summary>
    ///     使用 v2rayN 定义的 VMess 链接格式
    /// </summary>
    public class V2rayNSharing
    {
        /// <summary>
        ///     地址
        /// </summary>
        public string add { get; set; } = string.Empty;

        /// <summary>
        ///     额外 ID
        /// </summary>
        public int aid { get; set; }

        /// <summary>
        ///     伪装域名（HTTP，WS）
        /// </summary>
        public string? host { get; set; } = string.Empty;

        /// <summary>
        ///     用户 ID
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        ///     传输协议
        /// </summary>
        public string net { get; set; } = string.Empty;

        /// <summary>
        ///     伪装路径
        /// </summary>
        public string? path { get; set; } = string.Empty;

        /// <summary>
        ///     端口
        /// </summary>
        public ushort port { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string ps { get; set; } = string.Empty;

        /// <summary>
        ///     是否使用 TLS
        /// </summary>
        public string tls { get; set; } = string.Empty;

        /// <summary>
        ///     伪装类型
        /// </summary>
        public string type { get; set; } = string.Empty;

        /// <summary>
        ///     链接版本
        /// </summary>
        public int v { get; set; } = 2;
    }
}
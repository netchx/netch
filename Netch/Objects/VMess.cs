namespace Netch.Objects
{
    /// <summary>
    ///     使用 v2rayN 定义的 VMess 链接格式
    /// </summary>
    public class VMess
    {
        /// <summary>
        ///     链接版本
        /// </summary>
        public string v;

        /// <summary>
        ///     备注
        /// </summary>
        public string ps;

        /// <summary>
        ///     地址
        /// </summary>
        public string add;

        /// <summary>
        ///     端口
        /// </summary>
        public int port;

        /// <summary>
        ///     用户 ID
        /// </summary>
        public string id;

        /// <summary>
        ///     额外 ID
        /// </summary>
        public int aid = 0;

        /// <summary>
        ///     传输协议
        /// </summary>
        public string net;

        /// <summary>
        ///     伪装类型
        /// </summary>
        public string type;

        /// <summary>
        ///     伪装域名（HTTP，WS）
        /// </summary>
        public string host;

        /// <summary>
        ///     伪装路径
        /// </summary>
        public string path;

        /// <summary>
        ///     是否使用 TLS
        /// </summary>
        public string tls;
    }
}

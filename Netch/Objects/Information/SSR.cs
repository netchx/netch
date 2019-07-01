namespace Netch.Objects.Information
{
    public class SSR
    {
        /// <summary>
        ///     地址
        /// </summary>
        public string server;

        /// <summary>
        ///     端口
        /// </summary>
        public int server_port;

        /// <summary>
        ///     密码
        /// </summary>
        public string password;

        /// <summary>
        ///     加密方式
        /// </summary>
        public string method;

        /// <summary>
        ///     协议
        /// </summary>
        public string protocol;

        /// <summary>
        ///     协议参数
        /// </summary>
        public string protocol_param;

        /// <summary>
        ///     混淆
        /// </summary>
        public string obfs;

        /// <summary>
        ///     混淆参数
        /// </summary>
        public string obfs_param;

        /// <summary>
        ///     本地地址
        /// </summary>
        public string local_addr = "0.0.0.0";

        /// <summary>
        ///     本地端口
        /// </summary>
        public int local_port = 2801;
    }
}

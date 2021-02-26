#nullable disable
namespace Netch.Servers.Shadowsocks.Models.SSD
{
    public class SSDServer
    {
        /// <summary>
        ///     加密方式
        /// </summary>
        public string encryption;

        /// <summary>
        ///     密码
        /// </summary>
        public string password;

        /// <summary>
        ///     插件
        /// </summary>
        public string plugin;

        /// <summary>
        ///     插件参数
        /// </summary>
        public string plugin_options;

        /// <summary>
        ///     端口
        /// </summary>
        public ushort port;

        /// <summary>
        ///     备注
        /// </summary>
        public string remarks;
        /// <summary>
        ///     服务器地址
        /// </summary>
        public string server;
    }
}
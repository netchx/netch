using System.Collections.Generic;

namespace Netch.Models
{
    /// <summary>
    ///     用于读取和写入的配置的类
    /// </summary>
    public class LegacySetting
    {
        /// <summary>
        ///		服务器选择位置
        /// </summary>
        public int ServerComboBoxSelectedIndex = 0;

        /// <summary>
        ///		模式选择位置
        /// </summary>
        public int ModeComboBoxSelectedIndex = 0;

        /// <summary>
        ///		HTTP 本地端口
        /// </summary>
        public int HTTPLocalPort = 2802;

        /// <summary>
        ///		Socks5 本地端口
        /// </summary>
        public int Socks5LocalPort = 2801;

        /// <summary>
        ///		HTTP 和 Socks5 本地代理地址
        /// </summary>
        public string LocalAddress = "127.0.0.1";

        /// <summary>
        ///		Redirector TCP 占用端口
        /// </summary>
        public int RedirectorTCPPort = 2800;

        /// <summary>
        ///		TUNTAP 适配器配置
        /// </summary>
        public TUNTAPConfig TUNTAP = new TUNTAPConfig();

        /// <summary>
        ///     服务器列表
        /// </summary>
        public List<LegacyServer> Server = new List<LegacyServer>();

        /// <summary>
        ///     订阅链接列表
        /// </summary>
        public List<SubscribeLink> SubscribeLink = new List<SubscribeLink>();

        /// <summary>
        ///		全局绕过 IP 列表
        /// </summary>
        public List<string> BypassIPs = new List<string>();
    }
}

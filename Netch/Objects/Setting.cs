using System.Collections.Generic;

namespace Netch.Objects
{
    /// <summary>
    ///		TUN/TAP 适配器配置类
    /// </summary>
    public class TUNTAPConfig
    {
        /// <summary>
        ///		地址
        /// </summary>
        public string Address = "10.0.236.10";

        /// <summary>
        ///		掩码
        /// </summary>
        public string Netmask = "255.255.255.0";

        /// <summary>
        ///		网关
        /// </summary>
        public string Gateway = "10.0.236.1";

        /// <summary>
        ///		DNS
        /// </summary>
        public List<string> DNS = new List<string>()
        {
        };
        /// <summary>
        ///		使用伪装 DNS
        /// </summary>
        public bool UseFakeDNS = false;

        /// <summary>
        ///		使用自定义 DNS 设置
        /// </summary>
        public bool UseCustomDNS = false;
    }

    /// <summary>
    ///     用于读取和写入的配置的类
    /// </summary>
    public class Setting
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

        public TUNTAPConfig TUNTAP = new TUNTAPConfig();

        /// <summary>
        ///     服务器列表
        /// </summary>
        public List<Objects.Server> Server = new List<Objects.Server>();

        /// <summary>
        ///     订阅链接列表
        /// </summary>
        public List<Objects.SubscribeLink> SubscribeLink = new List<Objects.SubscribeLink>();

        /// <summary>
        ///		全局绕过 IP 列表
        /// </summary>
        public List<string> BypassIPs = new List<string>();
    }
}

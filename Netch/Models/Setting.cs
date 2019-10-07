using System.Collections.Generic;

namespace Netch.Models
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
        public List<string> DNS = new List<string>();

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
        ///		是否关闭窗口时退出
        /// </summary>
        public bool ExitWhenClosed = false;

        /// <summary>
        ///		是否退出时停止
        /// </summary>
        public bool StopWhenExited = false;

        /// <summary>
        ///		是否打开软件时启动加速
        /// </summary>
        public bool StartWhenOpened = false;

        /// <summary>
        ///		使用何种模式文件名
        ///		0 为自定义文件名，1 为使用和备注一致的文件名，2 为使用时间数据作为文件名
        /// </summary>
        public int ModeFileNameType = 1;

        /// <summary>
        ///		HTTP 本地端口
        /// </summary>
        public int HTTPLocalPort = 2802;

        /// <summary>
        ///		Socks5 本地端口
        /// </summary>
        public int Socks5LocalPort = 2801;

        /// <summary>
        ///		Redirector TCP 占用端口
        /// </summary>
        public int RedirectorTCPPort = 2800;

        /// <summary>
        ///		HTTP 和 Socks5 本地代理地址
        /// </summary>
        public string LocalAddress = "127.0.0.1";

        /// <summary>
        ///		TUNTAP 适配器配置
        /// </summary>
        public TUNTAPConfig TUNTAP = new TUNTAPConfig();

        /// <summary>
        ///		使用代理更新订阅
        /// </summary>
        public bool UseProxyToUpdateSubscription = false;

        /// <summary>
        ///     订阅链接列表
        /// </summary>
        public List<Models.SubscribeLink> SubscribeLink = new List<Models.SubscribeLink>();

        /// <summary>
        ///     服务器列表
        /// </summary>
        public List<Models.Server> Server = new List<Models.Server>();

        /// <summary>
        ///		全局绕过 IP 列表
        /// </summary>
        public List<string> BypassIPs = new List<string>();
    }
}

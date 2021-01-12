using System.Collections.Generic;

namespace Netch.Models
{
    /// <summary>
    ///     TUN/TAP 适配器配置类
    /// </summary>
    public class TUNTAPConfig
    {
        /// <summary>
        ///     地址
        /// </summary>
        public string Address = "10.0.236.10";

        /// <summary>
        ///     DNS
        /// </summary>
        public List<string> DNS = new();

        /// <summary>
        ///     网关
        /// </summary>
        public string Gateway = "10.0.236.1";

        /// <summary>
        ///     掩码
        /// </summary>
        public string Netmask = "255.255.255.0";

        /// <summary>
        ///     模式 2 下是否代理 DNS
        /// </summary>
        public bool ProxyDNS = false;

        /// <summary>
        ///     使用自定义 DNS 设置
        /// </summary>
        public bool UseCustomDNS = false;

        /// <summary>
        ///     使用Fake DNS
        /// </summary>
        public bool UseFakeDNS = false;
    }

    public class KcpConfig
    {
        public bool congestion = false;

        public int downlinkCapacity = 100;
        public int mtu = 1350;

        public int readBufferSize = 2;

        public int tti = 50;

        public int uplinkCapacity = 12;

        public int writeBufferSize = 2;
    }

    public class V2rayConfig
    {
        public bool AllowInsecure = true;

        public KcpConfig KcpConfig = new();

        public bool UseMux = false;
    }

    public class AioDNSConfig
    {
        public string ChinaDNS = "223.5.5.5";

        public string OtherDNS = "1.1.1.1";

        public string Protocol = "tcp";
        public string RulePath = "bin\\china_site_list";
    }

    /// <summary>
    ///     用于读取和写入的配置的类
    /// </summary>
    public class Setting
    {
        /// <summary>
        ///     服务器列表
        /// </summary>
        public readonly List<Server> Server = new();

        /// <summary>
        ///     ACL规则
        /// </summary>
        public string ACL = "https://raw.githubusercontent.com/ACL4SSR/ACL4SSR/master/banAD.acl";

        public AioDNSConfig AioDNS = new();

        /// <summary>
        ///     是否使用DLL启动Shadowsocks
        /// </summary>
        public bool BootShadowsocksFromDLL = true;

        /// <summary>
        ///     全局绕过 IP 列表
        /// </summary>
        public List<string> BypassIPs = new();

        /// <summary>
        ///     是否检查 Beta 更新
        /// </summary>
        public bool CheckBetaUpdate = false;

        /// <summary>
        ///     是否打开软件时检查更新
        /// </summary>
        public bool CheckUpdateWhenOpened = true;

        /// <summary>
        ///     是否关闭窗口时退出
        /// </summary>
        public bool ExitWhenClosed = false;

        /// <summary>
        ///     HTTP 本地端口
        /// </summary>
        public ushort HTTPLocalPort = 2802;

        /// <summary>
        ///     语言设置
        /// </summary>
        public string Language = "System";

        /// <summary>
        ///     HTTP 和 Socks5 本地代理地址
        /// </summary>
        public string LocalAddress = "127.0.0.1";

        /// <summary>
        ///     是否启动后自动最小化
        /// </summary>
        public bool MinimizeWhenStarted = false;

        /// <summary>
        ///     模式选择位置
        /// </summary>
        public int ModeComboBoxSelectedIndex = 0;

        /// <summary>
        ///     要修改为的系统 DNS
        /// </summary>
        public string ModifiedDNS = "1.1.1.1,8.8.8.8";

        /// <summary>
        ///     修改系统 DNS
        /// </summary>
        public bool ModifySystemDNS = false;

        /// <summary>
        ///     GFWList
        /// </summary>
        public string PAC = "https://raw.githubusercontent.com/HMBSbige/Text_Translation/master/ShadowsocksR/ss_white.pac";

        /// <summary>
        ///     PAC端口
        /// </summary>
        public int Pac_Port = 2803;

        /// <summary>
        ///     PAC URL
        /// </summary>
        public string Pac_Url = "";

        /// <summary>
        ///     不代理TCP
        /// </summary>
        public bool ProcessNoProxyForTcp = false;

        /// <summary>
        ///     不代理UDP
        /// </summary>
        public bool ProcessNoProxyForUdp = false;

        /// <summary>
        ///     快捷配置数量
        /// </summary>
        public int ProfileCount = 4;

        /// <summary>
        ///     已保存的快捷配置
        /// </summary>
        public List<Profile> Profiles = new();

        /// <summary>
        ///     是否使用RDR内置SS
        /// </summary>
        public bool RedirectorSS = false;

        /// <summary>
        ///     Redirector TCP 占用端口
        /// </summary>
        public ushort RedirectorTCPPort = 3901;

        /// <summary>
        ///     网页请求超时 毫秒
        /// </summary>
        public int RequestTimeout = 10000;

        /// <summary>
        ///     解析服务器主机名
        /// </summary>
        public bool ResolveServerHostname = false;

        /// <summary>
        ///     是否开机启动软件
        /// </summary>
        public bool RunAtStartup = false;

        /// <summary>
        ///     服务器选择位置
        /// </summary>
        public int ServerComboBoxSelectedIndex = 0;

        /// <summary>
        ///     服务器测试方式 false.ICMPing true.TCPing
        /// </summary>
        public bool ServerTCPing = true;

        /// <summary>
        ///     Socks5 本地端口
        /// </summary>
        public ushort Socks5LocalPort = 2801;

        /// <summary>
        ///     是否启用启动后延迟测试
        /// </summary>
        public bool StartedTcping = false;

        /// <summary>
        ///     启动后延迟测试间隔/秒
        /// </summary>
        public int StartedTcping_Interval = 3;

        /// <summary>
        ///     是否打开软件时启动加速
        /// </summary>
        public bool StartWhenOpened = false;

        /// <summary>
        ///     是否退出时停止
        /// </summary>
        public bool StopWhenExited = false;

        /// <summary>
        ///     STUN测试服务器
        /// </summary>
        public string STUN_Server = "stun.syncthing.net";

        /// <summary>
        ///     STUN测试服务器
        /// </summary>
        public int STUN_Server_Port = 3478;

        /// <summary>
        ///     订阅链接列表
        /// </summary>
        public List<SubscribeLink> SubscribeLink = new();

        /// <summary>
        ///     TUNTAP 适配器配置
        /// </summary>
        public TUNTAPConfig TUNTAP = new();

        /// <summary>
        ///     UDP Socket 占用端口
        /// </summary>
        public ushort UDPSocketPort = 18291;

        /// <summary>
        ///     是否打开软件时更新订阅
        /// </summary>
        public bool UpdateServersWhenOpened = false;

        /// <summary>
        ///     使用代理更新订阅
        /// </summary>
        public bool UseProxyToUpdateSubscription = false;

        public V2rayConfig V2RayConfig = new();
    }
}
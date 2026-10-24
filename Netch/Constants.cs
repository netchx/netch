using Netch.Enums;

namespace Netch;

public static class Constants
{
    public const string TempConfig = "data\\last.json";
    public const string TempRouteFile = "data\\route.txt";

    public const string AioDnsRuleFile = "bin\\aiodns.conf";
    public const string NFDriver = "bin\\nfdriver.sys";
    public const string STUNServersFile = "bin\\stun.txt";

    public const string LogFile = "logging\\application.log";

    public const string OutputTemplate = @"[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level}] {Message:lj}{NewLine}{Exception}";
    public const string EOF = "\r\n";

    public const string DefaultGroup = "NONE";

    public static class Parameter
    {
        public const string Show = "-show";
        public const string ForceUpdate = "-forceUpdate";
    }

    public const string TUN2SocksFile = "bin\\tun2socks-windows-amd64-v3.exe";
    public const string WintunDllFile = "bin\\wintun.dll";
    public const string DisableModeDirectoryFileName = "disabled";

    public const string DefaultPrimaryDNS = "1.1.1.1";
    public const string DefaultCNPrimaryDNS = "223.5.5.5";




    public const string DefaultSecurity = "auto";
    public const string DefaultNetwork = "tcp";
    public const string TcpHeaderHttp = "http";
    public const string StreamSecurity = "tls";
    public const string StreamSecurityReality = "reality";

    public const int Hysteria2DefaultHopInt = 10;

    public const string GrpcGunMode = "gun";
    public const string GrpcMultiMode = "multi";

    public const string None = "none";

    public const string NamespaceSample = "ServiceLib.Sample.";
    public const string V2raySampleClient = NamespaceSample + "SampleClientConfig";
    public const string V2raySampleHttpRequestFileName = NamespaceSample + "SampleHttpRequest";
    public const string V2raySampleHttpResponseFileName = NamespaceSample + "SampleHttpResponse";
    public const string V2raySampleInbound = NamespaceSample + "SampleInbound";
    public const string V2raySampleOutbound = NamespaceSample + "SampleOutbound";
    public const string CustomRoutingFileName = NamespaceSample + "custom_routing_";
    public const string DNSV2rayNormalFileName = NamespaceSample + "dns_v2ray_normal";
    public const string LinuxAutostartConfig = NamespaceSample + "linux_autostart_config";
    public const string PacFileName = NamespaceSample + "pac";
    public const string ProxySetOSXShellFileName = NamespaceSample + "proxy_set_osx_sh";
    public const string ProxySetLinuxShellFileName = NamespaceSample + "proxy_set_linux_sh";
    public const string KillAsSudoOSXShellFileName = NamespaceSample + "kill_as_sudo_osx_sh";
    public const string KillAsSudoLinuxShellFileName = NamespaceSample + "kill_as_sudo_linux_sh";


    public const string LocalAppData = "V2RAYN_LOCAL_APPLICATION_DATA_V2";
    /// <summary>
    ///     V2Ray 传输协议
    /// </summary>
    public static readonly List<string> Networks =
    [
        "tcp",
        "kcp",
        "ws",
        "httpupgrade",
        "xhttp",
        "h2",
        "quic",
        "grpc",
        "hysteria"
    ];

    /// <summary>
    ///     TLS 安全类型
    /// </summary>
    public static readonly List<string> TLSSecure = new()
    {
        "none",
        "tls",
        "xtls",
        "reality"
    };

    public static readonly List<string> Fingerprints =
    [
        "",
        "chrome",
        "firefox",
        "safari",
        "ios",
        "android",
        "edge",
        "360",
        "qq",
        "random",
        "randomized",
    ];

    public static readonly Dictionary<string, string> UserAgentTexts = new()
    {
        {"none" , "" }, // 返回空字符串
        {"chrome" , "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36" },
        {"firefox" , "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:90.0) Gecko/20100101 Firefox/90.0" },
        {"safari" , "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15" },
        {"ios" , "Mozilla/5.0 (iPhone; CPU iPhone OS 17_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1" },
        {"android" , "Mozilla/5.0 (Linux; Android 14; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36" },
        {"edge" , "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.70" },
        {"360" , "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36" },
        {"qq" , "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36 QQBrowser/11.4.5116.400" }
    };
    public static string GetRandomUserAgent()
    {
        Random random = new();
        return UserAgentTexts.GetValueOrDefault(Fingerprints[random.Next(1, Fingerprints.Count - 2)]);
    }

    public static readonly List<string> Alpn = new()
    {
        "",
        "h3",
        "h2",
        "http/1.1",
        "h3,h2",
        "h2,http/1.1",
        "h3,h2,http/1.1",
    };
    public static readonly List<string> EchForceQuerys = new()
    {
        "",
        "none",
        "half",
        "full",
    };

    /// <summary>
    ///     V2Ray 伪装类型
    /// </summary>
    public static readonly List<string> AllHeaderTypes = new()
    {
        "none",
        "http",
        "srtp",
        "utp",
        "wechat-video",
        "dtls",
        "wireguard",
        "dns",
        "auto",
        "packet-up",
        "stream-up",
        "stream-one",
        "gun",
        "multi"
    };

    public static readonly List<string> SsSecuritiesInXray =
    [
        "aes-256-gcm",
        "aes-128-gcm",
        "chacha20-poly1305",
        "chacha20-ietf-poly1305",
        "xchacha20-poly1305",
        "xchacha20-ietf-poly1305",
        "none",
        "plain",
        "2022-blake3-aes-128-gcm",
        "2022-blake3-aes-256-gcm",
        "2022-blake3-chacha20-poly1305"
    ];

    /// <summary>
    ///     V2Ray 伪装类型
    /// </summary>
    public static readonly List<string> KcpHeaderTypes = new()
    {
        "srtp",
        "utp",
        "wechat-video",
        "dtls",
        "wireguard",
        "dns"
    };

    public static readonly Dictionary<string, string> KcpHeaderMaskMap = new()
    {
        { "srtp", "header-srtp" },
        { "utp", "header-utp" },
        { "wechat-video", "header-wechat" },
        { "dtls", "header-dtls" },
        { "wireguard", "header-wireguard" },
        { "dns", "header-dns" }
    };

    public static readonly List<string> AllowInsecure = new()
    {
        "",
        "true",
        "false",
    };
    public static readonly List<string> VmessSecurities = new()
    {
        "auto",
        "none",
        "aes-128-gcm",
        "chacha20-poly1305",
        "zero"
    };


    /// <summary>
    /// VLESS 流控（Flow）
    /// </summary>
    public static readonly List<string> Flow = new()
    {
        "",
        "xtls-rprx-vision",
        "xtls-rprx-vision-udp443"
    };

    public static readonly List<string> XhttpMode =
    [
        "auto",
        "packet-up",
        "stream-up",
        "stream-one"
    ];

    public static readonly List<string> LogLevels =
    [
        "debug",
        "info",
        "warning",
        "error",
        "none"
    ];

    public static readonly List<string> Hysteria2Obfs = new()
    {
        "salamander",
    };

    public static readonly List<int> TunMtus =
    [
        1280,
        1408,
        1500,
        4064,
        9000,
        65535
    ];

    public static readonly Dictionary<EConfigType, string> ProtocolShares = new()
    {
        { EConfigType.VMess, "vmess://" },
        { EConfigType.Shadowsocks, "ss://" },
        { EConfigType.SOCKS, "socks://" },
        { EConfigType.VLESS, "vless://" },
        { EConfigType.Trojan, "trojan://" },
        { EConfigType.Hysteria2, "hysteria2://" },
        { EConfigType.WireGuard, "wireguard://" },
        { EConfigType.TUIC, "tuic://" },
        { EConfigType.AnyTLS, "anytls://" },
        { EConfigType.Naive, "naive+https://" },
        { EConfigType.MASQUE, "masque://" },
    };
}

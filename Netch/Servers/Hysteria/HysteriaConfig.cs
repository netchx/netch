#nullable disable
namespace Netch.Servers;

public class HysteriaConfig
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string server { get; set; }

    /// <summary>
    /// 协议
    /// </summary>
    public string protocol { get; set; }

    /// <summary>
    /// 混淆密码
    /// </summary>
    public string obfs { get; set; }

    /// <summary>
    /// QUIC TLS ALPN
    /// </summary>
    public string alpn { get; set; }

    /// <summary>
    /// Base64 认证密钥
    /// </summary>
    public string auth { get; set; }

    /// <summary>
    /// 字符串认证密钥
    /// </summary>
    public string auth_str { get; set; }

    /// <summary>
    /// 服务器名称指示
    /// </summary>
    public string server_name { get; set; }

    /// <summary>
    /// 忽略证书错误
    /// </summary>
    public bool insecure { get; set; }

    /// <summary>
    /// 最大上行
    /// </summary>
    public int up_mbps { get; set; }

    /// <summary>
    /// 最大下行
    /// </summary>
    public int down_mbps { get; set; }

    /// <summary>
    /// QUIC 流接收窗口
    /// </summary>
    public int recv_window_conn { get; set; }

    /// <summary>
    /// QUIC 连接接收窗口
    /// </summary>
    public int recv_window { get; set; }

    /// <summary>
    /// socks5 配置
    /// </summary>
    public Socks5Config socks5 { get; set; }

    /// <summary>
    /// 禁用 MTU 探测
    /// </summary>
    public bool disable_mtu_discovery { get; set; }
}

public class Socks5Config
{
    /// <summary>
    /// socks5 监听地址
    /// </summary>
    public string listen { get; set; }
}
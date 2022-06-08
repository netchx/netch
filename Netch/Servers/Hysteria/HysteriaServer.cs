using Netch.Models;

namespace Netch.Servers;

public class HysteriaServer : Server
{
    public override string Type { get; } = "Hysteria";

    public override string MaskedData()
    {
        return "";
    }

    /// <summary>
    /// 协议
    /// </summary>
    public string Protocol { get; set; } = "udp";

    /// <summary>
    /// 混淆密码
    /// </summary>
    public string OBFS { get; set; } = string.Empty;

    /// <summary>
    /// QUIC TLS ALPN
    /// </summary>
    public string ALPN { get; set; } = "h3";

    /// <summary>
    /// 认证类型
    /// </summary>
    public string AuthType { get; set; } = "DISABLED";

    /// <summary>
    /// 认证载荷
    /// </summary>
    public string AuthPayload { get; set; } = string.Empty;

    /// <summary>
    /// 服务器名称指示
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// 忽略证书错误
    /// </summary>
    public string Insecure { get; set; } = "false";

    /// <summary>
    /// 最大上行
    /// </summary>
    public int UpMbps { get; set; } = 100;

    /// <summary>
    /// 最大下行
    /// </summary>
    public int DownMbps { get; set; } = 100;

    /// <summary>
    /// QUIC 流接收窗口
    /// </summary>
    public int RecvWindowConn { get; set; } = 15728640;

    /// <summary>
    /// QUIC 连接接收窗口
    /// </summary>
    public int RecvWindow { get; set; } = 67108864;

    /// <summary>
    /// 禁用 MTU 探测
    /// </summary>
    public string DisableMTUDiscovery { get; set; } = "false";
}

public class HysteriaGlobal
{
    public static readonly List<string> Protocol = new()
    {
        "udp",
        "wechat-video"
    };

    public static readonly List<string> Auth_Type = new()
    {
        "DISABLED",
        "BASE64",
        "STR"
    };

    public static readonly List<string> Insecure = new()
    {
        "true",
        "false"
    };

    public static readonly List<string> Disable_MTU_Discovery = new()
    {
        "true",
        "false"
    };
}
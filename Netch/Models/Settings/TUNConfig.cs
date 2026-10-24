namespace Netch.Models;

/// <summary>
///     TUN/TAP 适配器配置类
/// </summary>
public class TUNConfig
{
    /// <summary>
    ///     地址
    /// </summary>
    public string Address { get; set; } = "10.0.236.10";

    /// <summary>
    ///     DNS
    /// </summary>
    public string DNS { get; set; } = Constants.DefaultPrimaryDNS;

    /// <summary>
    ///     网关
    /// </summary>
    public string Gateway { get; set; } = "10.0.236.1";

    /// <summary>
    ///     掩码
    /// </summary>
    public string Netmask { get; set; } = "255.255.255.0";

    /// <summary>
    ///     是否启用 TUN IPv6 地址和路由
    /// </summary>
    public bool EnableIPv6 { get; set; } = true;

    /// <summary>
    ///     TUN IPv6 地址
    /// </summary>
    public string IPv6Address { get; set; } = "fd00:236::10";

    /// <summary>
    ///     TUN IPv6 网关
    /// </summary>
    public string IPv6Gateway { get; set; } = "fd00:236::1";

    /// <summary>
    ///     TUN IPv6 前缀长度
    /// </summary>
    public byte IPv6PrefixLength { get; set; } = 64;

    /// <summary>
    ///     模式 2 下是否代理 DNS
    /// </summary>
    public bool ProxyDNS { get; set; } = false;

    /// <summary>
    ///     使用自定义 DNS 设置
    /// </summary>
    public bool UseCustomDNS { get; set; } = false;

    /// <summary>
    ///     Global bypass IPs
    /// </summary>
    public List<string> BypassIPs { get; set; } = new();
}

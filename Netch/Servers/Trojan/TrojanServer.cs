using Netch.Models;

namespace Netch.Servers;

public class TrojanServer : Server
{
    public override string Type { get; } = "Trojan";

    public override string MaskedData()
    {
        return "";
    }

    /// <summary>
    ///     密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     伪装域名
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// 传输协议
    /// </summary>
    public string Protocol { get; set; } = "original";

    /// <summary>
    /// 加密
    /// </summary>
    public string Encryption { get; set; } = "none";

    /// <summary>
    /// Websocket 主机
    /// </summary>
    public string WebsocketHost { get; set; } = string.Empty;

    /// <summary>
    /// Websocket 路径
    /// </summary>
    public string WebsocketPath { get; set; } = string.Empty;

    /// <summary>
    /// Shadowsocks 加密方式
    /// </summary>
    public string ShadowsocksEncryption { get; set; } = "aes-128-gcm";

    /// <summary>
    /// Shadowsocks 密码
    /// </summary>
    public string ShadowsocksPassword { get; set; } = string.Empty;
}

public class TrojanGoGlobal
{
    public static readonly List<string> TransportProtocol = new()
    {
        "original",
        "ws"
    };

    public static readonly List<string> TrojanGoEncryptMethod = new()
    {
        "none",
        "ss"
    };

    public static readonly List<string> ShadowsocksEncryptMethod = new()
    {
        "aes-128-gcm",
        "aes-256-gcm",
        "chacha20-ietf-poly1305"
    };
}
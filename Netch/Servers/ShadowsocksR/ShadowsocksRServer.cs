using Netch.Models;

namespace Netch.Servers;

public class ShadowsocksRServer : Server
{
    public override string Type { get; } = "SSR";
    public override string MaskedData()
    {
        return $"{EncryptMethod} + {Protocol} + {OBFS}";
    }

    /// <summary>
    ///     密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     加密方式
    /// </summary>
    public string EncryptMethod { get; set; } = SSRGlobal.EncryptMethods[4];

    /// <summary>
    ///     协议
    /// </summary>
    public string Protocol { get; set; } = SSRGlobal.Protocols[0];

    /// <summary>
    ///     协议参数
    /// </summary>
    public string? ProtocolParam { get; set; }

    /// <summary>
    ///     混淆
    /// </summary>
    public string OBFS { get; set; } = SSRGlobal.OBFSs[0];

    /// <summary>
    ///     混淆参数
    /// </summary>
    public string? OBFSParam { get; set; }
}

public class SSRGlobal
{
    /// <summary>
    ///     SSR 协议列表
    /// </summary>
    public static readonly List<string> Protocols = new()
    {
        "origin",
        "auth_sha1_v4",
        "auth_aes128_md5",
        "auth_aes128_sha1",
        "auth_chain_a",
        "auth_chain_b"
    };

    /// <summary>
    ///     SSR 混淆列表
    /// </summary>
    public static readonly List<string> OBFSs = new()
    {
        "plain",
        "http_simple",
        "http_post",
        "tls_simple",
        "tls1.2_ticket_auth",
        "tls1.2_ticket_fastauth",
        "random_head"
    };

    /// <summary>
    ///     SS/SSR 加密方式
    /// </summary>
    public static readonly List<string> EncryptMethods = SSGlobal.EncryptMethods;
}
namespace Netch.Servers;

public class VmessQRCode
{
    /// <summary>
    ///     链接版本
    /// </summary>
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public int v { get; set; } = 2;

    /// <summary>
    ///     备注
    /// </summary>
    public string ps { get; set; } = string.Empty;

    /// <summary>
    ///     地址
    /// </summary>
    public string add { get; set; } = string.Empty;

    /// <summary>
    ///     端口
    /// </summary>
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public ushort port { get; set; }

    /// <summary>
    ///     用户 ID
    /// </summary>
    public string id { get; set; } = string.Empty;

    /// <summary>
    ///     额外 ID
    /// </summary>
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public int aid { get; set; }

    /// <summary>
    ///     加密方式 (security)
    /// </summary>
    public string scy { get; set; } = "auto";

    /// <summary>
    ///     传输协议
    /// </summary>
    public string net { get; set; } = string.Empty;

    /// <summary>
    ///     伪装类型
    /// </summary>
    public string type { get; set; } = string.Empty;

    /// <summary>
    ///     伪装域名（HTTP，WS）
    /// </summary>
    public string host { get; set; } = string.Empty;

    /// <summary>
    ///     伪装路径/服务名称
    /// </summary>
    public string path { get; set; } = string.Empty;

    /// <summary>
    ///     是否使用 TLS
    /// </summary>
    public string tls { get; set; } = string.Empty;

    /// <summary>
    ///     serverName
    /// </summary>
    public string sni { get; set; } = string.Empty;

    public string alpn { get; set; } = string.Empty;

    public string fp { get; set; } = string.Empty;

    public string insecure { get; set; } = string.Empty;
}

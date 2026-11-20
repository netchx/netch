using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Netch.Servers;

public class VmessQRCode
{
    /// <summary>
    ///     链接版本
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
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
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ushort port { get; set; }

    /// <summary>
    ///     用户 ID
    /// </summary>
    public string id { get; set; } = string.Empty;

    /// <summary>
    ///     额外 ID
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
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

    [JsonConverter(typeof(FlexibleStringConverter))]
    public string insecure { get; set; } = string.Empty;

    public string? extra { get; set; }
    public string? pbk { get; set; }
    public string? sid { get; set; }
    public string? spx { get; set; }
    public string? pqv { get; set; }
    public string? ech { get; set; }
    public string? echForceQuery { get; set; }
    public string? pcs { get; set; }
}

internal sealed class FlexibleStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.True => "1",
            JsonTokenType.False => "0",
            JsonTokenType.Number when reader.TryGetInt64(out var number) => number.ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => string.Empty,
            _ => throw new JsonException()
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

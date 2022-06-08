using Netch.Models;

namespace Netch.Servers;

public class VLiteServer : Server
{
    public override string Type { get; } = "VLite";
    public override string MaskedData()
    {
        // return $"{ScramblePacket} + {ScramblePacket} + {EnableFec} + {EnableStabilization} + {EnableRenegotiation} + {HandshakeMaskingPaddingSize}";
        return "";
    }

    /// <summary>
    ///     密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     数据包混淆
    /// </summary>
    public string ScramblePacket { get; set; } = "false";

    /// <summary>
    ///     前向错误修复
    /// </summary>
    public string EnableFec { get; set; } = "false";

    /// <summary>
    ///     通用连接稳定机制
    /// </summary>
    public string EnableStabilization { get; set; } = "false";

    /// <summary>
    ///     通用连接稳定重协议协商机制
    /// </summary>
    public string EnableRenegotiation { get; set; } = "false";

    /// <summary>
    ///     混淆通用连接稳定握手消息
    /// </summary>
    public int HandshakeMaskingPaddingSize { get; set; }
}

public class VLiteGlobal
{
    public static readonly List<string> ScramblePacket = new()
    {
        "true",
        "false"
    };

    public static readonly List<string> EnableFec = new()
    {
        "true",
        "false"
    };

    public static readonly List<string> EnableStabilization = new()
    {
        "true",
        "false"
    };

    public static readonly List<string> EnableRenegotiation = new()
    {
        "true",
        "false"
    };
}
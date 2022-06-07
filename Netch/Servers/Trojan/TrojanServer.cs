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
    ///     TLS 底层传输安全
    /// </summary>
    public string TLSSecureType
    {
        get => _tlsSecureType;
        set
        {
            if (value == "")
                value = "none";

            _tlsSecureType = value;
        }
    } = VLESSGlobal.TLSSecure[1];
}
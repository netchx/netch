using Netch.Models;

namespace Netch.Servers;

public class TrojanServer : Server
{
    private string _tlsSecureType = VLESSGlobal.TLSSecure[1];

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

    public string TransferProtocol { get; set; } = "tcp";

    public string? TransportHost { get; set; }

    public string? Path { get; set; }

    public string? Alpn { get; set; }

    public string Flow { get; set; } = "xtls-rprx-direct";

    /// <summary>
    ///     TLS 底层传输安全
    /// </summary>
    public string TLSSecureType
    {
        get => _tlsSecureType;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                value = VLESSGlobal.TLSSecure[1];

            _tlsSecureType = value;
        }
    }
}

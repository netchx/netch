namespace Netch.Enums;

public enum EConfigType
{
    VMess,
    Shadowsocks,
    ShadowsocksR,
    SOCKS,
    VLESS,
    Trojan,
    Hysteria2,
    WireGuard,
    HTTP,
    SSH,
    // Keep the original values above stable for existing Netch settings.
    TUIC,
    AnyTLS,
    Naive,
    MASQUE,
}

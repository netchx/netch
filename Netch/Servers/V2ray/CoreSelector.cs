using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

/// <summary>
/// Selects the proxy core from the server protocol and its transport settings.
/// </summary>
public static class CoreSelector
{
    public static bool UseXray(Server server)
    {
        return server.ConfigType switch
        {
            EConfigType.VLESS => server.StreamSecurity is "tls" or "reality",
            EConfigType.VMess => server.StreamSecurity is "tls" or "reality",
            EConfigType.Trojan => server.StreamSecurity is "tls" or "reality",
            EConfigType.WireGuard or EConfigType.Hysteria2 or EConfigType.MASQUE => true,
            EConfigType.Shadowsocks => Constants.SsSecuritiesInXray.Contains(server.ProtoExtra.SsMethod),
            _ => false
        };
    }
}

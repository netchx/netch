using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public static class CoreSelector
{
    public static IServerController CreateController(Server server)
    {
        return UsesXhttp(server) ? new XrayController() : new SingboxController();
    }

    public static bool UsesXhttp(Server server)
    {
        return server.ConfigType is EConfigType.VMess or EConfigType.VLESS or EConfigType.Trojan or EConfigType.Shadowsocks
            && string.Equals(server.Network?.Trim(), nameof(ETransport.xhttp), StringComparison.OrdinalIgnoreCase);
    }
}

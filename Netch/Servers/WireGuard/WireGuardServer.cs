using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class WireGuardServer : Server
{
    public override EConfigType ConfigType { get; } = EConfigType.WireGuard;

    public override string MaskedData()
    {
        return $"{ProtoExtra.WgInterfaceAddress} + {ProtoExtra.WgMtu}";
    }
}

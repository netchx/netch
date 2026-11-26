using Netch.Enums;

namespace Netch.Servers;

public class AmneziaWGServer : WireGuardServer
{
    public override EConfigType ConfigType { get; } = EConfigType.AmneziaWG;

    public override string MaskedData()
    {
        return $"AWG · {ProtoExtra.WgInterfaceAddress} + {ProtoExtra.WgMtu}";
    }
}

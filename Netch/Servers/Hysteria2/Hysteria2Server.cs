using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class Hysteria2Server : Server
{
    public override EConfigType ConfigType { get; } = EConfigType.Hysteria2;

    public override string MaskedData()
    {
        return "";
    }
}

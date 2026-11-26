using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class TrojanServer : Server
{
    public override EConfigType ConfigType { get; } = EConfigType.Trojan;

    public override string MaskedData()
    {
        return "";
    }
}
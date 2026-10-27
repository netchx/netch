using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class ShadowsocksServer : Server
{
    public override EConfigType ConfigType { get; } = EConfigType.Shadowsocks;
    public override string MaskedData()
    {
        return $"{ProtoExtra.SsMethod}";
    }
}
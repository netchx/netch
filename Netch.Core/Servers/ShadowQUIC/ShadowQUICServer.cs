using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class ShadowQUICServer : Server
{
    public override EConfigType ConfigType => EConfigType.ShadowQUIC;

    public override string MaskedData()
    {
        return Sni.IsNotEmpty() ? $"QUIC · {Sni}" : "QUIC";
    }
}

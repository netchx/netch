using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class MieruServer : Server
{
    public override EConfigType ConfigType => EConfigType.Mieru;

    public override string MaskedData()
    {
        var transport = ProtoExtra.MieruTransport.IsNotEmpty() ? ProtoExtra.MieruTransport : "TCP";
        var portDisplay = ProtoExtra.MieruPortRange.IsNotEmpty() ? ProtoExtra.MieruPortRange : Port.ToString();
        return $"{transport} · {portDisplay}";
    }
}

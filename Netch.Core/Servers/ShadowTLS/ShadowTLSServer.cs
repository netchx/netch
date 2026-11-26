using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public sealed class ShadowTLSServer : Server
{
    public override EConfigType ConfigType => EConfigType.ShadowTLS;

    public ShadowTLSServer()
    {
        StreamSecurity = Constants.StreamSecurity;
        ProtoExtra.ShadowTlsVersion = 3;
        ProtoExtra.SsMethod = Constants.SsCiphers[0];
    }

    public override string MaskedData()
    {
        var version = ProtoExtra.ShadowTlsVersion is >= 1 and <= 3 ? $"v{ProtoExtra.ShadowTlsVersion}" : "v3";
        return $"{version} / {ProtoExtra.SsMethod}";
    }
}

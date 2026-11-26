using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class TrustTunnelServer : Server
{
    public TrustTunnelServer()
    {
        StreamSecurity = Constants.StreamSecurity;
    }

    public override EConfigType ConfigType => EConfigType.TrustTunnel;

    public override string MaskedData()
    {
        var transport = ProtoExtra.TrustTunnelQuic == true ? "HTTP/3" : "HTTP/2";
        return Username.IsNotEmpty() ? $"{transport} · {Username}" : transport;
    }
}

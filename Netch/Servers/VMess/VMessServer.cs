using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class VMessServer : Server
{
    public override EConfigType ConfigType { get; } = EConfigType.VMess;

    public override string MaskedData()
    {
        var maskedData = $"{ProtoExtra.VmessSecurity} + {Network} + {HeaderType}";
        switch (Network)
        {
            case "tcp":
            case "ws":
                maskedData += $" + {StreamSecurity}";
                break;
            case "quic":
                maskedData += $" + {RequestHost}";
                break;
            case "grpc":
                break;
            case "kcp":
                break;
        }

        return maskedData;
    }   
}
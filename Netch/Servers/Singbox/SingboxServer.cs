using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public abstract class SingboxServerBase : Server
{
    protected SingboxServerBase()
    {
        StreamSecurity = Constants.StreamSecurity;
    }

    public override string MaskedData()
    {
        return ConfigType switch
        {
            EConfigType.TUIC => $"UUID: {Username}",
            EConfigType.AnyTLS or EConfigType.Naive => "TLS",
            EConfigType.MASQUE => $"User: {Username}",
            _ => string.Empty
        };
    }
}

public sealed class TUICServer : SingboxServerBase
{
    public override EConfigType ConfigType => EConfigType.TUIC;
}

public sealed class AnyTLSServer : SingboxServerBase
{
    public override EConfigType ConfigType => EConfigType.AnyTLS;
}

public sealed class NaiveServer : SingboxServerBase
{
    public override EConfigType ConfigType => EConfigType.Naive;
}

public sealed class MASQUEServer : SingboxServerBase
{
    public override EConfigType ConfigType => EConfigType.MASQUE;
}

using Netch.Enums;

namespace Netch.Servers;

public class VLESSServer : VMessServer
{
    public override EConfigType ConfigType { get; } = EConfigType.VLESS;
}
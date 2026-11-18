using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public sealed class ShadowsocksRServer : Server
{
    public override EConfigType ConfigType => EConfigType.ShadowsocksR;
    public string EncryptMethod { get; set; } = "aes-256-cfb";
    public string Protocol { get; set; } = "origin";
    public string? ProtocolParam { get; set; }
    public string OBFS { get; set; } = "plain";
    public string? OBFSParam { get; set; }

    public override string MaskedData() => $"{EncryptMethod} + {Protocol} + {OBFS}";
}

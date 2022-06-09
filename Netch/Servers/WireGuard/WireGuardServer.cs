using Netch.Models;

namespace Netch.Servers;

public class WireGuardServer : Server
{
    public override string Type { get; } = "WireGuard";
    
    public override string MaskedData()
    {
        return $"{LocalAddresses} + {MTU}";
    }

    /// <summary>
    ///     本地地址
    /// </summary>
    public string LocalAddresses { get; set; } = "172.16.0.2";

    /// <summary>
    ///     节点公钥
    /// </summary>
    public string PeerPublicKey { get; set; } = string.Empty;

    /// <summary>
    ///     私钥
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    ///     节点预共享密钥
    /// </summary>
    public string? PreSharedKey { get; set; }

    /// <summary>
    ///     MTU
    /// </summary>
    public int MTU { get; set; } = 1420;
}

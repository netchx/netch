using Netch.Models;

namespace Netch.Servers;

public class SSHServer : Server
{
    public override string Type { get; } = "SSH";
    
    public override string MaskedData()
    {
        return $"{User}";
    }

    /// <summary>
    ///     用户
    /// </summary>
    public string User { get; set; } = "root";

    /// <summary>
    ///     密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     私钥
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    ///     主机公钥
    /// </summary>
    public string? PublicKey { get; set; }
}

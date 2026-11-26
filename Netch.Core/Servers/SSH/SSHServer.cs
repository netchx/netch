using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public sealed class SSHServer : Server
{
    public override EConfigType ConfigType => EConfigType.SSH;

    public SSHServer()
    {
        Port = 22;
    }

    public override string MaskedData()
    {
        var auth = ProtoExtra.SshPrivateKey.IsNotEmpty()
            ? (Password.IsNotEmpty() ? "key+pass" : "key")
            : "password";
        return $"{Username} ({auth})";
    }
}

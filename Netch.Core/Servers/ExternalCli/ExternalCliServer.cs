using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class ExternalCliServer : Server
{
    public ExternalCliServer()
    {
        Address = "127.0.0.1";
        Port = 1080;
    }

    public override EConfigType ConfigType { get; } = EConfigType.ExternalCli;

    public override string MaskedData()
    {
        var exeName = System.IO.Path.GetFileName(ProtoExtra.CliExecutable?.Trim() ?? string.Empty);
        return $"{exeName} -> {Address}:{Port}";
    }
}

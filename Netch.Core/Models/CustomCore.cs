using Netch.Enums;

namespace Netch.Models;

public enum CustomCoreMode
{
    CliArguments = 0,
    MihomoConfig = 1,
    SingboxConfig = 2,
    XrayConfig = 3
}

public class CustomCore
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public EConfigType Protocol { get; set; } = EConfigType.Shadowsocks;

    public string ExecutablePath { get; set; } = string.Empty;

    public CustomCoreMode Mode { get; set; } = CustomCoreMode.CliArguments;

    public string ArgumentsTemplate { get; set; } = string.Empty;

    public string ConfigTemplate { get; set; } = string.Empty;

    public string ConfigFileExtension { get; set; } = ".json";

    public bool IsProtocolDefault { get; set; }
}

namespace Netch.Models;

public class WebProxyConfig
{
    public const string DefaultBypassValue =
        "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;<local>";

    public ushort PacPort { get; set; } = 2803;

    public string DefaultBypass { get; set; } = DefaultBypassValue;
}

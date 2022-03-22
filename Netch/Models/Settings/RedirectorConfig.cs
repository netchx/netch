namespace Netch.Models;

public class RedirectorConfig
{
    public bool FilterTCP { get; set; } = true;

    public bool FilterUDP { get; set; } = true;

    public bool FilterDNS { get; set; } = true;

    public bool FilterParent { get; set; } = false;

    public bool HandleOnlyDNS { get; set; } = true;

    public bool DNSProxy { get; set; } = true;

    public string DNSHost { get; set; } = $"{Constants.DefaultPrimaryDNS}:53";

    public int ICMPDelay { get; set; } = 10;

    public bool FilterICMP { get; set; } = false;
}
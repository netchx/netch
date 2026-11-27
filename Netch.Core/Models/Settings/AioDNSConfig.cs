using System.Text.Json.Serialization;

namespace Netch.Models;

public class AioDNSConfig
{
    public string ChinaDNS { get; set; } = $"udp://{Constants.DefaultCNPrimaryDNS}:53";

    public string OtherDNS { get; set; } = $"udp://{Constants.DefaultPrimaryDNS}:53";

    public List<string> CustomChinaDns { get; set; } = new();

    public List<string> CustomOtherDns { get; set; } = new();

    [JsonIgnore]
    public ushort ListenPort { get; set; } = 53;
}

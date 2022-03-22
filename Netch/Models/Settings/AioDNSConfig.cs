using System.Text.Json.Serialization;

namespace Netch.Models;

public class AioDNSConfig
{
    public string ChinaDNS { get; set; } = $"tcp://{Constants.DefaultCNPrimaryDNS}:53";

    public string OtherDNS { get; set; } = $"tcp://{Constants.DefaultPrimaryDNS}:53";

    [JsonIgnore]
    public ushort ListenPort { get; set; } = 53;
}
namespace Netch.Models
{
    public class AioDNSConfig
    {
        public string ChinaDNS { get; set; } = $"tcp://{Constants.DefaultCNPrimaryDNS}";

        public string OtherDNS { get; set; } = $"tcp://{Constants.DefaultPrimaryDNS}";

        public ushort ListenPort { get; set; } = 253;
    }
}
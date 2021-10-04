namespace Netch.Models.Config
{
    public class DNSProxy
    {
        [Newtonsoft.Json.JsonProperty("otherdns")]
        public string OtherDNS = "tls://8.8.8.8:853";

        [Newtonsoft.Json.JsonProperty("args")]
        public string Args = "";
    }
}

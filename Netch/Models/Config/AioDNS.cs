namespace Netch.Models.Config
{
    public class AioDNS
    {
        /// <summary>
        ///     监听地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("listenport")]
        public ushort ListenPort = 53;

        /// <summary>
        ///     国内 DNS 地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("chinadns")]
        public string ChinaDNS = "tcp://119.29.29.29:53";

        /// <summary>
        ///     国外 DNS 地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("otherdns")]
        public string OtherDNS = "tls://1.1.1.1:853";
    }
}

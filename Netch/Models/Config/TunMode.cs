using System.Collections.Generic;

namespace Netch.Models.Config
{
    public class TunMode
    {
        /// <summary>
        ///     地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("network")]
        public string Network = "100.64.0.100/24";

        /// <summary>
        ///     网关
        /// </summary>
        [Newtonsoft.Json.JsonProperty("gateway")]
        public string Gateway = "100.64.0.1";

        /// <summary>
        ///     DNS
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dns")]
        public string DNS = "aiodns";

        /// <summary>
        ///     绕过 IP 地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("bypass")]
        public List<string> BypassIPs = new();
    }
}

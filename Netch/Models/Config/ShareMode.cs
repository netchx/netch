using System.Collections.Generic;

namespace Netch.Models.Config
{
    public class ShareMode
    {
        /// <summary>
        ///     硬件地址（用于 ARP 回复）
        ///     
        ///     CuteCR
        ///     43:75:74:65:43:52    
        /// 
        ///     NetchX
        ///     4e:65:74:63:68:58
        /// </summary>
        [Newtonsoft.Json.JsonProperty("hardware")]
        public string Hardware = "4e:65:74:63:68:58";

        /// <summary>
        ///     地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("network")]
        public string Network = "100.64.0.0/24";

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
        ///     网卡名（默认自动检测）
        /// </summary>
        public string EthernetName = "auto";

        /// <summary>
        ///     绕过 IP 地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("bypass")]
        public List<string> BypassIPs = new();
    }
}

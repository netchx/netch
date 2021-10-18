using System.Collections.Generic;

namespace Netch.Models.Mode.ProcessMode
{
    public class ProcessMode : Mode
    {
        public ProcessMode()
        {
            this.Type = ModeType.ProcessMode;
        }

        /// <summary>
        ///     过滤 IPv4 + IPv6 环路流量
        /// </summary>
        [Newtonsoft.Json.JsonProperty("filterLoopback")]
        public bool Loopback = false;

        /// <summary>
        ///     过滤 内网 流量
        /// </summary>
        [Newtonsoft.Json.JsonProperty("filterIntranet")]
        public bool Intranet = false;

        /// <summary>
        ///     过滤 ICMP 流量（伪造 ICMP 回复）
        /// </summary>
        [Newtonsoft.Json.JsonProperty("filterICMP")]
        public bool ICMP = true;

        /// <summary>
        ///     过滤 TCP 流量
        /// </summary>
        [Newtonsoft.Json.JsonProperty("filterTCP")]
        public bool TCP = true;

        /// <summary>
        ///     过滤 UDP 流量
        /// </summary>
        [Newtonsoft.Json.JsonProperty("filterUDP")]
        public bool UDP = true;

        /// <summary>
        ///     过滤 DNS 流量
        /// </summary>
        [Newtonsoft.Json.JsonProperty("filterDNS")]
        public bool DNS = true;

        /// <summary>
        ///     绕过列表
        /// </summary>
        [Newtonsoft.Json.JsonProperty("bypass")]
        public List<string> BypassList;

        /// <summary>
        ///     代理列表
        /// </summary>
        [Newtonsoft.Json.JsonProperty("handle")]
        public List<string> HandleList;
    }
}

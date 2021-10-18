namespace Netch.Models.Config
{
    public class ProcessMode
    {
        /// <summary>
        ///     伪造 ICMP 延迟
        /// </summary>
        [Newtonsoft.Json.JsonProperty("icmping")]
        public int Icmping = 1;

        /// <summary>
        ///     仅劫持规则内进程
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dnsOnly")]
        public bool DNSOnly = false;

        /// <summary>
        ///     远程 DNS 查询
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dnsProx")]
        public bool DNSProx = true;

        /// <summary>
        ///     DNS 地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dnsHost")]
        public string DNSHost = "1.1.1.1";

        /// <summary>
        ///     DNS 端口
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dnsPort")]
        public ushort DNSPort = 53;
    }
}

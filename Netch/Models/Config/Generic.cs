namespace Netch.Models.Config
{
    public class Generic
    {
        /// <summary>
        ///     检查 Unstable 更新
        /// </summary>
        [Newtonsoft.Json.JsonProperty("unstable")]
        public bool Unstable = false;

        /// <summary>
        ///     使用 ICMP 测试延迟
        /// </summary>
        [Newtonsoft.Json.JsonProperty("icmping")]
        public bool ICMPing = true;

        /// <summary>
        ///     使用 AioDNS 解析器
        /// </summary>
        [Newtonsoft.Json.JsonProperty("aiodns")]
        public bool AioDNS = false;

        /// <summary>
        ///     使用 XRay 后端
        /// </summary>
        [Newtonsoft.Json.JsonProperty("xray")]
        public bool XRay = false;
    }
}

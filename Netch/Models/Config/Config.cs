using System.Collections.Generic;

namespace Netch.Models.Config
{
    public class Config
    {
        /// <summary>
        ///     配置 版本
        /// </summary>
        [Newtonsoft.Json.JsonProperty("verCode")]
        public int VerCode = 1;

        /// <summary>
        ///     通用 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("generic")]
        public Generic Generic = new();

        /// <summary>
        ///     端口 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("ports")]
        public Ports Ports = new();

        /// <summary>
        ///     ProcessMode 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("processmode")]
        public ProcessMode ProcessMode = new();

        /// <summary>
        ///     ShareMode 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("sharemode")]
        public ShareMode ShareMode = new();

        /// <summary>
        ///     TunMode 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("tunmode")]
        public TunMode TunMode = new();

        /// <summary>
        ///     AioDNS 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("aiodns")]
        public AioDNS AioDNS = new();

        /// <summary>
        ///     DNSProxy 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dnsproxy")]
        public DNSProxy DNSProxy = new();

        /// <summary>
        ///     V2Ray 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("v2ray")]
        public V2Ray V2Ray = new();

        /// <summary>
        ///     V2Ray 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("xray")]
        public XRay XRay = new();

        /// <summary>
        ///     STUN 配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("stun")]
        public STUN STUN = new();

        /// <summary>
        ///     订阅链接
        /// </summary>
        [Newtonsoft.Json.JsonProperty("subscriptions")]
        public List<Subscription> Subscriptions = new();
    }
}

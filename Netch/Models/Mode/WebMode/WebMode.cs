using System.Collections.Generic;

namespace Netch.Models.Mode.WebMode
{
    public class WebMode : Mode
    {
        public WebMode()
        {
            this.Type = ModeType.WebMode;
        }

        /// <summary>
        ///     设置系统代理
        /// </summary>
        [Newtonsoft.Json.JsonProperty("setSystemProxy")]
        public bool SetSystemProxy;

        /// <summary>
        ///     绕过域名后缀
        /// </summary>
        [Newtonsoft.Json.JsonProperty("bypassDomainSuffix")]
        public List<string> BypassDomainSuffix;

        /// <summary>
        ///     绕过 IP 地址
        /// </summary>
        [Newtonsoft.Json.JsonProperty("bypassIPs")]
        public List<string> BypassIPs;
    }
}

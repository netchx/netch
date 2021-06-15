using System.Collections.Generic;

namespace Netch.Models.Mode.TunMode
{
    public class TunMode : Mode
    {
        public TunMode()
        {
            this.Type = ModeType.TunMode;
        }

        /// <summary>
        ///     绕过列表（IP CIDR）
        /// </summary>
        [Newtonsoft.Json.JsonProperty("bypass")]
        public List<string> BypassList;

        /// <summary>
        ///     代理列表（IP CIDR）
        /// </summary>
        [Newtonsoft.Json.JsonProperty("handle")]
        public List<string> HandleList;
    }
}

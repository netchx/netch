using System.Collections.Generic;

namespace Netch.Models.Mode.TapMode
{
    public class TapMode : Mode
    {
        public TapMode()
        {
            this.Type = ModeType.TapMode;
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

using System.Collections.Generic;

namespace Netch.Models.Mode.ShareMode
{
    public class ShareMode : Mode
    {
        public ShareMode()
        {
            this.Type = ModeType.ShareMode;
        }

        /// <summary>
        ///     绕过列表（IP CIDR）
        /// </summary>
        [Newtonsoft.Json.JsonProperty("bypass")]
        public List<string> BypassList;
    }
}

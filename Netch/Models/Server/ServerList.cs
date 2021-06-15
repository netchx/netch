using System.Collections.Generic;

namespace Netch.Models.Server
{
    public class ServerList
    {
        /// <summary>
        ///     群组
        /// </summary>
        [Newtonsoft.Json.JsonProperty("name")]
        public string Group;

        /// <summary>
        ///     节点
        /// </summary>
        [Newtonsoft.Json.JsonProperty("list")]
        public List<Server> List;
    }
}

namespace Netch.Models.Config
{
    public class Ports
    {
        /// <summary>
        ///     Socks 端口
        /// </summary>
        [Newtonsoft.Json.JsonProperty("socks")]
        public int Socks = 2081;

        /// <summary>
        ///     Mixed 端口
        /// </summary>
        [Newtonsoft.Json.JsonProperty("mixed")]
        public int Mixed = 2082;
    }
}

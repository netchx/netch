namespace Netch.Models.Config
{
    public class ProcessMode
    {
        /// <summary>
        ///     DNS
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dns")]
        public string DNS = "1.1.1.1:53";
    }
}

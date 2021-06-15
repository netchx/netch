namespace Netch.Models.GitHub
{
    public class Asset
    {
        /// <summary>
        ///     name
        /// </summary>
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name;

        /// <summary>
        ///     browser_download_url
        /// </summary>
        [Newtonsoft.Json.JsonProperty("browser_download_url")]
        public string URL;

        /// <summary>
        ///     size
        /// </summary>
        [Newtonsoft.Json.JsonProperty("size")]
        public ulong Size;
    }
}

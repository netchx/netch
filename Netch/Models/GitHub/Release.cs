using System.Collections.Generic;

namespace Netch.Models.GitHub
{
    /// <summary>
    ///      https://api.github.com/repos/{owner}/{repo}/releases
    /// </summary>
    public class Release
    {
        /// <summary>
        ///     id
        /// </summary>
        [Newtonsoft.Json.JsonProperty("id")]
        public int ID;

        /// <summary>
        ///     html_url
        /// </summary>
        [Newtonsoft.Json.JsonProperty("html_url")]
        public string URL;

        /// <summary>
        ///     tag_name
        /// </summary>
        [Newtonsoft.Json.JsonProperty("tag_name")]
        public string VerCode;

        /// <summary>
        ///     draft
        /// </summary>
        [Newtonsoft.Json.JsonProperty("draft")]
        public bool Draft;

        /// <summary>
        ///     prerelease
        /// </summary>
        [Newtonsoft.Json.JsonProperty("prerelease")]
        public bool Unstable;

        /// <summary>
        ///     assets
        /// </summary>
        [Newtonsoft.Json.JsonProperty("assets")]
        public List<Asset> Files;
    }
}

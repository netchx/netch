namespace Netch.Models.Server.Clash
{
    public class Clash : Server
    {
        public Clash()
        {
            this.Type = ServerType.Clash;
        }

        /// <summary>
        ///     自定义配置
        /// </summary>
        [Newtonsoft.Json.JsonProperty("custom")]
        public bool Custom = true;

        /// <summary>
        ///     自定义配置文件路径
        /// </summary>
        [Newtonsoft.Json.JsonProperty("filepath")]
        public string FilePath;

        /// <summary>
        ///     解析链接
        /// </summary>
        /// <param name="link">链接</param>
        /// <returns>是否成功</returns>
        public bool ParseLink(string link)
        {
            return false;
        }
    }
}

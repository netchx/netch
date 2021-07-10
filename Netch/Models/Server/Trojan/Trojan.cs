namespace Netch.Models.Server.Trojan
{
    public class Trojan : Server
    {
        public Trojan()
        {
            this.Type = ServerType.Trojan;
        }

        /// <summary>
        ///     密码
        /// </summary>
        [Newtonsoft.Json.JsonProperty("password")]
        public string Password;

        /// <summary>
        ///     伪装 SNI 标头
        /// </summary>
        [Newtonsoft.Json.JsonProperty("sni")]
        public string SNI;

        /// <summary>
        ///     复用会话
        /// </summary>
        [Newtonsoft.Json.JsonProperty("reuse")]
        public bool Reuse = true;

        /// <summary>
        ///     Session Ticket 
        /// </summary>
        [Newtonsoft.Json.JsonProperty("ticket")]
        public bool Ticket = false;

        /// <summary>
        ///     不安全模式（跳过证书验证、跳过主机名验证）
        /// </summary>
        [Newtonsoft.Json.JsonProperty("insecure")]
        public bool Insecure = false;

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

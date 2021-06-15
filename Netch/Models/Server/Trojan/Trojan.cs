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
        public string Passwd;

        /// <summary>
        ///     伪装 SNI 标头
        /// </summary>
        public string SNI;

        /// <summary>
        ///     复用会话
        /// </summary>
        public bool Reuse = true;

        /// <summary>
        ///     Session Ticket 
        /// </summary>
        public bool Ticket = false;

        /// <summary>
        ///     不安全模式（跳过证书验证、跳过主机名验证）
        /// </summary>
        public bool Insecure = true;

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

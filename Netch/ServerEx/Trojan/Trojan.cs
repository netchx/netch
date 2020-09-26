using Netch.Models;

namespace Netch.ServerEx.Trojan
{
    public class Trojan : Server
    {
        public Trojan()
        {
            Type = "Trojan";
        }

        /// <summary>
        ///     密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     伪装域名
        /// </summary>
        public string Host { get; set; }
    }
}
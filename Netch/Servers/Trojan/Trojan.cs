using Netch.Models;

namespace Netch.Servers.Trojan
{
    public class Trojan : Server
    {
        public override string Type { get; } = "Trojan";

        /// <summary>
        ///     密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        ///     伪装域名
        /// </summary>
        public string? Host { get; set; }
    }
}
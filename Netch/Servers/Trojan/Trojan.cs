using Netch.Models;

namespace Netch.Servers
{
    public class Trojan : Server
    {
        public override string Type { get; } = "Trojan";
        public override string MaskedData()
        {
            return "";
        }

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
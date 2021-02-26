using Netch.Models;

namespace Netch.Servers.Socks5
{
    public class Socks5 : Server
    {
        /// <summary>
        ///     密码
        /// </summary>
        public string? Password;

        /// <summary>
        ///     账号
        /// </summary>
        public string? Username;

        public override string Type { get; } = "Socks5";

        public bool Auth()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }
    }
}
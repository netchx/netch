using Netch.Models;

namespace Netch.Servers.Socks5
{
    public class Socks5 : Server
    {
        /// <summary>
        ///     密码
        /// </summary>
        public string Password;

        /// <summary>
        ///     账号
        /// </summary>
        public string Username;

        public Socks5()
        {
            Type = "Socks5";
        }

        public bool Auth() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }
}
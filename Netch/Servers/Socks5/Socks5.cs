using Netch.Models;

namespace Netch.Servers
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

        public override string MaskedData()
        {
            return $"Auth: {Auth()}";
        }

        public Socks5()
        {
        }

        public Socks5(string hostname, ushort port)
        {
            Hostname = hostname;
            Port = port;
        }

        public Socks5(string hostname, ushort port, string username, string password) : this(hostname, port)
        {
            Username = username;
            Password = password;
        }

        public bool Auth()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }
    }
}
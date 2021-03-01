using System;
using System.Collections.Generic;
using System.Linq;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.Socks5.Form;

namespace Netch.Servers.Socks5
{
    public class S5Util : IServerUtil
    {
        public ushort Priority { get; } = 0;

        public string TypeName { get; } = "Socks5";

        public string FullName { get; } = "Socks5";

        public string ShortName { get; } = "S5";

        public string[] UriScheme { get; } = { };

        public Type ServerType { get; } = typeof(Socks5);

        public void Edit(Server s)
        {
            new Socks5Form((Socks5) s).ShowDialog();
        }

        public void Create()
        {
            new Socks5Form().ShowDialog();
        }

        public string GetShareLink(Server s)
        {
            var server = (Socks5) s;
            // https://t.me/socks?server=1.1.1.1&port=443
            return $"https://t.me/socks?server={server.Hostname}&port={server.Port}" +
                   $"{(!string.IsNullOrWhiteSpace(server.Username) ? $"&user={server.Username}" : "")}" +
                   $"{(server.Auth() ? $"&user={server.Password}" : "")}";
        }

        public IServerController GetController()
        {
            return new S5Controller();
        }

        public IEnumerable<Server> ParseUri(string text)
        {
            var dict = text.Replace("tg://socks?", "")
                .Replace("https://t.me/socks?", "")
                .Split('&')
                .Select(str => str.Split('='))
                .ToDictionary(splited => splited[0], splited => splited[1]);

            if (!dict.ContainsKey("server") || !dict.ContainsKey("port"))
                throw new FormatException();

            var data = new Socks5
            {
                Hostname = dict["server"],
                Port = ushort.Parse(dict["port"])
            };

            if (dict.ContainsKey("user") && !string.IsNullOrWhiteSpace(dict["user"]))
                data.Username = dict["user"];

            if (dict.ContainsKey("pass") && !string.IsNullOrWhiteSpace(dict["pass"]))
                data.Password = dict["pass"];

            return new[] {data};
        }

        public bool CheckServer(Server s)
        {
            return true;
        }
    }
}
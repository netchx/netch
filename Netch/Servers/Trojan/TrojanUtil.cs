using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.Trojan.Form;

namespace Netch.Servers.Trojan
{
    public class TrojanUtil : IServerUtil
    {
        public ushort Priority { get; } = 3;

        public string TypeName { get; } = "Trojan";

        public string FullName { get; } = "Trojan";

        public string ShortName { get; } = "TR";

        public string[] UriScheme { get; } = {"trojan"};

        public Type ServerType { get; } = typeof(Trojan);

        public void Edit(Server s)
        {
            new TrojanForm((Trojan) s).ShowDialog();
        }

        public void Create()
        {
            new TrojanForm().ShowDialog();
        }

        public string GetShareLink(Server s)
        {
            var server = (Trojan) s;
            return $"trojan://{HttpUtility.UrlEncode(server.Password)}@{server.Hostname}:{server.Port}#{server.Remark}";
        }

        public IServerController GetController()
        {
            return new TrojanController();
        }

        public IEnumerable<Server> ParseUri(string text)
        {
            var data = new Trojan();

            text = text.Replace("/?", "?");
            if (text.Contains("#"))
            {
                data.Remark = HttpUtility.UrlDecode(text.Split('#')[1]);
                text = text.Split('#')[0];
            }

            if (text.Contains("?"))
            {
                var reg = new Regex(@"^(?<data>.+?)\?(.+)$");
                var regmatch = reg.Match(text);

                if (!regmatch.Success)
                    throw new FormatException();

                var peer = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(text).Query).Get("peer"));

                if (peer != null)
                    data.Host = peer;

                text = regmatch.Groups["data"].Value;
            }

            var finder = new Regex(@"^trojan://(?<psk>.+?)@(?<server>.+):(?<port>\d+)");
            var match = finder.Match(text);
            if (!match.Success)
                throw new FormatException();

            data.Password = match.Groups["psk"].Value;
            data.Hostname = match.Groups["server"].Value;
            data.Port = ushort.Parse(match.Groups["port"].Value);

            return new[] {data};
        }

        public bool CheckServer(Server s)
        {
            return true;
        }
    }
}
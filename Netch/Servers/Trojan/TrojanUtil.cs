using System.Text.RegularExpressions;
using System.Web;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class TrojanUtil : IServerUtil
{
    public ushort Priority { get; } = 3;

    public string TypeName { get; } = "Trojan";

    public string FullName { get; } = "Trojan";

    public string ShortName { get; } = "TR";

    public string[] UriScheme { get; } = { "trojan" };

    public Type ServerType { get; } = typeof(TrojanServer);

    public void Edit(Server s)
    {
        new TrojanForm((TrojanServer)s).ShowDialog();
    }

    public void Create()
    {
        new TrojanForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        var server = (TrojanServer)s;
        return $"trojan://{HttpUtility.UrlEncode(server.Password)}@{server.Hostname}:{server.Port}?sni={server.Host}#{server.Remark}";
    }

    public IServerController GetController()
    {
        return new TrojanController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var data = new TrojanServer();

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

            if (peer != null) {
                data.Host = peer;
            } else {
                peer = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(text).Query).Get("sni"));
                if (peer != null)
                    data.Host = peer;
            }

            text = regmatch.Groups["data"].Value;
        }

        var finder = new Regex(@"^trojan://(?<psk>.+?)@(?<server>.+):(?<port>\d+)");
        var match = finder.Match(text);
        if (!match.Success)
            throw new FormatException();

        data.Password = HttpUtility.UrlDecode(match.Groups["psk"].Value);
        data.Hostname = match.Groups["server"].Value;
        data.Port = ushort.Parse(match.Groups["port"].Value);

        return new[] { data };
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}
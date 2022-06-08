using System.Text.RegularExpressions;
using System.Web;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class TrojanUtil : IServerUtil
{
    public ushort Priority { get; } = 3;

    public string TypeName { get; } = "Trojan";

    public string FullName { get; } = "Trojan";

    public string ShortName { get; } = "TR";

    public string[] UriScheme { get; } = { "trojan", "trojan-go" };

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

        string Type = server.Protocol == "ws" ? ("ws&host=" + HttpUtility.UrlEncode((server.WebsocketHost.ValueOrDefault() ?? server.Hostname)) + "&path=" + HttpUtility.UrlEncode(server.WebsocketPath)) : "original";
        string Encryption = server.Encryption == "ss" ? HttpUtility.UrlEncode(("ss;" + server.ShadowsocksEncryption + ';' + server.ShadowsocksPassword)) : "none";

        if (Type == "original" && Encryption == "none")
        {
            return $"trojan://{HttpUtility.UrlEncode(server.Password)}@{server.Hostname}:{server.Port}#{server.Remark}";
        }
        else
        {
            // https://p4gefau1t.github.io/trojan-go/developer/url/
            return $"trojan-go://{HttpUtility.UrlEncode(server.Password)}@{server.Hostname}:{server.Port}/?sni={HttpUtility.UrlEncode(server.Host.ValueOrDefault() ?? server.Hostname)}&type={Type}&encryption={Encryption}#{HttpUtility.UrlEncode(server.Remark)}";
        }
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
            var parameter = HttpUtility.ParseQueryString(text.Split("?")[1]);
            text = text.Substring(0, text.IndexOf("?", StringComparison.Ordinal));

            data.Host = (HttpUtility.UrlDecode(parameter.Get("sni")) ?? HttpUtility.UrlDecode(parameter.Get("peer"))) ?? data.Hostname;

            data.Protocol = parameter.Get("type") ?? "original";
            data.WebsocketHost = HttpUtility.UrlDecode(parameter.Get("host")) ?? data.Hostname;
            data.WebsocketPath = HttpUtility.UrlDecode(parameter.Get("path")) ?? "";

            string EncryptStr = HttpUtility.UrlDecode(parameter.Get("encryption")) ?? "none";

            if (EncryptStr == "none")
            {
                data.Encryption = "none";
                data.ShadowsocksEncryption = "aes-128-gcm";
                data.ShadowsocksPassword = "";
            }
            else
            {
                data.Encryption = "ss";
                data.ShadowsocksEncryption = EncryptStr.Split(';')[1];
                data.ShadowsocksPassword = EncryptStr.Split(';')[2];
            }
        }

        var finder = new Regex(@"^((trojan-go)|(trojan))://(?<psk>.+?)@(?<server>.+):(?<port>\d+)");
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
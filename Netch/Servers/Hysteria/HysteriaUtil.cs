using System.Text.RegularExpressions;
using System.Web;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class HysteriaUtil : IServerUtil
{
    public ushort Priority { get; } = 4;

    public string TypeName { get; } = "Hysteria";

    public string FullName { get; } = "Hysteria";

    public string ShortName { get; } = "HY";

    public string[] UriScheme { get; } = { "hysteria" };

    public Type ServerType { get; } = typeof(HysteriaServer);

    public void Edit(Server s)
    {
        new HysteriaForm((HysteriaServer)s).ShowDialog();
    }

    public void Create()
    {
        new HysteriaForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        var server = (HysteriaServer)s;

        // https://github.com/HyNetwork/hysteria/wiki/URI-Scheme
        return $"hysteria://{server.Hostname}:{server.Port}?protocol={server.Protocol}&auth={server.AuthPayload}&peer={server.ServerName}&insecure={(server.Insecure == "true" ? 1 : 0)}&upmbps={server.UpMbps}&downmbps={server.DownMbps}&alpn={server.ALPN}&obfs=xplus&obfsParam={server.OBFS}#{server.Remark}";
    }

    public IServerController GetController()
    {
        return new HysteriaController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        if (text.StartsWith("hysteria://"))
            return new[] { ParseHyUri(text) };

        throw new FormatException();
    }

    public HysteriaServer ParseHyUri(string text)
    {
        var data = new HysteriaServer();

        // hysteria://host:port?protocol=udp&auth=123456&peer=sni.domain&insecure=1&upmbps=100&downmbps=100&alpn=hysteria&obfs=xplus&obfsParam=123456#remarks
        if (text.Contains('#'))
        {
            data.Remark = Uri.UnescapeDataString(text.Split('#')[1]);
            text = text.Split('#')[0];
        }

        var parameter = HttpUtility.ParseQueryString(text.Split('?')[1]);
        text = text.Substring(0, text.IndexOf("?", StringComparison.Ordinal));

        data.Protocol = parameter.Get("protocol") ?? "udp";
        data.OBFS = parameter.Get("obfsParam") ?? "";
        data.ALPN = parameter.Get("alpn") ?? "";
        data.AuthType = parameter.Get("auth") == null ? "DISABLED" : "STR";
        data.AuthPayload = parameter.Get("auth") ?? "";
        data.ServerName = parameter.Get("peer") ?? "";
        data.Insecure = parameter.Get("insecure") == "1" ? "true" : "false";
        data.UpMbps = Convert.ToInt32(parameter.Get("upmbps"));
        data.DownMbps = Convert.ToInt32(parameter.Get("downmbps"));

        var finder = new Regex(@"^hysteria://(?<server>.+):(?<port>\d+)");
        var match = finder.Match(text);
        if (!match.Success)
            throw new FormatException();

        data.Hostname = match.Groups["server"].Value;
        data.Port = ushort.Parse(match.Groups["port"].Value);
        return data;
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}
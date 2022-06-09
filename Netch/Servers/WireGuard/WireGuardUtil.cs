using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class WireGuardUtil : IServerUtil
{
    public ushort Priority { get; } = 4;

    public string TypeName { get; } = "WireGuard";

    public string FullName { get; } = "WireGuard";

    public string ShortName { get; } = "WG";

    public string[] UriScheme { get; } = { "wireguard" };

    public Type ServerType { get; } = typeof(WireGuardServer);

    public void Edit(Server s)
    {
        new WireGuardForm((WireGuardServer)s).ShowDialog();
    }

    public void Create()
    {
        new WireGuardForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        return V2rayUtils.GetVShareLink(s, "wireguard");
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        return V2rayUtils.ParseVUri(text);
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}
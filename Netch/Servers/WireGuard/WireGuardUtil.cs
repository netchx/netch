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

    public string[] UriScheme { get; } = Array.Empty<string>();

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
        return ShareLink.GetNetchLink(s);
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        throw new NotSupportedException("Use a Netch:// link for WireGuard servers.");
    }

    public static WireGuardServer ParseConfig(string text)
    {
        var interfaceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var peerValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? section = null;
        var peerCount = 0;

        foreach (var sourceLine in text.GetLines())
        {
            var line = sourceLine.Split('#', 2)[0].Trim();
            if (line.Length == 0)
                continue;

            if (line.Equals("[Interface]", StringComparison.OrdinalIgnoreCase))
            {
                section = interfaceValues;
                continue;
            }

            if (line.Equals("[Peer]", StringComparison.OrdinalIgnoreCase))
            {
                if (++peerCount > 1)
                    throw new NotSupportedException("Only single-peer WireGuard profiles are supported.");
                section = peerValues;
                continue;
            }

            var separator = line.IndexOf('=');
            if (section == null || separator < 1)
                throw new FormatException("Invalid WireGuard configuration");

            section[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        if (!interfaceValues.TryGetValue("PrivateKey", out var privateKey) || string.IsNullOrWhiteSpace(privateKey) ||
            !interfaceValues.TryGetValue("Address", out var addresses) || string.IsNullOrWhiteSpace(addresses) ||
            !peerValues.TryGetValue("PublicKey", out var publicKey) || string.IsNullOrWhiteSpace(publicKey) ||
            !peerValues.TryGetValue("Endpoint", out var endpoint) || string.IsNullOrWhiteSpace(endpoint))
            throw new FormatException("WireGuard profile is missing a required field");

        if (peerValues.TryGetValue("AllowedIPs", out var allowedIPs) &&
            !allowedIPs.Split(',').Select(value => value.Trim()).Any(value => value is "0.0.0.0/0" or "::/0"))
            throw new NotSupportedException("Only full-tunnel WireGuard profiles are supported.");

        var uri = ProxyUri.Parse("wireguard://" + endpoint, "wireguard");
        var mtu = 1420;
        if (interfaceValues.TryGetValue("MTU", out var mtuText) && (!int.TryParse(mtuText, out mtu) || mtu <= 0))
            throw new FormatException("Invalid WireGuard MTU");

        return new WireGuardServer
        {
            Hostname = ProxyUri.HostName(uri),
            Port = (ushort)uri.Port,
            PrivateKey = privateKey,
            LocalAddresses = addresses,
            PeerPublicKey = publicKey,
            PreSharedKey = peerValues.GetValueOrDefault("PresharedKey"),
            MTU = mtu,
            Remark = ProxyUri.HostName(uri)
        };
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}

using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public class WireGuardUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 4;

    public string TypeName { get; } = "WireGuard";

    public string FullName { get; } = "WireGuard";

    public string ShortName { get; } = "WG";

    public string[] UriScheme { get; } = { "wireguard", "wg" };

    public Type ServerType { get; } = typeof(WireGuardServer);

    public string GetShareLink(Server s)
    {
        if (s is not WireGuardServer item)
        {
            return string.Empty;
        }

        var remark = string.Empty;
        if (item.Remarks.IsNotEmpty())
        {
            remark = "#" + Utils.Utils.UrlEncode(item.Remarks);
        }

        var dicQuery = new Dictionary<string, string>();
        if (!item.ProtoExtra.WgPublicKey.IsNullOrEmpty())
        {
            dicQuery.Add("publickey", Utils.Utils.UrlEncode(item.ProtoExtra.WgPublicKey));
        }
        if (!item.ProtoExtra.WgReserved.IsNullOrEmpty())
        {
            dicQuery.Add("reserved", Utils.Utils.UrlEncode(item.ProtoExtra.WgReserved));
        }
        if (!item.ProtoExtra.WgPresharedKey.IsNullOrEmpty())
        {
            dicQuery.Add("presharedkey", Utils.Utils.UrlEncode(item.ProtoExtra.WgPresharedKey));
        }
        if (!item.ProtoExtra.WgInterfaceAddress.IsNullOrEmpty())
        {
            dicQuery.Add("address", Utils.Utils.UrlEncode(item.ProtoExtra.WgInterfaceAddress));
        }
        dicQuery.Add("mtu", Utils.Utils.UrlEncode(item.ProtoExtra.WgMtu > 0 ? item.ProtoExtra.WgMtu.ToString() : "1280"));
        return ToUri(EConfigType.WireGuard, item.Address, item.Port, item.Password, dicQuery, remark);
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        WireGuardServer item = new();

        var url = Utils.Utils.TryUri(str);
        if (url == null || url.IdnHost.IsNullOrEmpty() || !ServerValidator.IsValidPort(url.Port))
        {
            return [];
        }

        item.Address = url.IdnHost;
        item.Port = url.Port;
        item.Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
        item.Password = Utils.Utils.UrlDecode(url.UserInfo);

        var query = Utils.Utils.ParseQueryString(url.Query);

        item.ProtoExtra.WgPublicKey = GetFirstQueryValue(query, "publickey", "public-key", "public_key", "pbk");
        item.ProtoExtra.WgPresharedKey = GetFirstQueryValue(query, "presharedkey", "preshared-key", "pre-shared-key", "pre_shared_key", "psk");
        var reservedRaw = GetQueryDecoded(query, "reserved");
        item.ProtoExtra.WgReserved = ServerValidator.TryNormalizeWireGuardReserved(reservedRaw, out var normalizedReserved)
            ? normalizedReserved
            : reservedRaw;
        item.ProtoExtra.WgInterfaceAddress = GetFirstQueryValue(query, "address", "ip", "local_address");
        item.ProtoExtra.WgMtu = int.TryParse(GetQueryDecoded(query, "mtu"), out var mtuVal) && mtuVal > 0 ? mtuVal : 1280;
        return [item];
    }

    public bool CheckServer(Server s)
    {
        var extra = s.ProtoExtra;
        var peerKey = string.IsNullOrWhiteSpace(extra.WgPublicKey) ? s.PublicKey : extra.WgPublicKey;
        return s.Address.IsNotEmpty()
            && ServerValidator.IsValidPort(s.Port)
            && ServerValidator.IsValidWireGuardKey(s.Password)
            && ServerValidator.IsValidWireGuardKey(peerKey)
            && (string.IsNullOrWhiteSpace(extra.WgPresharedKey) || ServerValidator.IsValidWireGuardKey(extra.WgPresharedKey))
            && ServerValidator.IsValidWireGuardAddresses(extra.WgInterfaceAddress)
            && ServerValidator.IsValidWireGuardReserved(extra.WgReserved);
    }
}

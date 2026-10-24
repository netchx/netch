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
        var item = s as WireGuardServer;
        if (item == null)
        {
            return null;
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
        if (!item.ProtoExtra.WgInterfaceAddress.IsNullOrEmpty())
        {
            dicQuery.Add("address", Utils.Utils.UrlEncode(item.ProtoExtra.WgInterfaceAddress));
        }
        dicQuery.Add("mtu", Utils.Utils.UrlEncode(item.ProtoExtra.WgMtu > 0 ? item.ProtoExtra.WgMtu.ToString() : "1280"));
        return ToUri(EConfigType.WireGuard, item.Address, item.Port, item.Password, dicQuery, remark);
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        WireGuardServer item = new();

        var url = Utils.Utils.TryUri(str);
        if (url == null)
        {
            return null;
        }

        item.Address = url.IdnHost;
        item.Port = url.Port;
        item.Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
        item.Password = Utils.Utils.UrlDecode(url.UserInfo);

        var query = Utils.Utils.ParseQueryString(url.Query);

        item.ProtoExtra.WgPublicKey = GetQueryDecoded(query, "publickey");
        item.ProtoExtra.WgReserved = GetQueryDecoded(query, "reserved");
        item.ProtoExtra.WgInterfaceAddress = GetQueryDecoded(query, "address");
        item.ProtoExtra.WgMtu = int.TryParse(GetQueryDecoded(query, "mtu"), out var mtuVal) ? mtuVal : 1280;
        return [item];
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}
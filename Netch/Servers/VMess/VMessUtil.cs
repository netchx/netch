using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public class VMessUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 3;

    public string TypeName { get; } = "VMess";

    public string FullName { get; } = "VMess";

    public string ShortName { get; } = "V2";

    public string[] UriScheme { get; } = { "vmess" };

    public Type ServerType { get; } = typeof(VMessServer);

    public void Edit(Server s)
    {
        new VMessForm((VMessServer)s).ShowDialog();
    }

    public void Create()
    {
        new VMessForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        var item = s as VMessServer;
        if (item == null)
        {
            return null;
        }

        var vmessQRCode = new VmessQRCode
        {
            v = 2,
            ps = item.Remarks.TrimEx(),
            add = item.Address,
            port = (ushort)item.Port,
            id = item.Password,
            aid = item.ProtoExtra.AlterId ?? 0,
            scy = item.ProtoExtra.VmessSecurity ?? "",
            net = item.Network,
            type = item.HeaderType,
            host = item.RequestHost,
            path = item.Path,
            tls = item.StreamSecurity,
            sni = item.Sni,
            alpn = item.Alpn,
            fp = item.Fingerprint,
            insecure = item.AllowInsecure == true ? "1" : "0"
        };

        var url = JsonUtils.Serialize(vmessQRCode);
        url = Utils.Utils.Base64Encode(url);
        url = $"{Constants.ProtocolShares[EConfigType.VMess]}{url}";

        return url;
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        VMessServer? item;
        if (str.IndexOf('@') > 0)
        {
            item = ResolveStdVmess(str) ?? ResolveVmess(str);
        }
        else
        {
            item = ResolveVmess(str);
        }

        return new[] { item };
    }

    public bool CheckServer(Server s)
    {
        return true;
    }

    private static VMessServer? ResolveVmess(string result)
    {
        var item = new VMessServer();

        result = result[Constants.ProtocolShares[EConfigType.VMess].Length..];
        result = Utils.Utils.Base64Decode(result);

        var vmessQRCode = JsonUtils.Deserialize<VmessQRCode>(result);
        if (vmessQRCode == null)
        {
            return null;
        }

        item.Network = Constants.DefaultNetwork;
        item.HeaderType = Constants.None;

        //item.ConfigVersion = vmessQRCode.v;
        item.Remarks = Utils.Utils.ToString(vmessQRCode.ps);
        item.Address = Utils.Utils.ToString(vmessQRCode.add);
        item.Port = vmessQRCode.port;
        item.Password = Utils.Utils.ToString(vmessQRCode.id);
        item.ProtoExtra.AlterId = vmessQRCode.aid;
        item.ProtoExtra.VmessSecurity = vmessQRCode.scy.IsNullOrEmpty() ? Constants.DefaultSecurity : vmessQRCode.scy;
        if (vmessQRCode.net.IsNotEmpty())
        {
            item.Network = vmessQRCode.net;
        }
        if (vmessQRCode.type.IsNotEmpty())
        {
            item.HeaderType = vmessQRCode.type;
        }

        item.RequestHost = Utils.Utils.ToString(vmessQRCode.host);
        item.Path = Utils.Utils.ToString(vmessQRCode.path);
        item.StreamSecurity = Utils.Utils.ToString(vmessQRCode.tls);
        item.Sni = Utils.Utils.ToString(vmessQRCode.sni);
        item.Alpn = Utils.Utils.ToString(vmessQRCode.alpn);
        item.Fingerprint = Utils.Utils.ToString(vmessQRCode.fp);
        item.AllowInsecure = vmessQRCode.insecure == "1" ? true : null;

        return item;
    }

    public static VMessServer? ResolveStdVmess(string str)
    {
        var item = new VMessServer();

        var url = Utils.Utils.TryUri(str);
        if (url == null)
        {
            return null;
        }

        item.Address = url.IdnHost;
        item.Port = url.Port;
        item.Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
        item.Password = Utils.Utils.UrlDecode(url.UserInfo);

        item.ProtoExtra.VmessSecurity = "auto";

        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveUriQuery(query, ref item);

        return item;
    }
}
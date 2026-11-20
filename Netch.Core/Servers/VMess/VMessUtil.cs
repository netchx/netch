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
            insecure = item.AllowInsecure == true ? "1" : "0",
            extra = item.Extra.IsNotEmpty() ? item.Extra : null,
            pbk = item.PublicKey.IsNotEmpty() ? item.PublicKey : null,
            sid = item.ShortId.IsNotEmpty() ? item.ShortId : null,
            spx = item.SpiderX.IsNotEmpty() ? item.SpiderX : null,
            pqv = item.Mldsa65Verify.IsNotEmpty() ? item.Mldsa65Verify : null,
            ech = item.EchConfigList.IsNotEmpty() ? item.EchConfigList : null,
            echForceQuery = item.EchForceQuery.IsNotEmpty() ? item.EchForceQuery : null,
            pcs = item.CertSha.IsNotEmpty() ? item.CertSha : null
        };

        var url = JsonUtils.Serialize(vmessQRCode);
        url = Utils.Utils.Base64Encode(url);
        url = $"{Constants.ProtocolShares[EConfigType.VMess]}{url}";

        return url;
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

        return item != null ? [item] : [];
    }

    public bool CheckServer(Server s)
    {
        return s.Address.IsNotEmpty() && ServerValidator.IsValidPort(s.Port) && s.Password.IsNotEmpty();
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

        item.Remarks = Utils.Utils.ToString(vmessQRCode.ps);
        item.Address = Utils.Utils.ToString(vmessQRCode.add).Trim('[', ']');
        item.Port = vmessQRCode.port;
        item.Password = Utils.Utils.ToString(vmessQRCode.id);
        if (item.Address.IsNullOrEmpty() || !ServerValidator.IsValidPort(item.Port) || item.Password.IsNullOrEmpty())
        {
            return null;
        }

        item.ProtoExtra.AlterId = Math.Max(vmessQRCode.aid, 0);
        item.ProtoExtra.VmessSecurity = vmessQRCode.scy.IsNullOrEmpty() ? Constants.DefaultSecurity : vmessQRCode.scy;
        if (vmessQRCode.net.IsNotEmpty())
        {
            item.Network = vmessQRCode.net.Trim().ToLowerInvariant() switch
            {
                "http" => nameof(ETransport.h2),
                "raw" => nameof(ETransport.tcp),
                "splithttp" => nameof(ETransport.xhttp),
                var net => net
            };
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
        item.AllowInsecure = ParseBoolFlag(vmessQRCode.insecure);
        item.Extra = Utils.Utils.ToString(vmessQRCode.extra);
        item.PublicKey = Utils.Utils.ToString(vmessQRCode.pbk);
        item.ShortId = Utils.Utils.ToString(vmessQRCode.sid);
        item.SpiderX = Utils.Utils.ToString(vmessQRCode.spx);
        item.Mldsa65Verify = Utils.Utils.ToString(vmessQRCode.pqv);
        item.EchConfigList = Utils.Utils.ToString(vmessQRCode.ech);
        item.EchForceQuery = Utils.Utils.ToString(vmessQRCode.echForceQuery);
        item.CertSha = Utils.Utils.ToString(vmessQRCode.pcs);

        AddServerCommon(item);
        return item;
    }

    public static VMessServer? ResolveStdVmess(string str)
    {
        var item = new VMessServer();

        var url = Utils.Utils.TryUri(str);
        if (url == null || url.IdnHost.IsNullOrEmpty() || !ServerValidator.IsValidPort(url.Port))
        {
            return null;
        }

        item.Address = url.IdnHost;
        item.Port = url.Port;
        item.Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
        var (user, pass, hasSeparator) = SplitUserInfo(url.UserInfo);
        if (hasSeparator)
        {
            item.ProtoExtra.VmessSecurity = user;
            item.Password = pass;
        }
        else
        {
            item.Password = user;
        }

        var query = Utils.Utils.ParseQueryString(url.Query);
        if (item.ProtoExtra.VmessSecurity.IsNullOrEmpty())
        {
            item.ProtoExtra.VmessSecurity = GetFirstQueryValue(query, "scy", "encryption", "cipher") is { Length: > 0 } sec
                ? sec
                : Constants.DefaultSecurity;
        }
        if (int.TryParse(GetFirstQueryValue(query, "aid", "alterId"), out var aid) && aid >= 0)
        {
            item.ProtoExtra.AlterId = aid;
        }

        ResolveUriQuery(query, ref item);

        return item;
    }
}
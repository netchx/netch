using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;
//https://v2.hysteria.network/zh/docs/developers/URI-Scheme/
public class Hysteria2Util : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 3;

    public string TypeName { get; } = "Hysteria2";

    public string FullName { get; } = "Hysteria2";

    public string ShortName { get; } = "HY2";

    public string[] UriScheme { get; } = { "hysteria2", "hy2" };

    public Type ServerType { get; } = typeof(Hysteria2Server);

    public void Edit(Server s)
    {
        new Hysteria2Form((Hysteria2Server)s).ShowDialog();
    }

    public void Create()
    {
        new Hysteria2Form().ShowDialog();
    }

    //hysteria2://letmein@example.com:123,5000-6000/?insecure=1&obfs=salamander&obfs-password=gawrgura&pinSHA256=deadbeef&sni=real.example.com
    //hysteria2://[auth@]hostname[:port]/?[key=value]&[key=value]...
    public string GetShareLink(Server s)
    {

        var item = (Hysteria2Server)s;

        if (item == null)
        {
            return null;
        }

        var url = string.Empty;

        var remark = string.Empty;
        if (item.Remarks.IsNotEmpty())
        {
            remark = "#" + Utils.Utils.UrlEncode(item.Remarks);
        }
        var dicQuery = new Dictionary<string, string>();
        ToUriQueryLite(item, ref dicQuery);
        var protocolExtraItem = item.ProtoExtra;

        if (!protocolExtraItem.SalamanderPass.IsNullOrEmpty())
        {
            dicQuery.Add("obfs", "salamander");
            dicQuery.Add("obfs-password", Utils.Utils.UrlEncode(protocolExtraItem.SalamanderPass));
        }
        if (!protocolExtraItem.Ports.IsNullOrEmpty())
        {
            dicQuery.Add("mport", Utils.Utils.UrlEncode(protocolExtraItem.Ports.Replace(':', '-')));
        }
        if (!item.CertSha.IsNullOrEmpty())
        {
            var sha = item.CertSha;
            var idx = sha.IndexOf('~');
            if (idx > 0)
            {
                sha = sha[..idx];
            }
            dicQuery.Add("pinSHA256", Utils.Utils.UrlEncode(sha));
        }

        return ToUri(EConfigType.Hysteria2, item.Address, item.Port, item.Password, dicQuery, remark);
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var server = new Hysteria2Server();

        var url = Utils.Utils.TryUri(text);
        if (url == null)
        {
            return null;
        }

        server.Address = url.IdnHost;
        server.Port = url.Port > 0 ? url.Port : 443;
        server.Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
        server.Password = Utils.Utils.UrlDecode(url.UserInfo);

        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveUriQuery(query, ref server);
        if (server.CertSha.IsNullOrEmpty())
        {
            server.CertSha = GetQueryDecoded(query, "pinSHA256");
        }
        if (server.StreamSecurity.IsNullOrWhiteSpace())
        {
            server.StreamSecurity = Constants.StreamSecurity;
        }
        server.ProtoExtra.Ports = GetQueryDecoded(query, "mport");
        server.ProtoExtra.SalamanderPass = GetQueryDecoded(query, "obfs-password");

        return new[] { server };
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}

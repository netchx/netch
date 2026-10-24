using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public class TrojanUtil : ServerUtilBase, IServerUtil
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
        var item = (TrojanServer)s;
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
        if (!item.ProtoExtra.Flow.IsNullOrWhiteSpace())
        {
            dicQuery.Add("flow", item.ProtoExtra.Flow);
        }
        ToUriQuery(item, null, ref dicQuery);

        return ToUri(EConfigType.Trojan, item.Address, item.Port, item.Password, dicQuery, remark);
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        TrojanServer item = new();

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
        item.ProtoExtra.Flow = GetQueryValue(query, "flow");
        ResolveUriQuery(query, ref item);
        if (item.StreamSecurity.IsNullOrEmpty())
        {
            item.StreamSecurity = Constants.StreamSecurity;
        }
        return new[] { item };
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}
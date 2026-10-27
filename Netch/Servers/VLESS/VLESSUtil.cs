using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public class VLESSUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 2;

    public string TypeName { get; } = "VLESS";

    public string FullName { get; } = "VLESS";

    public string ShortName { get; } = "VL";

    public string[] UriScheme { get; } = { "vless" };

    public Type ServerType { get; } = typeof(VLESSServer);

    public void Edit(Server s)
    {
        new VLESSForm((VLESSServer)s).ShowDialog();
    }

    public void Create()
    {
        new VLESSForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {

        var item = (VLESSServer)s;

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
        dicQuery.Add("encryption", !item.ProtoExtra.VlessEncryption.IsNullOrWhiteSpace() ? item.ProtoExtra.VlessEncryption : Constants.None);
        if (!item.ProtoExtra.Flow.IsNullOrWhiteSpace())
        {
            dicQuery.Add("flow", item.ProtoExtra.Flow);
        }
        ToUriQuery(item, Constants.None, ref dicQuery);

        return ToUri(EConfigType.VLESS, item.Address, item.Port, item.Password, dicQuery, remark);
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        VLESSServer item = new();

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
        item.ProtoExtra.VlessEncryption = GetQueryValue(query, "encryption", Constants.None);
        item.ProtoExtra.Flow = GetQueryValue(query, "flow");
        item.StreamSecurity = GetQueryValue(query, "security");
        ResolveUriQuery(query, ref item);
        return [item];
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}
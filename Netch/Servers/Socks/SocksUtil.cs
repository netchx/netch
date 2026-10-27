using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;

namespace Netch.Servers;

public class SocksUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 0;

    public string TypeName { get; } = "SOCKS";

    public string FullName { get; } = "SOCKS";

    public string ShortName { get; } = "SOCKS";

    public string[] UriScheme { get; } = { "socks", "socks5", "socks4" };

    public Type ServerType { get; } = typeof(SocksServer);

    public void Edit(Server s)
    {
        new SocksForm((SocksServer)s).ShowDialog();
    }

    public void Create()
    {
        new SocksForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        var item = (SocksServer)s;
        if (item == null)
        {
            return null;
        }
        var remark = string.Empty;
        if (item.Remarks.IsNotEmpty())
        {
            remark = "#" + Utils.Utils.UrlEncode(item.Remarks);
        }
        //new
        var pw = Utils.Utils.Base64Encode($"{item.Username}:{item.Password}", true);
        return ToUri(EConfigType.SOCKS, item.Address, item.Port, pw, null, remark);
    }

    public IServerController GetController()
    {
        return new SocksController();
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        var item = ResolveSocksNew(str) ?? ResolveSocks(str);
        if (item == null)
        {
            return null;
        }
        if (item.Address.Length == 0 || item.Port == 0)
        {
            return null;
        }

        return [item];
    }

    public bool CheckServer(Server s)
    {
        return true;
    }

    private static SocksServer? ResolveSocks(string result)
    {
        SocksServer item = new();

        result = result[Constants.ProtocolShares[EConfigType.SOCKS].Length..];
        //remark
        var indexRemark = result.IndexOf('#');
        if (indexRemark > 0)
        {
            try
            {
                item.Remarks = Utils.Utils.UrlDecode(result.Substring(indexRemark + 1));
            }
            catch { }
            result = result[..indexRemark];
        }
        //part decode
        var indexS = result.IndexOf('@');
        if (indexS > 0)
        {
        }
        else
        {
            result = Utils.Utils.Base64Decode(result);
        }

        var arr1 = result.Split('@');
        if (arr1.Length != 2)
        {
            return null;
        }
        var arr21 = arr1.First().Split(':');
        var indexPort = arr1.Last().LastIndexOf(":");
        if (arr21.Length != 2 || indexPort < 0)
        {
            return null;
        }
        item.Address = arr1[1][..indexPort];
        item.Port = arr1[1][(indexPort + 1)..].ToInt();
        item.Username = arr21.First();
        item.Password = arr21[1];
        return item;
    }

    private static SocksServer? ResolveSocksNew(string result)
    {
        var parsedUrl = Utils.Utils.TryUri(result);
        if (parsedUrl == null)
        {
            return null;
        }

        SocksServer item = new()
        {
            Remarks = parsedUrl.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            Address = parsedUrl.IdnHost,
            Port = parsedUrl.Port,
        };
        // parse base64 UserInfo
        var rawUserInfo = Utils.Utils.UrlDecode(parsedUrl.UserInfo);
        var userInfo = rawUserInfo.Contains(':') ? rawUserInfo : Utils.Utils.Base64Decode(rawUserInfo);
        var userInfoParts = userInfo.Split([':'], 2);
        if (userInfoParts.Length == 2)
        {
            item.Username = userInfoParts.First();
            item.Password = userInfoParts[1];
        }

        return item;
    }
}

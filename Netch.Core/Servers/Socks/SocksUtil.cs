using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public class SocksUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 0;

    public string TypeName { get; } = "SOCKS";

    public string FullName { get; } = "SOCKS";

    public string ShortName { get; } = "SOCKS";

    public string[] UriScheme { get; } = { "socks", "socks5", "socks5h" };

    public Type ServerType { get; } = typeof(SocksServer);

    public string GetShareLink(Server s)
    {
        if (s is not SocksServer item)
        {
            return string.Empty;
        }
        var remark = item.Remarks.IsNotEmpty()
            ? "#" + Utils.Utils.UrlEncode(item.Remarks)
            : string.Empty;
        var pw = item.Username.IsNotEmpty() || item.Password.IsNotEmpty()
            ? Utils.Utils.Base64Encode($"{item.Username}:{item.Password}", true)
            : string.Empty;
        Dictionary<string, string>? dicQuery = item.ProtoExtra.Uot == true
            ? new() { ["uot"] = "1" }
            : null;
        return ToUri(EConfigType.SOCKS, item.Address, item.Port, pw, dicQuery, remark);
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        if (str.StartsWith("tg://socks?", StringComparison.OrdinalIgnoreCase) ||
            str.StartsWith("https://t.me/socks?", StringComparison.OrdinalIgnoreCase))
        {
            var tg = ResolveTelegramSocks(str);
            return tg != null && CheckServer(tg) ? [tg] : [];
        }

        var item = ResolveSocksNew(str) ?? ResolveSocks(str);
        if (item == null || item.Address.IsNullOrEmpty() || !ServerValidator.IsValidPort(item.Port))
        {
            return [];
        }

        return [item];
    }

    public bool CheckServer(Server s)
    {
        return s.Address.IsNotEmpty()
            && ServerValidator.IsValidPort(s.Port)
            && string.IsNullOrWhiteSpace(s.Username) == string.IsNullOrWhiteSpace(s.Password);
    }

    private static SocksServer? ResolveTelegramSocks(string str)
    {
        var url = Utils.Utils.TryUri(str);
        if (url == null)
            return null;

        var query = Utils.Utils.ParseQueryString(url.Query);
        var server = GetQueryValue(query, "server");
        if (string.IsNullOrWhiteSpace(server) ||
            !int.TryParse(GetQueryValue(query, "port"), out var port) ||
            !ServerValidator.IsValidPort(port))
        {
            return null;
        }

        return new SocksServer
        {
            Address = server.Trim('[', ']'),
            Port = port,
            Username = GetQueryValue(query, "user"),
            Password = GetQueryValue(query, "pass"),
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            ProtoExtra = { Uot = ParseBoolFlag(GetFirstQueryValue(query, "uot", "udp-over-tcp", "udp_over_tcp")) }
        };
    }

    private static SocksServer? ResolveSocks(string result)
    {
        var schemeIndex = result.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex < 0)
            return null;

        SocksServer item = new();
        result = result[(schemeIndex + 3)..];
        //remark
        var indexRemark = result.IndexOf('#');
        if (indexRemark > 0)
        {
            item.Remarks = Utils.Utils.UrlDecode(result.Substring(indexRemark + 1));
            result = result[..indexRemark];
        }
        //part decode
        var indexS = result.IndexOf('@');
        if (indexS <= 0)
        {
            result = Utils.Utils.Base64Decode(result);
        }

        var arr1 = result.Split('@');
        if (arr1.Length != 2)
        {
            return null;
        }
        var arr21 = arr1.First().Split(':', 2);
        var indexPort = arr1.Last().LastIndexOf(':');
        if (arr21.Length != 2 || indexPort < 0)
        {
            return null;
        }
        item.Address = arr1[1][..indexPort].Trim('[', ']');
        item.Port = arr1[1][(indexPort + 1)..].ToInt();
        item.Username = arr21.First();
        item.Password = arr21[1];
        return item;
    }

    private static SocksServer? ResolveSocksNew(string result)
    {
        var parsedUrl = Utils.Utils.TryUri(result);
        if (parsedUrl == null || parsedUrl.IdnHost.IsNullOrEmpty() || !ServerValidator.IsValidPort(parsedUrl.Port))
        {
            return null;
        }

        SocksServer item = new()
        {
            Remarks = parsedUrl.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            Address = parsedUrl.IdnHost,
            Port = parsedUrl.Port,
        };
        if (parsedUrl.UserInfo.IsNotEmpty())
        {
            if (parsedUrl.UserInfo.Contains(':'))
            {
                var (user, pass, _) = SplitUserInfo(parsedUrl.UserInfo);
                item.Username = user;
                item.Password = pass;
            }
            else
            {
                var decodedUserInfo = Utils.Utils.UrlDecode(parsedUrl.UserInfo);
                var userInfo = decodedUserInfo.Contains(':')
                    ? decodedUserInfo
                    : Utils.Utils.Base64Decode(decodedUserInfo);
                var userInfoParts = userInfo.Split([':'], 2);
                if (userInfoParts.Length == 2)
                {
                    item.Username = userInfoParts[0];
                    item.Password = userInfoParts[1];
                }
                else
                {
                    return null;
                }
            }
        }

        var query = Utils.Utils.ParseQueryString(parsedUrl.Query);
        item.ProtoExtra.Uot = ParseBoolFlag(GetFirstQueryValue(query, "uot", "udp-over-tcp", "udp_over_tcp"));

        return item;
    }
}

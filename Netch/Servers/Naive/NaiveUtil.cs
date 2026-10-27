using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class NaiveUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 7;
    public string TypeName { get; } = nameof(EConfigType.Naive);
    public string FullName { get; } = "NaiveProxy";
    public string ShortName { get; } = "Naive";
    public string[] UriScheme { get; } = ["naive", "naive+https", "naive+quic"];
    public Type ServerType { get; } = typeof(NaiveServer);

    public void Edit(Server s) => new SingboxServerForm(s, EConfigType.Naive).ShowDialog();
    public void Create() => new SingboxServerForm(null, EConfigType.Naive).ShowDialog();

    public string GetShareLink(Server s)
    {
        var query = TlsQuery(s);
        if (s.ProtoExtra.InsecureConcurrency is > 0)
            query["insecure-concurrency"] = s.ProtoExtra.InsecureConcurrency.Value.ToString();
        var userInfo = s.Username.IsNotEmpty()
            ? $"{Utils.Utils.UrlEncode(s.Username)}:{Utils.Utils.UrlEncode(s.Password)}"
            : Utils.Utils.UrlEncode(s.Password);
        var scheme = s.ProtoExtra.NaiveQuic == true ? "naive+quic://" : "naive+https://";
        var queryText = query.Count == 0 ? string.Empty : $"?{string.Join("&", query.Select(p => $"{p.Key}={p.Value}"))}";
        var remark = s.Remarks.IsNullOrEmpty() ? string.Empty : $"#{Utils.Utils.UrlEncode(s.Remarks)}";
        return $"{scheme}{userInfo}@{GetIpv6(s.Address)}:{s.Port}{queryText}{remark}";
    }

    public IServerController GetController() => new SingboxController();

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null) return [];
        var userInfo = Utils.Utils.UrlDecode(url.UserInfo);
        var server = new NaiveServer()
        {
            Address = url.IdnHost,
            Port = url.Port,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            StreamSecurity = Constants.StreamSecurity
        };
        if (url.Scheme.Contains("quic", StringComparison.OrdinalIgnoreCase)) server.ProtoExtra.NaiveQuic = true;
        if (userInfo.Contains(':'))
        {
            var split = userInfo.Split(':', 2);
            server.Username = split[0];
            server.Password = split[1];
        }
        else
        {
            server.Password = userInfo;
        }
        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQuery(query, server);
        if (int.TryParse(GetQueryValue(query, "insecure-concurrency"), out var concurrency) && concurrency > 0)
            server.ProtoExtra.InsecureConcurrency = concurrency;
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty() && s.Port > 0 && s.Password.IsNotEmpty();
}

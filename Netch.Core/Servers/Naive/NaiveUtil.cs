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

    public string GetShareLink(Server s)
    {
        var query = TlsQuery(s);
        if (s.ProtoExtra.InsecureConcurrency is > 0)
            query["insecure-concurrency"] = s.ProtoExtra.InsecureConcurrency.Value.ToString();
        if (s.ProtoExtra.NaiveQuic == true && s.ProtoExtra.CongestionControl.IsNotEmpty())
            query["quic-congestion-control"] = Utils.Utils.UrlEncode(s.ProtoExtra.CongestionControl);
        if (s.ProtoExtra.Uot == true)
            query["uot"] = "1";
        var userInfo = s.Username.IsNotEmpty()
            ? $"{Utils.Utils.UrlEncode(s.Username)}:{Utils.Utils.UrlEncode(s.Password)}"
            : Utils.Utils.UrlEncode(s.Password);
        var scheme = s.ProtoExtra.NaiveQuic == true ? "naive+quic://" : "naive+https://";
        var queryText = query.Count == 0 ? string.Empty : $"?{string.Join("&", query.Select(p => $"{p.Key}={p.Value}"))}";
        var remark = s.Remarks.IsNullOrEmpty() ? string.Empty : $"#{Utils.Utils.UrlEncode(s.Remarks)}";
        return $"{scheme}{userInfo}@{GetIpv6(s.Address)}:{s.Port}{queryText}{remark}";
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null || url.IdnHost.IsNullOrEmpty()) return [];
        var (user, pass, hasSeparator) = SplitUserInfo(url.UserInfo);
        var server = new NaiveServer()
        {
            Address = url.IdnHost,
            Port = url.Port > 0 ? url.Port : 443,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            StreamSecurity = Constants.StreamSecurity
        };
        if (hasSeparator)
        {
            server.Username = user;
            server.Password = pass;
        }
        else
        {
            server.Password = user;
        }
        var query = Utils.Utils.ParseQueryString(url.Query);
        if (url.Scheme.Contains("quic", StringComparison.OrdinalIgnoreCase) || ParseBoolFlag(GetQueryValue(query, "quic")) == true)
            server.ProtoExtra.NaiveQuic = true;
        ResolveTlsQuery(query, server);
        if (int.TryParse(GetFirstQueryValue(query, "insecure-concurrency", "insecure_concurrency"), out var concurrency) && concurrency > 0)
            server.ProtoExtra.InsecureConcurrency = concurrency;
        server.ProtoExtra.CongestionControl = GetFirstQueryValue(query, "quic-congestion-control", "quic_congestion_control", "congestion_control", "congestion-control");
        server.ProtoExtra.Uot = ParseBoolFlag(GetFirstQueryValue(query, "uot", "udp-over-tcp", "udp_over_tcp"));
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty() && ServerValidator.IsValidPort(s.Port) && s.Password.IsNotEmpty();
}

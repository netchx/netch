using System.Globalization;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class AnyTLSUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 6;
    public string TypeName { get; } = nameof(EConfigType.AnyTLS);
    public string FullName { get; } = "AnyTLS";
    public string ShortName { get; } = "AnyTLS";
    public string[] UriScheme { get; } = ["anytls"];
    public Type ServerType { get; } = typeof(AnyTLSServer);

    public string GetShareLink(Server s)
    {
        var query = TlsQuery(s);
        if (s.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds is { } checkInterval and >= 0)
            query["idle-session-check-interval"] = checkInterval.ToString(CultureInfo.InvariantCulture);
        if (s.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds is { } timeout and >= 0)
            query["idle-session-timeout"] = timeout.ToString(CultureInfo.InvariantCulture);
        if (s.ProtoExtra.AnyTlsMinIdleSession is { } minIdle and >= 0)
            query["min-idle-session"] = minIdle.ToString(CultureInfo.InvariantCulture);
        return ToShareUri(EConfigType.AnyTLS, s, Utils.Utils.UrlEncode(s.Password), query);
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null || url.IdnHost.IsNullOrEmpty()) return [];
        var server = new AnyTLSServer()
        {
            Address = url.IdnHost,
            Port = url.Port > 0 ? url.Port : 443,
            Password = Utils.Utils.UrlDecode(url.UserInfo),
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped)
        };
        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQuery(query, server);
        server.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds = ParseSeconds(GetFirstQueryValue(query, "idle-session-check-interval", "idle_session_check_interval"));
        server.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds = ParseSeconds(GetFirstQueryValue(query, "idle-session-timeout", "idle_session_timeout"));
        server.ProtoExtra.AnyTlsMinIdleSession = ParseSeconds(GetFirstQueryValue(query, "min-idle-session", "min_idle_session"));
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty()
        && ServerValidator.IsValidPort(s.Port)
        && s.Password.IsNotEmpty()
        && s.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds is null or >= 0
        && s.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds is null or >= 0
        && s.ProtoExtra.AnyTlsMinIdleSession is null or >= 0;

    private static int? ParseSeconds(string value) =>
        int.TryParse(value.Trim().TrimEnd('s', 'S'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0
            ? parsed
            : null;
}

using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class TUICUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 5;
    public string TypeName { get; } = nameof(EConfigType.TUIC);
    public string FullName { get; } = "TUIC";
    public string ShortName { get; } = "TUIC";
    public string[] UriScheme { get; } = ["tuic"];
    public Type ServerType { get; } = typeof(TUICServer);

    public string GetShareLink(Server s)
    {
        var query = TlsQuery(s);
        if (s.AllowInsecure == true) query["allow_insecure"] = "1";
        if (s.ProtoExtra.CongestionControl.IsNotEmpty()) query["congestion_control"] = Utils.Utils.UrlEncode(s.ProtoExtra.CongestionControl);
        if (s.ProtoExtra.TuicUdpRelayMode.IsNotEmpty()) query["udp_relay_mode"] = Utils.Utils.UrlEncode(s.ProtoExtra.TuicUdpRelayMode);
        if (s.ProtoExtra.TuicReduceRtt == true) query["reduce_rtt"] = "1";
        if (s.ProtoExtra.TuicDisableSni == true) query["disable_sni"] = "1";
        return ToShareUri(EConfigType.TUIC, s, $"{Utils.Utils.UrlEncode(s.Username)}:{Utils.Utils.UrlEncode(s.Password)}", query);
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null || url.IdnHost.IsNullOrEmpty() || !ServerValidator.IsValidPort(url.Port)) return [];

        var server = new TUICServer()
        {
            Address = url.IdnHost,
            Port = url.Port,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped)
        };
        var (user, pass, hasSeparator) = SplitUserInfo(url.UserInfo);
        if (!hasSeparator)
        {
            var split = user.Split(':', 2);
            if (split.Length != 2) return [];
            user = split[0];
            pass = split[1];
        }
        if (user.IsNullOrEmpty()) return [];
        server.Username = user;
        server.Password = pass;
        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQuery(query, server);
        server.ProtoExtra.CongestionControl = GetFirstQueryValue(query, "congestion_control", "congestion-control");
        var udpRelayMode = GetFirstQueryValue(query, "udp_relay_mode", "udp-relay-mode");
        if (udpRelayMode.Equals("native", StringComparison.OrdinalIgnoreCase) || udpRelayMode.Equals("quic", StringComparison.OrdinalIgnoreCase))
            server.ProtoExtra.TuicUdpRelayMode = udpRelayMode.ToLowerInvariant();
        server.ProtoExtra.TuicReduceRtt = ParseBoolFlag(GetFirstQueryValue(query, "reduce_rtt", "reduce-rtt", "zero_rtt_handshake"));
        server.ProtoExtra.TuicDisableSni = ParseBoolFlag(GetFirstQueryValue(query, "disable_sni", "disable-sni"));
        return [server];
    }

    public bool CheckServer(Server s) => s.Address.IsNotEmpty()
        && ServerValidator.IsValidPort(s.Port)
        && Guid.TryParse(s.Username, out var id)
        && id != Guid.Empty
        && s.Password.IsNotEmpty();
}

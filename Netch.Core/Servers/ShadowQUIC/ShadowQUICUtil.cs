using System.Globalization;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class ShadowQUICUtil : SingboxUtilBase, IServerUtil
{
    public ushort Priority { get; } = 13;
    public string TypeName { get; } = nameof(EConfigType.ShadowQUIC);
    public string FullName { get; } = "ShadowQUIC";
    public string ShortName { get; } = "SQ";
    public string[] UriScheme { get; } = ["shadowquic"];
    public Type ServerType { get; } = typeof(ShadowQUICServer);

    public string GetShareLink(Server s)
    {
        if (s is not ShadowQUICServer server)
            return string.Empty;

        var extra = server.ProtoExtra;
        var query = new Dictionary<string, string>();
        if (server.Sni.IsNotEmpty())
            query["sni"] = Utils.Utils.UrlEncode(server.Sni);
        if (server.Alpn.IsNotEmpty())
            query["alpn"] = Utils.Utils.UrlEncode(server.Alpn);
        if (extra.ShadowQuicVersions.IsNotEmpty())
            query["quic-versions"] = Utils.Utils.UrlEncode(extra.ShadowQuicVersions);
        if (extra.ShadowQuicUdpOverStream == true)
            query["udp-over-stream"] = "1";
        if (extra.ShadowQuicZeroRtt == true)
            query["zero-rtt"] = "1";
        if (extra.ShadowQuicKeepAliveInterval is > 0)
            query["keep-alive-interval"] = extra.ShadowQuicKeepAliveInterval.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.CongestionControl.IsNotEmpty())
            query["congestion-controller"] = Utils.Utils.UrlEncode(extra.CongestionControl);
        if (extra.BbrProfile.IsNotEmpty())
            query["bbr-profile"] = Utils.Utils.UrlEncode(extra.BbrProfile);
        if (extra.UpMbps is > 0)
            query["up"] = extra.UpMbps.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.DownMbps is > 0)
            query["down"] = extra.DownMbps.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.ShadowQuicCwnd is > 0)
            query["cwnd"] = extra.ShadowQuicCwnd.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.ShadowQuicMaxDatagramFrameSize is > 0)
            query["max-datagram-frame-size"] = extra.ShadowQuicMaxDatagramFrameSize.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.ShadowQuicMaxOpenStreams is > 0)
            query["max-open-streams"] = extra.ShadowQuicMaxOpenStreams.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.ShadowQuicRecvWindowConn is > 0)
            query["recv-window-conn"] = extra.ShadowQuicRecvWindowConn.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.ShadowQuicRecvWindow is > 0)
            query["recv-window"] = extra.ShadowQuicRecvWindow.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.ShadowQuicDisableMtuDiscovery == true)
            query["disable-mtu-discovery"] = "1";

        var userInfo = $"{Utils.Utils.UrlEncode(server.Username ?? string.Empty)}:{Utils.Utils.UrlEncode(server.Password ?? string.Empty)}";
        return ToShareUri(EConfigType.ShadowQUIC, server, userInfo, query);
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null || url.IdnHost.IsNullOrEmpty())
            return [];

        var (user, pass, hasSeparator) = SplitUserInfo(url.UserInfo);
        if (!hasSeparator || user.IsNullOrEmpty() || pass.IsNullOrEmpty())
            return [];

        var query = Utils.Utils.ParseQueryString(url.Query);
        var server = new ShadowQUICServer
        {
            Address = url.IdnHost,
            Port = url.Port < 0 ? 443 : url.Port,
            Username = user,
            Password = pass,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            Sni = GetFirstQueryValue(query, "sni", "servername"),
            Alpn = GetQueryDecoded(query, "alpn")
        };

        var extra = server.ProtoExtra;
        var versionsRaw = GetFirstQueryValue(query, "quic-versions", "quic_versions");
        extra.ShadowQuicVersions = versionsRaw.IsNotEmpty()
            ? string.Join(",", versionsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(v => v.ToLowerInvariant()))
            : string.Empty;
        extra.ShadowQuicUdpOverStream = ParseBoolFlag(GetFirstQueryValue(query, "udp-over-stream", "udp_over_stream"));
        extra.ShadowQuicZeroRtt = ParseBoolFlag(GetFirstQueryValue(query, "zero-rtt", "zero_rtt"));
        if (!TryParseOptionalInt(GetFirstQueryValue(query, "keep-alive-interval", "keep_alive_interval"), out var keepAliveInterval))
            return [];
        extra.ShadowQuicKeepAliveInterval = keepAliveInterval;
        var cc = GetFirstQueryValue(query, "congestion-controller", "congestion_controller", "congestion_control", "congestion-control");
        extra.CongestionControl = cc.IsNotEmpty() ? cc.Trim().ToLowerInvariant() : string.Empty;
        var bbr = GetFirstQueryValue(query, "bbr-profile", "bbr_profile");
        extra.BbrProfile = bbr.IsNotEmpty() ? bbr.Trim().ToLowerInvariant() : string.Empty;

        var upRaw = GetFirstQueryValue(query, "up", "upmbps", "up_mbps");
        var downRaw = GetFirstQueryValue(query, "down", "downmbps", "down_mbps");
        var upMbps = Hysteria2Util.TryParseMbps(upRaw);
        var downMbps = Hysteria2Util.TryParseMbps(downRaw);
        if ((!string.IsNullOrWhiteSpace(upRaw) && !upMbps.HasValue) ||
            (!string.IsNullOrWhiteSpace(downRaw) && !downMbps.HasValue))
        {
            return [];
        }
        extra.UpMbps = upMbps;
        extra.DownMbps = downMbps;

        if (!TryParseOptionalInt(GetQueryDecoded(query, "cwnd"), out var cwnd) ||
            !TryParseOptionalInt(GetFirstQueryValue(query, "max-datagram-frame-size", "max_datagram_frame_size"), out var maxDatagramFrameSize) ||
            !TryParseOptionalInt(GetFirstQueryValue(query, "max-open-streams", "max_open_streams"), out var maxOpenStreams) ||
            !TryParseOptionalInt(GetFirstQueryValue(query, "recv-window-conn", "recv_window_conn"), out var recvWindowConn) ||
            !TryParseOptionalInt(GetFirstQueryValue(query, "recv-window", "recv_window"), out var recvWindow))
        {
            return [];
        }
        extra.ShadowQuicCwnd = cwnd;
        extra.ShadowQuicMaxDatagramFrameSize = maxDatagramFrameSize;
        extra.ShadowQuicMaxOpenStreams = maxOpenStreams;
        extra.ShadowQuicRecvWindowConn = recvWindowConn;
        extra.ShadowQuicRecvWindow = recvWindow;
        extra.ShadowQuicDisableMtuDiscovery = ParseBoolFlag(GetFirstQueryValue(query, "disable-mtu-discovery", "disable_mtu_discovery"));

        return [server];
    }

    public bool CheckServer(Server s) => s is ShadowQUICServer && ServerValidator.Validate(s) == null;

    private static bool TryParseOptionalInt(string? value, out int? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }
}

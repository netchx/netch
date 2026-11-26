using System.Globalization;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public sealed class MieruUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 12;
    public string TypeName { get; } = nameof(EConfigType.Mieru);
    public string FullName { get; } = "Mieru";
    public string ShortName { get; } = "Mieru";
    public string[] UriScheme { get; } = ["mierus", "mieru"];
    public Type ServerType { get; } = typeof(MieruServer);

    public string GetShareLink(Server s)
    {
        if (s is not MieruServer server)
            return string.Empty;

        var extra = server.ProtoExtra;
        var query = new Dictionary<string, string>
        {
            ["port"] = !string.IsNullOrWhiteSpace(extra.MieruPortRange)
                ? Utils.Utils.UrlEncode(extra.MieruPortRange.Trim().Replace(':', '-'))
                : server.Port.ToString(CultureInfo.InvariantCulture),
            ["protocol"] = Utils.Utils.UrlEncode((!string.IsNullOrWhiteSpace(extra.MieruTransport) ? extra.MieruTransport : "TCP").Trim().ToUpperInvariant())
        };

        if (!string.IsNullOrWhiteSpace(extra.MieruMultiplexing))
            query["multiplexing"] = Utils.Utils.UrlEncode(extra.MieruMultiplexing.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(extra.MieruHandshakeMode))
            query["handshake-mode"] = Utils.Utils.UrlEncode(extra.MieruHandshakeMode.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(extra.MieruTrafficPattern))
            query["traffic-pattern"] = Utils.Utils.UrlEncode(extra.MieruTrafficPattern.Trim());

        var userInfo = $"{Utils.Utils.UrlEncode(server.Username ?? string.Empty)}:{Utils.Utils.UrlEncode(server.Password ?? string.Empty)}";
        var queryText = "?" + string.Join("&", query.Select(p => $"{p.Key}={p.Value}"));
        var remark = server.Remarks.IsNullOrEmpty() ? string.Empty : $"#{Utils.Utils.UrlEncode(server.Remarks)}";
        return $"{Constants.ProtocolShares[EConfigType.Mieru]}{userInfo}@{GetIpv6(server.Address)}{queryText}{remark}";
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
        var server = new MieruServer
        {
            Address = url.IdnHost,
            Username = user,
            Password = pass
        };

        var extra = server.ProtoExtra;
        var portRangeRaw = GetFirstQueryValue(query, "port-range", "port_range");
        var portRaw = GetQueryDecoded(query, "port");

        if (portRangeRaw.IsNotEmpty() && portRaw.IsNotEmpty())
            return [];

        if (portRangeRaw.IsNotEmpty())
        {
            if (!ServerValidator.IsValidSinglePortRange(portRangeRaw, out var startPort, out var endPort))
                return [];
            extra.MieruPortRange = $"{startPort.ToString(CultureInfo.InvariantCulture)}-{endPort.ToString(CultureInfo.InvariantCulture)}";
            server.Port = startPort;
        }
        else if (portRaw.IsNotEmpty())
        {
            if (portRaw.Contains('-') || portRaw.Contains(':'))
            {
                if (!ServerValidator.IsValidSinglePortRange(portRaw, out var startPort, out var endPort))
                    return [];
                extra.MieruPortRange = $"{startPort.ToString(CultureInfo.InvariantCulture)}-{endPort.ToString(CultureInfo.InvariantCulture)}";
                server.Port = startPort;
            }
            else if (int.TryParse(portRaw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort) &&
                     ServerValidator.IsValidPort(parsedPort))
            {
                server.Port = parsedPort;
            }
            else
            {
                return [];
            }
        }
        else
        {
            server.Port = url.Port;
        }

        if (!ServerValidator.IsValidPort(server.Port))
            return [];

        var transportRaw = GetFirstQueryValue(query, "protocol", "transport");
        extra.MieruTransport = string.IsNullOrWhiteSpace(transportRaw)
            ? "TCP"
            : transportRaw.Trim().ToUpperInvariant();

        var mux = GetQueryDecoded(query, "multiplexing");
        if (mux.IsNotEmpty())
            extra.MieruMultiplexing = mux.Trim().ToUpperInvariant();

        var handshake = GetFirstQueryValue(query, "handshake-mode", "handshake_mode");
        if (handshake.IsNotEmpty())
            extra.MieruHandshakeMode = handshake.Trim().ToUpperInvariant();

        var trafficPattern = GetFirstQueryValue(query, "traffic-pattern", "traffic_pattern");
        if (trafficPattern.IsNotEmpty())
            extra.MieruTrafficPattern = trafficPattern.Trim();

        var fragment = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
        server.Remarks = fragment.IsNotEmpty() ? fragment : GetQueryDecoded(query, "profile");
        return [server];
    }

    public bool CheckServer(Server s) => s is MieruServer && ServerValidator.Validate(s) == null;
}

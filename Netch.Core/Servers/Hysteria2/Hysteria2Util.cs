using System.Globalization;
using System.Text.RegularExpressions;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;
//https://v2.hysteria.network/zh/docs/developers/URI-Scheme/
public class Hysteria2Util : ServerUtilBase, IServerUtil
{
    private static readonly Regex HostPortsPattern = new(
        @"^(?<prefix>(?:hysteria2|hy2)://(?:[^/@?#]*@)?(?:\[[^\]]+\]|[^:/?#]+)):(?<ports>[0-9][0-9,\-:]*)(?<suffix>(?:[/?#].*)?)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ushort Priority { get; } = 3;

    public string TypeName { get; } = "Hysteria2";

    public string FullName { get; } = "Hysteria2";

    public string ShortName { get; } = "HY2";

    public string[] UriScheme { get; } = { "hysteria2", "hy2" };

    public Type ServerType { get; } = typeof(Hysteria2Server);

    internal static int? TryParseMbps(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();
        var multiplier = 1;
        var isKbps = false;
        if (text.EndsWith("Gbps", StringComparison.OrdinalIgnoreCase))
        {
            multiplier = 1000;
            text = text[..^4].Trim();
        }
        else if (text.EndsWith("Mbps", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^4].Trim();
        }
        else if (text.EndsWith("Kbps", StringComparison.OrdinalIgnoreCase))
        {
            isKbps = true;
            text = text[..^4].Trim();
        }
        else if (text.EndsWith('M') || text.EndsWith('m'))
        {
            text = text[..^1].Trim();
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var speed) || speed < 0)
            return null;

        if (isKbps)
            return speed == 0 ? 0 : Math.Max(1, speed / 1000);

        return speed * multiplier;
    }

    //hysteria2://letmein@example.com:123,5000-6000/?insecure=1&obfs=salamander&obfs-password=gawrgura&pinSHA256=deadbeef&sni=real.example.com
    //hysteria2://[auth@]hostname[:port]/?[key=value]&[key=value]...
    public string GetShareLink(Server s)
    {
        if (s is not Hysteria2Server item)
        {
            return string.Empty;
        }

        var remark = string.Empty;
        if (item.Remarks.IsNotEmpty())
        {
            remark = "#" + Utils.Utils.UrlEncode(item.Remarks);
        }
        var dicQuery = new Dictionary<string, string>();
        ToUriQueryLite(item, ref dicQuery);
        var protocolExtraItem = item.ProtoExtra;

        if (!protocolExtraItem.SalamanderPass.IsNullOrEmpty())
        {
            var obfsType = protocolExtraItem.Obfs == "gecko" ? "gecko" : "salamander";
            dicQuery.Add("obfs", obfsType);
            dicQuery.Add("obfs-password", Utils.Utils.UrlEncode(protocolExtraItem.SalamanderPass));
            if (obfsType == "gecko")
            {
                if (!protocolExtraItem.GeckoMinPacketSize.IsNullOrEmpty())
                    dicQuery.Add("obfs-min-packet-size", Utils.Utils.UrlEncode(protocolExtraItem.GeckoMinPacketSize));
                if (!protocolExtraItem.GeckoMaxPacketSize.IsNullOrEmpty())
                    dicQuery.Add("obfs-max-packet-size", Utils.Utils.UrlEncode(protocolExtraItem.GeckoMaxPacketSize));
            }
        }
        if (!protocolExtraItem.Ports.IsNullOrEmpty())
        {
            dicQuery.Add("mport", Utils.Utils.UrlEncode(protocolExtraItem.Ports.Replace(':', '-')));
        }
        if (!protocolExtraItem.HopInterval.IsNullOrEmpty())
        {
            dicQuery.Add("hop-interval", Utils.Utils.UrlEncode(protocolExtraItem.HopInterval));
        }
        if (protocolExtraItem.UpMbps is > 0)
        {
            dicQuery.Add("upmbps", protocolExtraItem.UpMbps.Value.ToString(CultureInfo.InvariantCulture));
        }
        if (protocolExtraItem.DownMbps is > 0)
        {
            dicQuery.Add("downmbps", protocolExtraItem.DownMbps.Value.ToString(CultureInfo.InvariantCulture));
        }
        if (!item.CertSha.IsNullOrEmpty())
        {
            var sha = item.CertSha;
            var idx = sha.IndexOf('~');
            if (idx > 0)
            {
                sha = sha[..idx];
            }
            dicQuery.Add("pinSHA256", Utils.Utils.UrlEncode(sha));
        }

        return ToUri(EConfigType.Hysteria2, item.Address, item.Port, item.Password ?? string.Empty, dicQuery, remark);
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var server = new Hysteria2Server();
        var hostPorts = ExtractHostPorts(text, out var normalizedUri);

        var url = Utils.Utils.TryUri(normalizedUri);
        if (url == null || url.IdnHost.IsNullOrEmpty())
        {
            return [];
        }

        server.Address = url.IdnHost;
        server.Port = url.Port > 0 ? url.Port : 443;
        server.Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
        server.Password = Utils.Utils.UrlDecode(url.UserInfo);

        var query = Utils.Utils.ParseQueryString(url.Query);
        ResolveTlsQueryOnly(query, server);
        if (server.CertSha.IsNullOrEmpty())
        {
            server.CertSha = GetFirstQueryValue(query, "pinSHA256", "pinsha256");
        }
        server.StreamSecurity = Constants.StreamSecurity;

        var mport = GetQueryDecoded(query, "mport");
        server.ProtoExtra.Ports = mport.IsNotEmpty() ? mport : hostPorts;
        server.ProtoExtra.HopInterval = GetFirstQueryValue(query, "hop-interval", "hop_interval", "hopInterval");
        server.ProtoExtra.UpMbps = TryParseMbps(GetFirstQueryValue(query, "upmbps", "up", "up_mbps"));
        server.ProtoExtra.DownMbps = TryParseMbps(GetFirstQueryValue(query, "downmbps", "down", "down_mbps"));

        server.ProtoExtra.SalamanderPass = GetFirstQueryValue(query, "obfs-password", "obfs_password");
        var obfs = GetQueryDecoded(query, "obfs");
        server.ProtoExtra.Obfs = obfs.IsNotEmpty()
            ? obfs.ToLowerInvariant()
            : (server.ProtoExtra.SalamanderPass.IsNotEmpty() ? "salamander" : Constants.None);
        server.ProtoExtra.GeckoMinPacketSize = GetFirstQueryValue(query, "obfs-min-packet-size", "obfs_min_packet_size");
        server.ProtoExtra.GeckoMaxPacketSize = GetFirstQueryValue(query, "obfs-max-packet-size", "obfs_max_packet_size");

        return [server];
    }

    public bool CheckServer(Server s)
    {
        return s.Address.IsNotEmpty() && ServerValidator.IsValidPort(s.Port);
    }

    private static string ExtractHostPorts(string text, out string normalizedUri)
    {
        normalizedUri = text;
        var match = HostPortsPattern.Match(text);
        if (!match.Success)
            return string.Empty;

        var ports = match.Groups["ports"].Value;
        if (!ports.Contains(',') && !ports.Contains('-') && !ports.Contains(':'))
            return string.Empty;

        var firstToken = ports.Split([',', '-', ':'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!ushort.TryParse(firstToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var firstPort) || firstPort == 0)
            return string.Empty;

        normalizedUri = $"{match.Groups["prefix"].Value}:{firstPort}{match.Groups["suffix"].Value}";
        return ports;
    }
}

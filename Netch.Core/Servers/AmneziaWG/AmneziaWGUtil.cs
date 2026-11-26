using System.Collections.Specialized;
using System.Globalization;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public sealed class AmneziaWGUtil : ServerUtilBase, IServerUtil
{
    private static readonly string[] AwgQueryKeys =
    [
        "version", "awg_version", "awg-version",
        "jc", "jmin", "jmax",
        "s1", "s2", "s3", "s4",
        "h1", "h2", "h3", "h4",
        "i1", "i2", "i3", "i4", "i5",
        "header-protection-key", "header_protection_key", "hpk",
        "content-padding-addition", "content_padding_addition",
        "rekey-after-time", "rekey_after_time",
        "rekey-timeout", "rekey_timeout",
        "reject-after-time", "reject_after_time",
        "keepalive-timeout", "keepalive_timeout",
        "max-handshake-attempts", "max_handshake_attempts",
        "random-trailers", "random_trailers",
        "disable-cookies", "disable_cookies"
    ];

    public ushort Priority { get; } = 14;
    public string TypeName { get; } = nameof(EConfigType.AmneziaWG);
    public string FullName { get; } = "AmneziaWG";
    public string ShortName { get; } = "AWG";
    public string[] UriScheme { get; } = ["amneziawg", "awg"];
    public Type ServerType { get; } = typeof(AmneziaWGServer);

    public string GetShareLink(Server s)
    {
        if (s is not AmneziaWGServer item)
            return string.Empty;

        var remark = item.Remarks.IsNotEmpty() ? "#" + Utils.Utils.UrlEncode(item.Remarks) : string.Empty;
        var extra = item.ProtoExtra;
        var dicQuery = new Dictionary<string, string>();
        if (!extra.WgPublicKey.IsNullOrEmpty())
            dicQuery.Add("publickey", Utils.Utils.UrlEncode(extra.WgPublicKey));
        if (!extra.WgReserved.IsNullOrEmpty())
            dicQuery.Add("reserved", Utils.Utils.UrlEncode(extra.WgReserved));
        if (!extra.WgPresharedKey.IsNullOrEmpty())
            dicQuery.Add("presharedkey", Utils.Utils.UrlEncode(extra.WgPresharedKey));
        if (!extra.WgInterfaceAddress.IsNullOrEmpty())
            dicQuery.Add("address", Utils.Utils.UrlEncode(extra.WgInterfaceAddress));
        dicQuery.Add("mtu", Utils.Utils.UrlEncode(extra.WgMtu > 0 ? extra.WgMtu.ToString() : "1280"));

        if (extra.AwgVersion is > 0)
            dicQuery["version"] = extra.AwgVersion.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.AwgJc.HasValue)
            dicQuery["jc"] = extra.AwgJc.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.AwgJmin.HasValue)
            dicQuery["jmin"] = extra.AwgJmin.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.AwgJmax.HasValue)
            dicQuery["jmax"] = extra.AwgJmax.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.AwgS1.HasValue)
            dicQuery["s1"] = extra.AwgS1.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.AwgS2.HasValue)
            dicQuery["s2"] = extra.AwgS2.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.AwgS3.HasValue)
            dicQuery["s3"] = extra.AwgS3.Value.ToString(CultureInfo.InvariantCulture);
        if (extra.AwgS4.HasValue)
            dicQuery["s4"] = extra.AwgS4.Value.ToString(CultureInfo.InvariantCulture);
        AddIfNotEmpty(dicQuery, "h1", extra.AwgH1);
        AddIfNotEmpty(dicQuery, "h2", extra.AwgH2);
        AddIfNotEmpty(dicQuery, "h3", extra.AwgH3);
        AddIfNotEmpty(dicQuery, "h4", extra.AwgH4);
        AddIfNotEmpty(dicQuery, "i1", extra.AwgI1);
        AddIfNotEmpty(dicQuery, "i2", extra.AwgI2);
        AddIfNotEmpty(dicQuery, "i3", extra.AwgI3);
        AddIfNotEmpty(dicQuery, "i4", extra.AwgI4);
        AddIfNotEmpty(dicQuery, "i5", extra.AwgI5);
        AddIfNotEmpty(dicQuery, "header-protection-key", extra.AwgHeaderProtectionKey);
        AddIfNotEmpty(dicQuery, "content-padding-addition", extra.AwgContentPaddingAddition);
        AddIfNotEmpty(dicQuery, "rekey-after-time", extra.AwgRekeyAfterTime);
        AddIfNotEmpty(dicQuery, "rekey-timeout", extra.AwgRekeyTimeout);
        AddIfNotEmpty(dicQuery, "reject-after-time", extra.AwgRejectAfterTime);
        AddIfNotEmpty(dicQuery, "keepalive-timeout", extra.AwgKeepaliveTimeout);
        AddIfNotEmpty(dicQuery, "max-handshake-attempts", extra.AwgMaxHandshakeAttempts);
        if (extra.AwgRandomTrailers == true)
            dicQuery["random-trailers"] = "1";
        if (extra.AwgDisableCookies == true)
            dicQuery["disable-cookies"] = "1";

        return ToUri(EConfigType.AmneziaWG, item.Address, item.Port, item.Password ?? string.Empty, dicQuery, remark);
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        var url = Utils.Utils.TryUri(str);
        if (url == null || url.IdnHost.IsNullOrEmpty() || !ServerValidator.IsValidPort(url.Port))
            return [];

        var item = new AmneziaWGServer
        {
            Address = url.IdnHost,
            Port = url.Port,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            Password = Utils.Utils.UrlDecode(url.UserInfo)
        };

        var query = Utils.Utils.ParseQueryString(url.Query);
        PopulateWireGuardQuery(query, item);
        if (!TryPopulateAwgQuery(query, item))
            return [];
        return [item];
    }

    public bool CheckServer(Server s) => s is AmneziaWGServer && ServerValidator.Validate(s) == null;

    internal static bool HasAwgQueryParams(NameValueCollection query) =>
        AwgQueryKeys.Any(key => !string.IsNullOrEmpty(query[key]));

    internal static void PopulateWireGuardQuery(NameValueCollection query, WireGuardServer item)
    {
        if (item.Password.IsNullOrEmpty())
            item.Password = GetFirstQueryValue(query, "secret", "privatekey", "private-key", "private_key");
        item.ProtoExtra.WgPublicKey = GetFirstQueryValue(query, "publickey", "public-key", "public_key", "pbk");
        item.ProtoExtra.WgPresharedKey = GetFirstQueryValue(query, "presharedkey", "preshared-key", "pre-shared-key", "pre_shared_key", "psk", "psi");
        var reservedRaw = GetQueryDecoded(query, "reserved");
        item.ProtoExtra.WgReserved = ServerValidator.TryNormalizeWireGuardReserved(reservedRaw, out var normalizedReserved)
            ? normalizedReserved
            : reservedRaw;
        item.ProtoExtra.WgInterfaceAddress = GetFirstQueryValue(query, "address", "ip", "local_address");
        item.ProtoExtra.WgMtu = int.TryParse(GetQueryDecoded(query, "mtu"), out var mtuVal) && mtuVal > 0 ? mtuVal : 1280;
    }

    internal static bool TryPopulateAwgQuery(NameValueCollection query, AmneziaWGServer item)
    {
        if (!TryParseOptionalInt(GetFirstQueryValue(query, "version", "awg_version", "awg-version"), out var version) ||
            !TryParseOptionalInt(GetQueryDecoded(query, "jc"), out var jc) ||
            !TryParseOptionalInt(GetQueryDecoded(query, "jmin"), out var jmin) ||
            !TryParseOptionalInt(GetQueryDecoded(query, "jmax"), out var jmax) ||
            !TryParseOptionalInt(GetQueryDecoded(query, "s1"), out var s1) ||
            !TryParseOptionalInt(GetQueryDecoded(query, "s2"), out var s2) ||
            !TryParseOptionalInt(GetQueryDecoded(query, "s3"), out var s3) ||
            !TryParseOptionalInt(GetQueryDecoded(query, "s4"), out var s4))
        {
            return false;
        }

        var extra = item.ProtoExtra;
        extra.AwgVersion = version;
        extra.AwgJc = jc;
        extra.AwgJmin = jmin;
        extra.AwgJmax = jmax;
        extra.AwgS1 = s1;
        extra.AwgS2 = s2;
        extra.AwgS3 = s3;
        extra.AwgS4 = s4;
        extra.AwgH1 = GetQueryDecoded(query, "h1");
        extra.AwgH2 = GetQueryDecoded(query, "h2");
        extra.AwgH3 = GetQueryDecoded(query, "h3");
        extra.AwgH4 = GetQueryDecoded(query, "h4");
        extra.AwgI1 = GetQueryDecoded(query, "i1");
        extra.AwgI2 = GetQueryDecoded(query, "i2");
        extra.AwgI3 = GetQueryDecoded(query, "i3");
        extra.AwgI4 = GetQueryDecoded(query, "i4");
        extra.AwgI5 = GetQueryDecoded(query, "i5");
        extra.AwgHeaderProtectionKey = GetFirstQueryValue(query, "header-protection-key", "header_protection_key", "hpk");
        extra.AwgContentPaddingAddition = GetFirstQueryValue(query, "content-padding-addition", "content_padding_addition");
        extra.AwgRekeyAfterTime = GetFirstQueryValue(query, "rekey-after-time", "rekey_after_time");
        extra.AwgRekeyTimeout = GetFirstQueryValue(query, "rekey-timeout", "rekey_timeout");
        extra.AwgRejectAfterTime = GetFirstQueryValue(query, "reject-after-time", "reject_after_time");
        extra.AwgKeepaliveTimeout = GetFirstQueryValue(query, "keepalive-timeout", "keepalive_timeout");
        extra.AwgMaxHandshakeAttempts = GetFirstQueryValue(query, "max-handshake-attempts", "max_handshake_attempts");
        extra.AwgRandomTrailers = ParseBoolFlag(GetFirstQueryValue(query, "random-trailers", "random_trailers"));
        extra.AwgDisableCookies = ParseBoolFlag(GetFirstQueryValue(query, "disable-cookies", "disable_cookies"));
        return true;
    }

    private static void AddIfNotEmpty(Dictionary<string, string> query, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            query[key] = Utils.Utils.UrlEncode(value);
    }

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

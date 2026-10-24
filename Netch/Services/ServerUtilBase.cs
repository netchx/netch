using Netch.Enums;
using Netch.Models;
using Netch.Utils;
using System.Collections.Specialized;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Netch.Services;

public class ServerUtilBase
{
    private static readonly string[] _allowInsecureArray = new[] { "insecure", "allowInsecure", "allow_insecure" };

    protected static string GetIpv6(string address)
    {
        if (Utils.Utils.IsIpv6(address))
        {
            // Check if the address is already surrounded by square brackets, if not, add square brackets
            return address.StartsWith('[') && address.EndsWith(']') ? address : $"[{address}]";
        }
        else
        {
            return address;
        }
    }

    protected static int ToUriQuery(Server item, string? securityDef, ref Dictionary<string, string> dicQuery)
    {
        if (item.StreamSecurity.IsNotEmpty())
        {
            dicQuery.Add("security", item.StreamSecurity);
        }
        else
        {
            if (securityDef != null)
            {
                dicQuery.Add("security", securityDef);
            }
        }
        if (item.Sni.IsNotEmpty())
        {
            dicQuery.Add("sni", Utils.Utils.UrlEncode(item.Sni));
        }
        if (item.Fingerprint.IsNotEmpty())
        {
            dicQuery.Add("fp", Utils.Utils.UrlEncode(item.Fingerprint));
        }
        if (item.PublicKey.IsNotEmpty())
        {
            dicQuery.Add("pbk", Utils.Utils.UrlEncode(item.PublicKey));
        }
        if (item.ShortId.IsNotEmpty())
        {
            dicQuery.Add("sid", Utils.Utils.UrlEncode(item.ShortId));
        }
        if (item.SpiderX.IsNotEmpty())
        {
            dicQuery.Add("spx", Utils.Utils.UrlEncode(item.SpiderX));
        }
        if (item.Mldsa65Verify.IsNotEmpty())
        {
            dicQuery.Add("pqv", Utils.Utils.UrlEncode(item.Mldsa65Verify));
        }

        if (item.StreamSecurity.Equals(Constants.StreamSecurity))
        {
            if (item.Alpn.IsNotEmpty())
            {
                dicQuery.Add("alpn", Utils.Utils.UrlEncode(item.Alpn));
            }
            ToUriQueryAllowInsecure(item, ref dicQuery);
        }
        if (item.EchConfigList.IsNotEmpty())
        {
            dicQuery.Add("ech", Utils.Utils.UrlEncode(item.EchConfigList));
        }
        if (item.CertSha.IsNotEmpty())
        {
            dicQuery.Add("pcs", Utils.Utils.UrlEncode(item.CertSha));
        }

        dicQuery.Add("type", item.Network.IsNotEmpty() ? item.Network : nameof(ETransport.tcp));

        switch (item.Network)
        {
            case nameof(ETransport.tcp):
                dicQuery.Add("headerType", item.HeaderType.IsNotEmpty() ? item.HeaderType : Constants.None);
                if (item.RequestHost.IsNotEmpty())
                {
                    dicQuery.Add("host", Utils.Utils.UrlEncode(item.RequestHost));
                }
                break;

            case nameof(ETransport.kcp):
                dicQuery.Add("headerType", item.HeaderType.IsNotEmpty() ? item.HeaderType : Constants.None);
                if (item.Path.IsNotEmpty())
                {
                    dicQuery.Add("seed", Utils.Utils.UrlEncode(item.Path));
                }
                break;

            case nameof(ETransport.ws):
            case nameof(ETransport.httpupgrade):
                if (item.RequestHost.IsNotEmpty())
                {
                    dicQuery.Add("host", Utils.Utils.UrlEncode(item.RequestHost));
                }
                if (item.Path.IsNotEmpty())
                {
                    dicQuery.Add("path", Utils.Utils.UrlEncode(item.Path));
                }
                break;

            case nameof(ETransport.xhttp):
                if (item.RequestHost.IsNotEmpty())
                {
                    dicQuery.Add("host", Utils.Utils.UrlEncode(item.RequestHost));
                }
                if (item.Path.IsNotEmpty())
                {
                    dicQuery.Add("path", Utils.Utils.UrlEncode(item.Path));
                }
                if (item.HeaderType.IsNotEmpty() && Constants.XhttpMode.Contains(item.HeaderType))
                {
                    dicQuery.Add("mode", Utils.Utils.UrlEncode(item.HeaderType));
                }
                if (item.Extra.IsNotEmpty())
                {
                    var node = JsonUtils.ParseJson(item.Extra);
                    var extra = node != null
                        ? JsonUtils.Serialize(node, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        })
                        : item.Extra;
                    dicQuery.Add("extra", Utils.Utils.UrlEncode(extra));
                }
                break;

            case nameof(ETransport.http):
            case nameof(ETransport.h2):
                dicQuery["type"] = nameof(ETransport.http);
                if (item.RequestHost.IsNotEmpty())
                {
                    dicQuery.Add("host", Utils.Utils.UrlEncode(item.RequestHost));
                }
                if (item.Path.IsNotEmpty())
                {
                    dicQuery.Add("path", Utils.Utils.UrlEncode(item.Path));
                }
                break;

            case nameof(ETransport.quic):
                dicQuery.Add("headerType", item.HeaderType.IsNotEmpty() ? item.HeaderType : Constants.None);
                dicQuery.Add("quicSecurity", Utils.Utils.UrlEncode(item.RequestHost));
                dicQuery.Add("key", Utils.Utils.UrlEncode(item.Path));
                break;

            case nameof(ETransport.grpc):
                if (item.Path.IsNotEmpty())
                {
                    dicQuery.Add("authority", Utils.Utils.UrlEncode(item.RequestHost));
                    dicQuery.Add("serviceName", Utils.Utils.UrlEncode(item.Path));
                    if (item.HeaderType is Constants.GrpcGunMode or Constants.GrpcMultiMode)
                    {
                        dicQuery.Add("mode", Utils.Utils.UrlEncode(item.HeaderType));
                    }
                }
                break;
        }
        return 0;
    }

    protected static int ToUriQueryLite(Server item, ref Dictionary<string, string> dicQuery)
    {
        if (item.Sni.IsNotEmpty())
        {
            dicQuery.Add("sni", Utils.Utils.UrlEncode(item.Sni));
        }
        if (item.Alpn.IsNotEmpty())
        {
            dicQuery.Add("alpn", Utils.Utils.UrlEncode(item.Alpn));
        }

        ToUriQueryAllowInsecure(item, ref dicQuery);

        return 0;
    }

    private static int ToUriQueryAllowInsecure(Server item, ref Dictionary<string, string> dicQuery)
    {
        if (item.AllowInsecure == true)
        {
            // Add two for compatibility
            dicQuery.Add("insecure", "1");
            dicQuery.Add("allowInsecure", "1");
        }
        else
        {
            dicQuery.Add("insecure", "0");
            dicQuery.Add("allowInsecure", "0");
        }

        return 0;
    }

    protected static int ResolveUriQuery<T>(NameValueCollection query, ref T item) where T : Server
    {
        item.StreamSecurity = GetQueryValue(query, "security");
        item.Sni = GetQueryValue(query, "sni");
        item.Alpn = GetQueryDecoded(query, "alpn");
        item.Fingerprint = GetQueryDecoded(query, "fp");
        item.PublicKey = GetQueryDecoded(query, "pbk");
        item.ShortId = GetQueryDecoded(query, "sid");
        item.SpiderX = GetQueryDecoded(query, "spx");
        item.Mldsa65Verify = GetQueryDecoded(query, "pqv");
        item.EchConfigList = GetQueryDecoded(query, "ech");
        item.CertSha = GetQueryDecoded(query, "pcs");

        if (_allowInsecureArray.Any(k => GetQueryDecoded(query, k) == "1"))
        {
            item.AllowInsecure = true;
        }
        else if (_allowInsecureArray.Any(k => GetQueryDecoded(query, k) == "0"))
        {
            item.AllowInsecure = false;
        }
        else
        {
            item.AllowInsecure = null;
        }

        item.Network = GetQueryValue(query, "type", nameof(ETransport.tcp));
        switch (item.Network)
        {
            case nameof(ETransport.tcp):
                item.HeaderType = GetQueryValue(query, "headerType", Constants.None);
                item.RequestHost = GetQueryDecoded(query, "host");
                break;

            case nameof(ETransport.kcp):
                item.HeaderType = GetQueryValue(query, "headerType", Constants.None);
                item.Path = GetQueryDecoded(query, "seed");
                break;

            case nameof(ETransport.ws):
            case nameof(ETransport.httpupgrade):
                item.RequestHost = GetQueryDecoded(query, "host");
                item.Path = GetQueryDecoded(query, "path", "/");
                break;

            case nameof(ETransport.xhttp):
                item.RequestHost = GetQueryDecoded(query, "host");
                item.Path = GetQueryDecoded(query, "path", "/");
                item.HeaderType = GetQueryDecoded(query, "mode");
                var extraDecoded = GetQueryDecoded(query, "extra");
                if (extraDecoded.IsNotEmpty())
                {
                    var node = JsonUtils.ParseJson(extraDecoded);
                    if (node != null)
                    {
                        extraDecoded = JsonUtils.Serialize(node, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                    }
                }
                item.Extra = extraDecoded;
                break;

            case nameof(ETransport.http):
            case nameof(ETransport.h2):
                item.Network = nameof(ETransport.h2);
                item.RequestHost = GetQueryDecoded(query, "host");
                item.Path = GetQueryDecoded(query, "path", "/");
                break;

            case nameof(ETransport.quic):
                item.HeaderType = GetQueryValue(query, "headerType", Constants.None);
                item.RequestHost = GetQueryValue(query, "quicSecurity", Constants.None);
                item.Path = GetQueryDecoded(query, "key");
                break;

            case nameof(ETransport.grpc):
                item.RequestHost = GetQueryDecoded(query, "authority");
                item.Path = GetQueryDecoded(query, "serviceName");
                item.HeaderType = GetQueryDecoded(query, "mode", Constants.GrpcGunMode);
                break;

            default:
                break;
        }

        AddServerCommon(item);
        return 0;
    }

    /// <summary>
    /// Common server addition logic used by all server types
    /// Sets common properties and handles sorting and persistence
    /// </summary>
    /// <param name="profileItem">Profile item to add</param>
    /// <returns>0 if successful</returns>
    public static int AddServerCommon<T>(T profileItem) where T : Server
    {
        profileItem.ConfigVersion = 3;

        if (profileItem.StreamSecurity.IsNotEmpty())
        {
            if (profileItem.StreamSecurity != Constants.StreamSecurity
                 && profileItem.StreamSecurity != Constants.StreamSecurityReality)
            {
                profileItem.StreamSecurity = string.Empty;
            }
            else
            {
                if (profileItem.AllowInsecure == null)
                {
                    profileItem.AllowInsecure = Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure;
                }
                if (profileItem.Fingerprint.IsNullOrEmpty() && profileItem.StreamSecurity == Constants.StreamSecurityReality)
                {
                    profileItem.Fingerprint = Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint;
                }
            }
        }

        if (profileItem.Network.IsNotEmpty() && !Constants.Networks.Contains(profileItem.Network))
        {
            profileItem.Network = Constants.DefaultNetwork;
        }
        return 0;
    }

    protected static bool Contains(string str, params string[] s)
    {
        return s.All(item => str.Contains(item, StringComparison.OrdinalIgnoreCase));
    }

    protected static string WriteAllText(string strData, string ext = "json")
    {
        var fileName = Utils.Utils.GetTempPath($"{Utils.Utils.GetGuid(false)}.{ext}");
        File.WriteAllText(fileName, strData);
        return fileName;
    }

    protected static string ToUri(EConfigType eConfigType, string address, object port, string userInfo, Dictionary<string, string>? dicQuery, string? remark)
    {
        var query = dicQuery != null
            ? ("?" + string.Join("&", dicQuery.Select(x => x.Key + "=" + x.Value).ToArray()))
            : string.Empty;

        var url = $"{Utils.Utils.UrlEncode(userInfo)}@{GetIpv6(address)}:{port}";
        return $"{Constants.ProtocolShares[eConfigType]}{url}{query}{remark}";
    }

    protected static string GetQueryValue(NameValueCollection query, string key, string defaultValue = "")
    {
        return query[key] ?? defaultValue;
    }

    protected static string GetQueryDecoded(NameValueCollection query, string key, string defaultValue = "")
    {
        return Utils.Utils.UrlDecode(GetQueryValue(query, key, defaultValue));
    }
}

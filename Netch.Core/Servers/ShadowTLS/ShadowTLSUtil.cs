using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public sealed class ShadowTLSUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 9;
    public string TypeName { get; } = nameof(EConfigType.ShadowTLS);
    public string FullName { get; } = "ShadowTLS";
    public string ShortName { get; } = "ShadowTLS";
    public string[] UriScheme { get; } = [];
    public Type ServerType { get; } = typeof(ShadowTLSServer);

    public string GetShareLink(Server s)
    {
        if (s.Cert.IsNotEmpty()
            || s.CertSha.IsNotEmpty()
            || s.EchConfigList.IsNotEmpty()
            || (s.Fingerprint.IsNotEmpty() && !s.Fingerprint.Equals(Constants.None, StringComparison.OrdinalIgnoreCase))
            || s.Alpn.IsNotEmpty()
            || s.AllowInsecure.HasValue)
        {
            return string.Empty;
        }

        var version = s.ProtoExtra.ShadowTlsVersion is >= 1 and <= 3
            ? s.ProtoExtra.ShadowTlsVersion.Value
            : 3;

        var pluginParts = new List<string>
        {
            "shadow-tls",
            $"version={version.ToString(CultureInfo.InvariantCulture)}"
        };
        if (s.Sni.IsNotEmpty())
            pluginParts.Add($"host={EscapePluginValue(s.Sni)}");
        if (version >= 2 && s.ProtoExtra.ShadowTlsPassword.IsNotEmpty())
            pluginParts.Add($"password={EscapePluginValue(s.ProtoExtra.ShadowTlsPassword)}");

        var query = new List<string>
        {
            $"plugin={Utils.Utils.UrlEncode(string.Join(";", pluginParts))}"
        };
        if (s.ProtoExtra.Uot == true)
            query.Add("uot=1");

        var userInfo = ShareLink.URLSafeBase64Encode($"{s.ProtoExtra.SsMethod}:{s.Password}");
        var remark = s.Remarks.IsNullOrEmpty() ? string.Empty : $"#{Utils.Utils.UrlEncode(s.Remarks)}";
        return $"ss://{userInfo}@{GetIpv6(s.Address)}:{s.Port}/?{string.Join("&", query)}{remark}";
    }

    public IEnumerable<Server> ParseUri(string text) => [];

    public bool CheckServer(Server s)
    {
        if (s is not ShadowTLSServer || string.IsNullOrWhiteSpace(s.Address) || !ServerValidator.IsValidPort(s.Port))
            return false;

        if (!ShadowsocksUtil.IsSupportedCipher(s.ProtoExtra.SsMethod) ||
            !ServerValidator.IsValidShadowsocksPassword(s.ProtoExtra.SsMethod, s.Password))
            return false;

        var version = s.ProtoExtra.ShadowTlsVersion;
        if (version is not (1 or 2 or 3))
            return false;

        if (version >= 2 && s.ProtoExtra.ShadowTlsPassword.IsNullOrEmpty())
            return false;

        return true;
    }

    internal static (bool IsShadowTls, Dictionary<string, string> Options) ParsePluginOptions(string? rawPlugin)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawPlugin))
            return (false, options);

        var segments = SplitUnescaped(rawPlugin, ';');
        if (segments.Count == 0 || !segments[0].Trim().Equals("shadow-tls", StringComparison.OrdinalIgnoreCase))
            return (false, options);

        for (var i = 1; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (string.IsNullOrWhiteSpace(segment))
                continue;

            var eqIndex = FindUnescaped(segment, '=');
            if (eqIndex <= 0)
                continue;

            var key = UnescapePluginValue(segment[..eqIndex]).Trim();
            var value = UnescapePluginValue(segment[(eqIndex + 1)..]);
            if (key.Length > 0)
                options[key] = value;
        }

        return (true, options);
    }

    internal static ShadowTLSServer? CreateFromShadowsocks(
        string address,
        int port,
        string? remarks,
        string method,
        string ssPassword,
        IReadOnlyDictionary<string, string> pluginOptions,
        NameValueCollection? query = null)
    {
        int version = 3;
        if (pluginOptions.TryGetValue("version", out var rawVersion) && !string.IsNullOrWhiteSpace(rawVersion))
        {
            if (!int.TryParse(rawVersion.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out version)
                || version is not (1 or 2 or 3))
            {
                return null;
            }
        }

        var server = new ShadowTLSServer
        {
            Address = address,
            Port = port,
            Remarks = remarks ?? string.Empty,
            Password = ssPassword,
            StreamSecurity = Constants.StreamSecurity
        };

        if (query != null)
        {
            ResolveTlsQueryOnly(query, server);
            server.StreamSecurity = Constants.StreamSecurity;
            var uotRaw = GetFirstQueryValue(query, "uot", "udp-over-tcp", "udp_over_tcp");
            server.ProtoExtra.Uot = uotRaw == "2" ? true : ParseBoolFlag(uotRaw);
        }

        server.ProtoExtra.SsMethod = method;
        server.ProtoExtra.ShadowTlsVersion = version;
        if (pluginOptions.TryGetValue("password", out var handshakePassword) && !string.IsNullOrWhiteSpace(handshakePassword))
            server.ProtoExtra.ShadowTlsPassword = handshakePassword;

        if (server.Sni.IsNullOrEmpty())
        {
            if (pluginOptions.TryGetValue("host", out var host) && !string.IsNullOrWhiteSpace(host))
                server.Sni = host.Trim();
            else if (pluginOptions.TryGetValue("sni", out var sni) && !string.IsNullOrWhiteSpace(sni))
                server.Sni = sni.Trim();
        }

        if (pluginOptions.TryGetValue("fast-open", out _) && query == null)
        {
            // fast-open has no dedicated property; preserved safely by ignoring unknown optional plugin flags.
        }

        return server;
    }

    private static string EscapePluginValue(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is '\\' or ';' or '=')
                sb.Append('\\');
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static List<string> SplitUnescaped(string value, char separator)
    {
        var result = new List<string>();
        var start = 0;
        var escaped = false;
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }
            if (ch == '\\')
            {
                escaped = true;
                continue;
            }
            if (ch == separator)
            {
                result.Add(value[start..i]);
                start = i + 1;
            }
        }
        result.Add(value[start..]);
        return result;
    }

    private static int FindUnescaped(string value, char target)
    {
        var escaped = false;
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }
            if (ch == '\\')
            {
                escaped = true;
                continue;
            }
            if (ch == target)
                return i;
        }
        return -1;
    }

    private static string UnescapePluginValue(string value)
    {
        if (!value.Contains('\\'))
            return value;

        var sb = new StringBuilder(value.Length);
        var escaped = false;
        foreach (var ch in value)
        {
            if (escaped)
            {
                sb.Append(ch);
                escaped = false;
                continue;
            }
            if (ch == '\\')
            {
                escaped = true;
                continue;
            }
            sb.Append(ch);
        }
        if (escaped)
            sb.Append('\\');
        return sb.ToString();
    }
}

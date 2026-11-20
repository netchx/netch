using Netch.Enums;
using Netch.Interfaces;
using Netch.Manager;
using Netch.Models;
using Netch.Services;
using Netch.Utils;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Netch.Servers;

public class ShadowsocksUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 1;

    public string TypeName { get; } = "Shadowsocks";

    public string FullName { get; } = "Shadowsocks";

    public string ShortName { get; } = "SS";

    public string[] UriScheme { get; } = { "ss", "ssd" };

    public Type ServerType { get; } = typeof(ShadowsocksServer);

    internal static string DecodePluginPath(string path) =>
        path.Replace("\\=", "=").Replace("\\,", ",").Replace("\\\\", "\\");

    internal static string EncodePluginPath(string path) =>
        path.Replace("\\", "\\\\").Replace("=", "\\=").Replace(",", "\\,");

    internal static bool IsSupportedCipher(string? method) =>
        method != null && Constants.SsCiphers.Contains(method, StringComparer.OrdinalIgnoreCase);

    public string GetShareLink(Server s)
    {
        if (s is not ShadowsocksServer item)
        {
            return string.Empty;
        }
        var remark = string.Empty;
        if (item.Remarks.IsNotEmpty())
        {
            remark = "#" + Utils.Utils.UrlEncode(item.Remarks);
        }
        var pw = Utils.Utils.Base64Encode($"{item.ProtoExtra.SsMethod}:{item.Password}", true);

        // plugin
        var plugin = string.Empty;
        var pluginArgs = string.Empty;

        if (item.ProtoExtra.SsPlugin.IsNotEmpty())
        {
            plugin = item.ProtoExtra.SsPlugin;
            pluginArgs = item.ProtoExtra.SsPluginOptions ?? string.Empty;
        }
        else if (item.Network == nameof(ETransport.tcp) && item.HeaderType == Constants.TcpHeaderHttp)
        {
            plugin = "obfs-local";
            pluginArgs = $"obfs=http;obfs-host={item.RequestHost};";
        }
        else
        {
            if (item.Network == nameof(ETransport.ws))
            {
                pluginArgs += "mode=websocket;";
                pluginArgs += $"host={item.RequestHost};";
                // https://github.com/shadowsocks/v2ray-plugin/blob/e9af1cdd2549d528deb20a4ab8d61c5fbe51f306/args.go#L172
                // Equal signs and commas [and backslashes] must be escaped with a backslash.
                var path = EncodePluginPath(item.Path);
                pluginArgs += $"path={path};";
            }
            else if (item.Network == nameof(ETransport.quic))
            {
                pluginArgs += "mode=quic;";
            }
            if (item.StreamSecurity == Constants.StreamSecurity)
            {
                pluginArgs += "tls;";
                var certs = CertPemManager.ParsePemChain(item.Cert);
                if (certs.Count > 0)
                {
                    var cert = certs.First();
                    const string beginMarker = "-----BEGIN CERTIFICATE-----\n";
                    const string endMarker = "\n-----END CERTIFICATE-----";

                    var base64Content = cert.Replace(beginMarker, "").Replace(endMarker, "").Trim();

                    base64Content = base64Content.Replace("=", "\\=");

                    pluginArgs += $"certRaw={base64Content};";
                }
            }
            if (pluginArgs.Length > 0)
            {
                plugin = "v2ray-plugin";
                pluginArgs += "mux=0;";
            }
        }

        var dicQuery = new Dictionary<string, string>();
        if (plugin.IsNotEmpty())
        {
            var pluginStr = pluginArgs.IsNotEmpty() ? plugin + ";" + pluginArgs : plugin;
            if (pluginStr.EndsWith(';'))
            {
                pluginStr = pluginStr[..^1];
            }
            dicQuery["plugin"] = Utils.Utils.UrlEncode(pluginStr);
        }
        if (item.ProtoExtra.Uot == true)
        {
            dicQuery["uot"] = "1";
        }
        if (item.AllowInsecure == true && (plugin.IsNotEmpty() || item.StreamSecurity == Constants.StreamSecurity))
        {
            dicQuery["allowInsecure"] = "1";
        }

        return ToUri(EConfigType.Shadowsocks, item.Address, item.Port, pw, dicQuery, remark);
    }

    public IEnumerable<Server> ParseUri(string str)
    {
        if (str.StartsWith("ssd://", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveSsd(str);
        }

        var item = ResolveSSLegacy(str) ?? ResolveSip002(str);
        if (item == null || !CheckServer(item))
        {
            return [];
        }

        return [item];
    }

    public bool CheckServer(Server s)
    {
        if (s is not ShadowsocksServer server || s.Address.IsNullOrEmpty() || !ServerValidator.IsValidPort(s.Port))
            return false;

        if (!IsSupportedCipher(server.ProtoExtra.SsMethod))
        {
            Log.Warning("Unsupported SS Encrypt Method: {Method}", server.ProtoExtra.SsMethod);
            return false;
        }

        return ServerValidator.IsValidShadowsocksPassword(server.ProtoExtra.SsMethod, server.Password);
    }

    private static readonly Regex UrlFinder = new(@"^ss://(?<base64>[A-Za-z0-9+/=_-]+)/*(?:#(?<tag>.*))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DetailsParser = new(@"^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\d+?))$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private IEnumerable<Server> ResolveSsd(string str)
    {
        var jsonText = Utils.Utils.Base64Decode(str["ssd://".Length..]);
        if (string.IsNullOrWhiteSpace(jsonText))
            return [];

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonText);
        }
        catch (JsonException)
        {
            return [];
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("servers", out var serversNode) || serversNode.ValueKind != JsonValueKind.Array)
                return [];

            var airport = ReadString(root, "airport");
            var defaultPort = ReadInt(root, "port");
            var defaultMethod = ReadString(root, "encryption");
            var defaultPassword = ReadString(root, "password");
            var defaultPlugin = ReadString(root, "plugin");
            var defaultPluginOpts = ReadString(root, "plugin_options");

            var results = new List<Server>();
            foreach (var entry in serversNode.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    continue;

                var item = new ShadowsocksServer
                {
                    Address = ReadString(entry, "server").Trim('[', ']'),
                    Port = ReadInt(entry, "port") is > 0 and var p ? p : defaultPort,
                    Password = ReadString(entry, "password") is { Length: > 0 } pw ? pw : defaultPassword,
                    Remarks = ReadString(entry, "remarks") is { Length: > 0 } r ? r : airport
                };
                item.ProtoExtra.SsMethod = ReadString(entry, "encryption") is { Length: > 0 } m ? m : defaultMethod;

                var plugin = ReadString(entry, "plugin") is { Length: > 0 } ep ? ep : defaultPlugin;
                var pluginOpts = ReadString(entry, "plugin_options") is { Length: > 0 } epo ? epo : defaultPluginOpts;
                ApplyPlugin(item, plugin, pluginOpts);

                if (CheckServer(item))
                    results.Add(item);
            }
            return results;
        }

        static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? string.Empty : string.Empty;

        static int ReadInt(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var prop))
                return 0;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val))
                return val;
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out val))
                return val;
            return 0;
        }
    }

    private static ShadowsocksServer? ResolveSSLegacy(string result)
    {
        var match = UrlFinder.Match(result);
        if (!match.Success)
        {
            return null;
        }

        ShadowsocksServer item = new();
        var base64 = match.Groups["base64"].Value.TrimEnd('/');
        var tag = match.Groups["tag"].Value;
        if (tag.IsNotEmpty())
        {
            item.Remarks = Utils.Utils.UrlDecode(tag);
        }
        Match details;
        try
        {
            details = DetailsParser.Match(Utils.Utils.Base64Decode(base64));
        }
        catch (FormatException)
        {
            return null;
        }
        if (!details.Success)
        {
            return null;
        }
        item.ProtoExtra.SsMethod = details.Groups["method"].Value;
        item.Password = details.Groups["password"].Value;
        item.Address = details.Groups["hostname"].Value.Trim('[', ']');
        item.Port = details.Groups["port"].Value.ToInt();
        return item;
    }

    private static ShadowsocksServer? ResolveSip002(string result)
    {
        var parsedUrl = Utils.Utils.TryUri(result);
        if (parsedUrl == null || parsedUrl.IdnHost.IsNullOrEmpty() || !ServerValidator.IsValidPort(parsedUrl.Port))
        {
            return null;
        }

        ShadowsocksServer item = new()
        {
            Remarks = parsedUrl.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
            Address = parsedUrl.IdnHost,
            Port = parsedUrl.Port,
        };
        if (parsedUrl.UserInfo.Contains(':'))
        {
            var (method, password, _) = SplitUserInfo(parsedUrl.UserInfo);
            item.ProtoExtra.SsMethod = method;
            item.Password = password;
        }
        else
        {
            var decodedUserInfo = Utils.Utils.UrlDecode(parsedUrl.UserInfo);
            var userInfo = decodedUserInfo.Contains(':')
                ? decodedUserInfo
                : Utils.Utils.Base64Decode(decodedUserInfo);
            var userInfoParts = userInfo.Split([':'], 2);
            if (userInfoParts.Length != 2)
            {
                return null;
            }
            item.ProtoExtra.SsMethod = userInfoParts[0];
            item.Password = userInfoParts[1];
        }

        var queryParameters = Utils.Utils.ParseQueryString(parsedUrl.Query);
        if (queryParameters["plugin"] is { } pluginStr)
        {
            var pluginParts = pluginStr.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            if (pluginParts.Length == 0)
            {
                return null;
            }

            ApplyPlugin(item, pluginParts[0], string.Join(";", pluginParts.Skip(1)));
        }

        var uot = GetFirstQueryValue(queryParameters, "uot", "udp-over-tcp", "udp_over_tcp");
        if (uot is "1" or "2" || uot.Equals("true", StringComparison.OrdinalIgnoreCase))
            item.ProtoExtra.Uot = true;
        else if (uot is "0" || uot.Equals("false", StringComparison.OrdinalIgnoreCase))
            item.ProtoExtra.Uot = false;

        item.AllowInsecure = ParseAllowInsecure(queryParameters);
        return item;
    }

    private static void ApplyPlugin(ShadowsocksServer item, string? pluginName, string? options)
    {
        if (pluginName.IsNullOrEmpty())
            return;

        // "simple-obfs" and "obfs" alias to "obfs-local".
        if (pluginName is "simple-obfs" or "obfs")
        {
            pluginName = "obfs-local";
        }

        item.ProtoExtra.SsPlugin = pluginName;
        item.ProtoExtra.SsPluginOptions = options ?? string.Empty;

        var pluginParts = (options ?? string.Empty).Split([';'], StringSplitOptions.RemoveEmptyEntries);
        if (pluginName == "obfs-local")
        {
            var obfsMode = pluginParts.FirstOrDefault(t => t.StartsWith("obfs="));
            var obfsHost = pluginParts.FirstOrDefault(t => t.StartsWith("obfs-host="));

            if (obfsMode != null && obfsMode.Contains("obfs=http") && obfsHost.IsNotEmpty())
            {
                item.Network = Constants.DefaultNetwork;
                item.HeaderType = Constants.TcpHeaderHttp;
                item.RequestHost = obfsHost["obfs-host=".Length..];
            }
        }
        else if (pluginName == "v2ray-plugin")
        {
            var mode = pluginParts.FirstOrDefault(t => t.StartsWith("mode="), "mode=websocket");
            var host = pluginParts.FirstOrDefault(t => t.StartsWith("host="));
            var path = pluginParts.FirstOrDefault(t => t.StartsWith("path="));
            var hasTls = pluginParts.Any(t => t == "tls");
            var certRaw = pluginParts.FirstOrDefault(t => t.StartsWith("certRaw="));

            var modeValue = mode.StartsWith("mode=") ? mode["mode=".Length..] : mode;
            if (modeValue == "websocket")
            {
                item.Network = nameof(ETransport.ws);
                if (host.IsNotEmpty())
                {
                    item.RequestHost = host["host=".Length..];
                    item.Sni = item.RequestHost;
                }
                if (path.IsNotEmpty())
                {
                    item.Path = DecodePluginPath(path["path=".Length..]);
                }
            }
            else if (modeValue == "quic")
            {
                item.Network = nameof(ETransport.quic);
            }

            if (hasTls)
            {
                item.StreamSecurity = Constants.StreamSecurity;

                if (certRaw.IsNotEmpty())
                {
                    var certBase64 = certRaw["certRaw=".Length..].Replace("\\=", "=");
                    const string beginMarker = "-----BEGIN CERTIFICATE-----\n";
                    const string endMarker = "\n-----END CERTIFICATE-----";
                    item.Cert = beginMarker + certBase64 + endMarker;
                }
            }
        }
    }
}

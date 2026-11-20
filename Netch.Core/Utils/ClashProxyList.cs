using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Netch.Models;
using Netch.Servers;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Netch.Utils;

internal static class ClashProxyList
{
    internal static List<Server>? TryParse(string text, out int skipped)
    {
        skipped = 0;
        if (!text.Contains("proxies", StringComparison.Ordinal) &&
            !text.Contains("proxy-providers", StringComparison.Ordinal))
            return null;

        var yaml = new YamlStream();
        try
        {
            using var reader = new StringReader(text.TrimStart('\uFEFF'));
            yaml.Load(reader);
        }
        catch (YamlException)
        {
            if (text.GetLines().Any(line =>
                {
                    var end = line.IndexOf("://", StringComparison.Ordinal);
                    return end > 0 && Uri.CheckSchemeName(line[..end]);
                }))
                return null;
            Log.Warning("Could not read Clash YAML");
            return [];
        }

        if (yaml.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode root)
            return null;
        if (!root.Children.TryGetValue(new YamlScalarNode("proxies"), out var list))
            return root.Children.ContainsKey(new YamlScalarNode("proxy-providers")) ? [] : null;
        if (list is not YamlSequenceNode proxies)
            return [];

        var imported = new List<Server>();
        foreach (var proxy in proxies)
        {
            try
            {
                imported.Add(Parse(new Fields(proxy)));
            }
            catch (FormatException)
            {
                skipped++;
            }
        }

        Log.Information("Imported {ServerCount} Clash node(s); skipped {SkippedCount}", imported.Count, skipped);
        return imported;
    }

    private static Server Parse(Fields fields)
    {
        Server server = fields.Text("type") switch
        {
            "ss" => new ShadowsocksServer(),
            "ssr" => new ShadowsocksRServer(),
            "vmess" => new VMessServer(),
            "vless" => new VLESSServer(),
            "trojan" => new TrojanServer(),
            "hysteria2" => new Hysteria2Server(),
            "tuic" => new TUICServer(),
            "wireguard" => new WireGuardServer(),
            "anytls" => new AnyTLSServer(),
            "socks5" => new SocksServer(),
            "http" => new HttpServer(),
            _ => throw new FormatException()
        };

        server.Remarks = fields.Required("name").Trim();
        server.Address = fields.Required("server").Trim();
        server.Port = fields.Number("port") ?? 0;
        if (server.Port is < 1 or > ushort.MaxValue || Uri.CheckHostName(server.Address) == UriHostNameType.Unknown)
            throw new FormatException();

        // UDP follows the native Netch profile. Disabled Clash tuning options do not affect the mapping.
        fields.Flag("udp");
        var smux = fields.Map("smux");
        if (fields.Flag("tfo") || fields.Flag("mptcp") || smux.Flag("enabled") ||
            fields.Text("ip-version") is not (null or "" or "dual") ||
            fields.Text("dialer-proxy") is { Length: > 0 })
            throw new FormatException();

        var extra = server.ProtoExtra;
        switch (server)
        {
            case HttpServer or SocksServer:
                server.Username = fields.Text("username");
                server.Password = fields.Text("password");
                if (server is HttpServer)
                {
                    ReadTls(fields, server, "sni");
                    extra.HttpHeaders = fields.Map("headers").HttpHeaders();
                }
                else
                    extra.Uot = fields.Flag("udp-over-tcp");
                break;
            case ShadowsocksServer:
                ReadShadowsocks(fields, server);
                break;
            case ShadowsocksRServer ssr:
                ssr.Password = fields.Required("password");
                ssr.EncryptMethod = fields.Required("cipher");
                ssr.Protocol = fields.Required("protocol");
                ssr.ProtocolParam = fields.Text("protocol-param");
                ssr.OBFS = fields.Required("obfs");
                ssr.OBFSParam = fields.Text("obfs-param");
                break;
            case VMessServer:
                server.Password = ReadUuid(fields);
                server.MuxEnabled = false;
                if (server is VLESSServer)
                {
                    extra.Flow = fields.Text("flow");
                    extra.VlessEncryption = fields.Text("encryption");
                    // Mihomo's native VLESS profile already uses XUDP.
                    if (fields.Text("packet-encoding") is not (null or "" or "xudp") || fields.Flag("packet-addr"))
                        throw new FormatException();
                    fields.Flag("xudp");
                }
                else
                {
                    extra.AlterId = fields.Number("alterId") ?? 0;
                    extra.VmessSecurity = fields.Text("cipher") ?? "auto";
                    if (extra.AlterId < 0 || fields.Flag("global-padding") || fields.Flag("authenticated-length") ||
                        fields.Text("packet-encoding") is { Length: > 0 })
                        throw new FormatException();
                }
                ReadTls(fields, server, "servername");
                ReadTransport(fields, server);
                break;
            case TrojanServer:
                server.Password = fields.Required("password");
                ReadTls(fields, server, "sni", required: true);
                ReadTransport(fields, server);
                break;
            case Hysteria2Server:
                server.Password = fields.Required("password");
                ReadTls(fields, server, "sni", required: true);
                extra.UpMbps = ReadMbps(fields.Text("up"));
                extra.DownMbps = ReadMbps(fields.Text("down"));
                extra.Ports = fields.Text("ports");
                if (extra.Ports is { Length: > 0 })
                    extra.HopInterval = (fields.Number("hop-interval") ?? 30).ToString(CultureInfo.InvariantCulture);
                var obfs = fields.Text("obfs");
                if (obfs is not (null or ""))
                {
                    if (obfs is not ("salamander" or "gecko"))
                        throw new FormatException();
                    extra.Obfs = obfs;
                    extra.SalamanderPass = fields.Required("obfs-password");
                    if (obfs == "gecko")
                    {
                        extra.GeckoMinPacketSize = fields.Number("obfs-min-packet-size")?.ToString(CultureInfo.InvariantCulture);
                        extra.GeckoMaxPacketSize = fields.Number("obfs-max-packet-size")?.ToString(CultureInfo.InvariantCulture);
                    }
                }
                break;
            case TUICServer:
                server.Username = ReadUuid(fields);
                server.Password = fields.Required("password");
                if (fields.Text("udp-relay-mode") is not (null or "" or "native"))
                    throw new FormatException();
                extra.CongestionControl = fields.Text("congestion-controller") ?? "cubic";
                ReadTls(fields, server, "sni", required: true);
                if (server.Alpn.Length == 0)
                    server.Alpn = "h3";
                break;
            case AnyTLSServer:
                server.Password = fields.Required("password");
                ReadTls(fields, server, "sni", required: true);
                break;
            case WireGuardServer:
                ReadWireGuard(fields, server);
                break;
        }
        fields.EnsureEmpty();
        return server;
    }

    private static void ReadTls(Fields fields, Server server, string nameKey, bool required = false)
    {
        var enabled = fields.Flag("tls") || required;
        server.Sni = fields.Text(nameKey);
        server.AllowInsecure = fields.Flag("skip-cert-verify");
        server.Alpn = string.Join(",", fields.List("alpn"));
        server.Fingerprint = fields.Text("client-fingerprint");
        var reality = fields.Map("reality-opts");
        if (!reality.IsEmpty)
        {
            if (!enabled || server is not (VMessServer or TrojanServer))
                throw new FormatException();
            server.PublicKey = reality.Required("public-key");
            server.ShortId = reality.Text("short-id");
            reality.EnsureEmpty();
            server.StreamSecurity = "reality";
        }
        else
            server.StreamSecurity = enabled ? "tls" : "none";
    }

    private static void ReadTransport(Fields fields, Server server)
    {
        server.Network = fields.Text("network") ?? "tcp";
        switch (server.Network)
        {
            case "" or "tcp":
                break;
            case "ws":
                var ws = fields.Map("ws-opts");
                server.RequestHost = ws.Map("headers").Host();
                server.Path = ws.Text("path") ?? "/";
                var upgrade = ws.Flag("v2ray-http-upgrade");
                var earlyData = ws.Number("max-early-data") ?? 0;
                var earlyHeader = ws.Text("early-data-header-name");
                if (ws.Flag("v2ray-http-upgrade-fast-open") || earlyData < 0 ||
                    (earlyData > 0 && (upgrade || !string.Equals(earlyHeader, "Sec-WebSocket-Protocol", StringComparison.OrdinalIgnoreCase))))
                    throw new FormatException();
                if (upgrade)
                    server.Network = "httpupgrade";
                else if (earlyData > 0)
                    server.Path += $"?ed={earlyData.ToString(CultureInfo.InvariantCulture)}";
                ws.EnsureEmpty();
                break;
            case "grpc":
                var grpc = fields.Map("grpc-opts");
                server.Path = grpc.Text("grpc-service-name");
                grpc.EnsureEmpty();
                break;
            case "h2" when server is VMessServer:
                var h2 = fields.Map("h2-opts");
                server.RequestHost = string.Join(",", h2.List("host"));
                server.Path = h2.Text("path");
                h2.EnsureEmpty();
                break;
            case "xhttp" when server is VLESSServer:
                var xhttp = fields.Map("xhttp-opts");
                server.RequestHost = xhttp.Text("host");
                server.Path = xhttp.Text("path");
                server.HeaderType = xhttp.Text("mode") ?? "auto";
                if (!Constants.XhttpMode.Contains(server.HeaderType))
                    throw new FormatException();
                xhttp.EnsureEmpty();
                break;
            default:
                throw new FormatException();
        }
    }

    private static void ReadShadowsocks(Fields fields, Server server)
    {
        var extra = server.ProtoExtra;
        extra.SsMethod = fields.Required("cipher");
        if (!ShadowsocksUtil.IsSupportedCipher(extra.SsMethod))
            throw new FormatException();
        server.Password = fields.Text("password");
        if (server.Password == null || server.Password.Length == 0 && extra.SsMethod is not ("none" or "plain"))
            throw new FormatException();
        extra.Uot = fields.Flag("udp-over-tcp");
        var uotVersion = fields.Number("udp-over-tcp-version");
        if (extra.Uot == true && uotVersion != 2)
            throw new FormatException();

        var plugin = fields.Text("plugin");
        var options = fields.Map("plugin-opts");
        var host = options.Text("host");
        if (host?.Contains(';') == true)
            throw new FormatException();
        switch (plugin)
        {
            case null or "":
                if (host is { Length: > 0 })
                    throw new FormatException();
                break;
            case "obfs":
                extra.SsPlugin = "obfs-local";
                var mode = options.Text("mode") ?? "http";
                if (mode is not ("http" or "tls"))
                    throw new FormatException();
                extra.SsPluginOptions = host is { Length: > 0 } ? $"obfs={mode};obfs-host={host}" : $"obfs={mode}";
                break;
            case "v2ray-plugin":
                if (options.Text("mode") is not (null or "websocket"))
                    throw new FormatException();
                extra.SsPlugin = "v2ray-plugin";
                var args = new List<string> { "mode=websocket", options.Flag("mux") ? "mux=1" : "mux=0" };
                if (host is { Length: > 0 })
                    args.Add($"host={host}");
                if (options.Text("path") is { Length: > 0 } path)
                {
                    if (path.Contains(';'))
                        throw new FormatException();
                    args.Add($"path={ShadowsocksUtil.EncodePluginPath(path)}");
                }
                if (options.Flag("tls"))
                    args.Add("tls");
                extra.SsPluginOptions = string.Join(";", args);
                server.Network = "ws";
                server.AllowInsecure = options.Flag("skip-cert-verify");
                break;
            default:
                throw new FormatException();
        }
        options.EnsureEmpty();
    }

    private static void ReadWireGuard(Fields fields, Server server)
    {
        var extra = server.ProtoExtra;
        var ipv4 = fields.Text("ip");
        var ipv6 = fields.Text("ipv6");
        if (ipv4 == null && ipv6 == null ||
            ipv4 != null && !IsInterfaceAddress(ipv4, AddressFamily.InterNetwork, 32) ||
            ipv6 != null && !IsInterfaceAddress(ipv6, AddressFamily.InterNetworkV6, 128))
            throw new FormatException();
        if (fields.List("allowed-ips").Any(ip => ip is not ("0.0.0.0/0" or "::/0")) ||
            fields.Flag("remote-dns-resolve"))
            throw new FormatException();
        extra.WgInterfaceAddress = ipv4 == null || ipv6 == null ? ipv4 ?? ipv6 : $"{ipv4},{ipv6}";
        server.Password = fields.Required("private-key");
        extra.WgPublicKey = fields.Required("public-key");
        extra.WgPresharedKey = fields.Text("pre-shared-key");
        extra.WgMtu = fields.Number("mtu");
        var reserved = fields.List("reserved");
        if (reserved.Length != 0)
        {
            if (reserved.Length != 3 || reserved.Any(value => !byte.TryParse(value, out _)))
                throw new FormatException();
            extra.WgReserved = string.Join(",", reserved);
        }
    }

    private static bool IsInterfaceAddress(string value, AddressFamily family, int maxPrefix)
    {
        var parts = value.Split('/', 2);
        return IPAddress.TryParse(parts[0], out var address) && address.AddressFamily == family &&
            (parts.Length == 1 || int.TryParse(parts[1], out var prefix) && prefix >= 0 && prefix <= maxPrefix);
    }

    private static string ReadUuid(Fields fields) =>
        Guid.TryParse(fields.Text("uuid"), out var id) ? id.ToString() : throw new FormatException();

    private static int ReadMbps(string? value)
    {
        if (value == null)
            return 0;
        if (value.EndsWith("Mbps", StringComparison.OrdinalIgnoreCase))
            value = value[..^4];
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var speed) && speed >= 0
            ? speed : throw new FormatException();
    }

    private sealed class Fields
    {
        private readonly Dictionary<string, YamlNode> _nodes = new(StringComparer.Ordinal);

        internal Fields(YamlNode? node)
        {
            if (node == null)
                return;
            if (node is not YamlMappingNode mapping)
                throw new FormatException();
            foreach (var (key, value) in mapping.Children)
                if (!_nodes.TryAdd(Scalar(key), value))
                    throw new FormatException();
        }

        internal bool IsEmpty => _nodes.Count == 0;
        internal void EnsureEmpty()
        {
            if (!IsEmpty)
                throw new FormatException();
        }

        internal string? Text(string key) => Take(key) is { } node ? Scalar(node) : null;
        internal string Required(string key) =>
            Text(key) is { } value && !string.IsNullOrWhiteSpace(value) ? value : throw new FormatException();
        internal Fields Map(string key) => new(Take(key));
        internal string[] List(string key) => Take(key) switch
        {
            null => [],
            YamlSequenceNode sequence => sequence.Select(Scalar).ToArray(),
            _ => throw new FormatException()
        };

        internal bool Flag(string key) => Text(key) is { } text &&
            (bool.TryParse(text, out var value) ? value : throw new FormatException());

        internal int? Number(string key) => Text(key) is { } text
            ? int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : throw new FormatException()
            : null;

        internal string? Host()
        {
            var key = _nodes.Keys.FirstOrDefault(key => key.Equals("Host", StringComparison.OrdinalIgnoreCase));
            var host = key == null ? null : Text(key);
            EnsureEmpty();
            return host;
        }

        internal string HttpHeaders() => string.Join("\n", _nodes.Select(pair => $"{pair.Key}: {Scalar(pair.Value)}"));

        private YamlNode? Take(string key) => _nodes.Remove(key, out var node) ? node : null;
        private static string Scalar(YamlNode node) =>
            node is YamlScalarNode { Value: { } value } ? value : throw new FormatException();
    }
}

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Netch.Enums;
using Netch.Models;
using Netch.Servers;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Netch.Utils;

internal static class ClashProxyList
{
    private static readonly JsonSerializerOptions ExtraJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    internal static bool IsClashYaml(string text)
    {
        if (!text.Contains("proxies", StringComparison.Ordinal) &&
            !text.Contains("proxy-providers", StringComparison.Ordinal))
            return false;

        var yaml = new YamlStream();
        try
        {
            using var reader = new StringReader(text.TrimStart('\uFEFF'));
            yaml.Load(reader);
        }
        catch (YamlException)
        {
            return false;
        }

        if (yaml.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode root)
            return false;

        if (root.Children.TryGetValue(new YamlScalarNode("proxies"), out var list))
            return list is YamlSequenceNode;

        return root.Children.ContainsKey(new YamlScalarNode("proxy-providers"));
    }

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
            "naive" => new NaiveServer(),
            "masque" => new MASQUEServer(),
            "trusttunnel" => new TrustTunnelServer(),
            "mieru" => new MieruServer(),
            "shadowquic" => new ShadowQUICServer(),
            "socks5" => new SocksServer(),
            "http" => new HttpServer(),
            "ssh" => new SSHServer(),
            _ => throw new FormatException()
        };

        server.Remarks = fields.Required("name").Trim();
        if (server is not WireGuardServer)
        {
            server.Address = fields.Required("server").Trim();
            if (server is not MieruServer)
                server.Port = fields.Number("port") ?? (server is SSHServer ? 22 : 0);
        }

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
                {
                    if (fields.Flag("tls"))
                        throw new FormatException();
                    fields.Flag("skip-cert-verify");
                    extra.Uot = fields.Flag("udp-over-tcp");
                }
                break;
            case ShadowsocksServer ss:
                server = ReadShadowsocksOrShadowTls(fields, ss);
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
                        fields.Text("packet-encoding") is not (null or "" or "xudp"))
                        throw new FormatException();
                }
                ReadTls(fields, server, "servername");
                ReadTransport(fields, server);
                break;
            case TrojanServer:
                server.Password = fields.Required("password");
                ReadTls(fields, server, "sni", required: true, allowDisableTls: true);
                ReadTransport(fields, server);
                break;
            case Hysteria2Server:
                server.Password = fields.Required("password");
                ReadTls(fields, server, "sni", required: true);
                extra.UpMbps = ReadMbps(fields.Text("up"));
                extra.DownMbps = ReadMbps(fields.Text("down"));
                extra.Ports = fields.Text("ports");
                uint? minHopVal = null;
                uint? maxHopVal = null;
                var rawHopInterval = fields.Text("hop-interval");
                if (rawHopInterval is { Length: > 0 })
                {
                    var dash = rawHopInterval.IndexOf('-');
                    if (dash > 0)
                    {
                        if (!Hysteria2Util.TryParseHopSeconds(rawHopInterval[..dash], out var minVal) ||
                            !Hysteria2Util.TryParseHopSeconds(rawHopInterval[(dash + 1)..], out var maxVal) ||
                            maxVal < minVal)
                            throw new FormatException();
                        minHopVal = minVal;
                        maxHopVal = maxVal;
                    }
                    else if (!Hysteria2Util.TryParseHopSeconds(rawHopInterval, out var singleHop))
                    {
                        throw new FormatException();
                    }
                    else
                    {
                        minHopVal = singleHop;
                    }
                }
                var hopIntervalMax = fields.Text("hop-interval-max");
                if (hopIntervalMax is { Length: > 0 })
                {
                    if (!Hysteria2Util.TryParseHopSeconds(hopIntervalMax, out var explicitMax) ||
                        (minHopVal.HasValue && explicitMax < minHopVal.Value))
                        throw new FormatException();
                    maxHopVal = explicitMax;
                }
                if (extra.Ports is { Length: > 0 })
                {
                    if (maxHopVal.HasValue)
                    {
                        extra.HopInterval = (minHopVal ?? Math.Min(30u, maxHopVal.Value)).ToString(CultureInfo.InvariantCulture);
                        extra.HopIntervalMax = maxHopVal.Value.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        extra.HopInterval = (minHopVal ?? 30u).ToString(CultureInfo.InvariantCulture);
                    }
                    if (server.Port == 0)
                        server.Port = ParseFirstPort(extra.Ports);
                }
                else if (maxHopVal.HasValue)
                {
                    throw new FormatException();
                }
                var bbrProfile = fields.Text("bbr-profile");
                if (bbrProfile is { Length: > 0 })
                {
                    var normalizedBbr = bbrProfile.Trim().ToLowerInvariant();
                    if (!Constants.Hysteria2BbrProfiles.Contains(normalizedBbr))
                        throw new FormatException();
                    extra.BbrProfile = normalizedBbr;
                }
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
                var udpRelayMode = fields.Text("udp-relay-mode");
                if (udpRelayMode is not (null or "" or "native" or "quic"))
                    throw new FormatException();
                if (udpRelayMode is { Length: > 0 })
                    extra.TuicUdpRelayMode = udpRelayMode;
                if (fields.Flag("reduce-rtt"))
                    extra.TuicReduceRtt = true;
                if (fields.Flag("disable-sni"))
                    extra.TuicDisableSni = true;
                extra.CongestionControl = fields.Text("congestion-controller") ?? "cubic";
                ReadTls(fields, server, "sni", required: true);
                if (server.Alpn.Length == 0)
                    server.Alpn = "h3";
                break;
            case AnyTLSServer:
                server.Password = fields.Required("password");
                ReadTls(fields, server, "sni", required: true);
                extra.AnyTlsIdleSessionCheckIntervalSeconds = fields.Number("idle-session-check-interval");
                extra.AnyTlsIdleSessionTimeoutSeconds = fields.Number("idle-session-timeout");
                extra.AnyTlsMinIdleSession = fields.Number("min-idle-session");
                if (extra.AnyTlsIdleSessionCheckIntervalSeconds is < 0 || extra.AnyTlsIdleSessionTimeoutSeconds is < 0 ||
                    extra.AnyTlsMinIdleSession is < 0)
                    throw new FormatException();
                break;
            case NaiveServer:
                server.Username = fields.Text("username");
                server.Password = fields.Required("password");
                ReadTls(fields, server, "sni", required: true);
                extra.NaiveQuic = fields.Flag("quic");
                extra.CongestionControl = fields.Text("quic-congestion-control");
                extra.InsecureConcurrency = fields.Number("insecure-concurrency");
                if (extra.InsecureConcurrency is < 0)
                    throw new FormatException();
                extra.Uot = fields.Flag("udp-over-tcp");
                break;
            case MASQUEServer:
                server.Username = fields.Text("username");
                server.Password = fields.Text("password") ?? string.Empty;
                ReadTls(fields, server, "sni", required: true);
                extra.MasquePath = fields.Text("path");
                var masqueHeaders = fields.Map("headers").HttpHeaders();
                if (masqueHeaders.Length > 0)
                    extra.MasqueHeaders = masqueHeaders;
                break;
            case TrustTunnelServer:
                ReadTrustTunnel(fields, server);
                break;
            case MieruServer:
                ReadMieru(fields, server);
                break;
            case ShadowQUICServer:
                ReadShadowQuic(fields, server);
                break;
            case WireGuardServer wg:
                server = ReadWireGuardOrAmneziaWg(fields, wg);
                break;
            case SSHServer:
                ReadSsh(fields, server);
                break;
        }

        if (string.IsNullOrWhiteSpace(server.Address) || server.Port is < 1 or > ushort.MaxValue ||
            Uri.CheckHostName(server.Address) == UriHostNameType.Unknown)
            throw new FormatException();

        fields.EnsureEmpty();
        if (server is TrustTunnelServer or MieruServer or ShadowQUICServer or AmneziaWGServer &&
            ServerValidator.Validate(server) != null)
            throw new FormatException();
        return server;
    }

    private static void ReadTls(Fields fields, Server server, string nameKey, bool required = false, bool allowDisableTls = false)
    {
        var tlsFlag = fields.FlagOptional("tls");
        var enabled = allowDisableTls && tlsFlag == false ? false : (tlsFlag == true || required);
        var altKey = nameKey == "sni" ? "servername" : "sni";
        var primarySni = fields.Text(nameKey);
        var altSni = fields.Text(altKey);
        if (primarySni is { Length: > 0 } && altSni is { Length: > 0 } && !string.Equals(primarySni, altSni, StringComparison.Ordinal))
            throw new FormatException();
        server.Sni = primarySni is { Length: > 0 } ? primarySni : altSni;
        server.AllowInsecure = fields.Flag("skip-cert-verify");
        server.Alpn = string.Join(",", fields.List("alpn"));
        server.Fingerprint = fields.Text("client-fingerprint");
        server.Cert = fields.Text("certificate");
        var ech = fields.Map("ech-opts");
        if (!ech.IsEmpty)
        {
            var echEnabled = ech.Flag("enable");
            var echConfig = ech.Text("config");
            ech.EnsureEmpty();
            if (echEnabled && !string.IsNullOrWhiteSpace(echConfig))
                server.EchConfigList = echConfig;
        }
        var reality = fields.Map("reality-opts");
        if (!reality.IsEmpty)
        {
            if (!enabled || server is not (VMessServer or TrojanServer))
                throw new FormatException();
            server.PublicKey = reality.Required("public-key");
            server.ShortId = reality.Text("short-id");
            server.SpiderX = reality.Text("spider-x");
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
            case "h2":
                var h2 = fields.Map("h2-opts");
                server.RequestHost = string.Join(",", h2.List("host"));
                server.Path = h2.Text("path");
                h2.EnsureEmpty();
                break;
            case "xhttp" when server is VMessServer or TrojanServer:
                var xhttp = fields.Map("xhttp-opts");
                server.RequestHost = xhttp.Text("host");
                server.Path = xhttp.Text("path");
                server.HeaderType = xhttp.Text("mode") ?? "auto";
                if (!Constants.XhttpMode.Contains(server.HeaderType))
                    throw new FormatException();
                var extraNode = xhttp.TakeNode("extra");
                if (extraNode is YamlScalarNode scalarExtra)
                    server.Extra = scalarExtra.Value;
                else if (extraNode is YamlMappingNode mappingExtra)
                    server.Extra = YamlToJsonNode(mappingExtra)?.ToJsonString(ExtraJsonOptions);
                else if (extraNode != null)
                    throw new FormatException();
                xhttp.EnsureEmpty();
                break;
            case "mkcp" or "kcp" when server.ConfigType == EConfigType.VMess:
                server.Network = nameof(ETransport.kcp);
                var mkcp = fields.Map("mkcp-opts");
                if (mkcp.IsEmpty)
                    mkcp = fields.Map("kcp-opts");
                server.Path = mkcp.Text("seed");
                server.HeaderType = mkcp.Text("header") ?? Constants.None;
                mkcp.Number("mtu");
                mkcp.Number("tti");
                mkcp.Number("uplink-capacity");
                mkcp.Number("downlink-capacity");
                mkcp.Flag("congestion");
                mkcp.Number("read-buffer");
                mkcp.Number("write-buffer");
                mkcp.EnsureEmpty();
                break;
            case "quic":
                var quic = fields.Map("quic-opts");
                quic.EnsureEmpty();
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
            case "obfs" or "obfs-local" or "simple-obfs":
                extra.SsPlugin = "obfs-local";
                var mode = options.Text("mode") ?? "http";
                if (mode is not ("http" or "tls"))
                    throw new FormatException();
                extra.SsPluginOptions = host is { Length: > 0 } ? $"obfs={mode};obfs-host={host}" : $"obfs={mode}";
                break;
            case "v2ray-plugin":
                var pluginMode = options.Text("mode") ?? "websocket";
                if (pluginMode is not ("websocket" or "quic"))
                    throw new FormatException();
                extra.SsPlugin = "v2ray-plugin";
                var args = new List<string> { $"mode={pluginMode}", options.Flag("mux") ? "mux=1" : "mux=0" };
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
                server.Network = pluginMode == "quic" ? nameof(ETransport.quic) : nameof(ETransport.ws);
                server.AllowInsecure = options.Flag("skip-cert-verify");
                break;
            default:
                throw new FormatException();
        }
        options.EnsureEmpty();
    }

    private static Server ReadShadowsocksOrShadowTls(Fields fields, ShadowsocksServer ss)
    {
        var plugin = fields.PeekText("plugin");
        if (!string.Equals(plugin, "shadow-tls", StringComparison.OrdinalIgnoreCase))
        {
            ReadShadowsocks(fields, ss);
            return ss;
        }

        fields.Text("plugin");
        var stls = new ShadowTLSServer
        {
            Remarks = ss.Remarks,
            Address = ss.Address,
            Port = ss.Port,
            Password = fields.Text("password") ?? string.Empty
        };
        var extra = stls.ProtoExtra;
        extra.SsMethod = fields.Required("cipher");
        if (!ShadowsocksUtil.IsSupportedCipher(extra.SsMethod) ||
            !ServerValidator.IsValidShadowsocksPassword(extra.SsMethod, stls.Password))
            throw new FormatException();

        extra.Uot = fields.FlagOptional("udp-over-tcp") ?? fields.FlagOptional("uot");
        var uotVersion = fields.Number("udp-over-tcp-version");
        if (uotVersion is not (null or 2))
            throw new FormatException();

        var hasTopSkipCertVerify = fields.PeekText("skip-cert-verify") != null;
        var opts = fields.Map("plugin-opts");
        var versionText = opts.Text("version");
        var version = 3;
        if (versionText is { Length: > 0 } &&
            (!int.TryParse(versionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out version) || version is not (1 or 2 or 3)))
        {
            throw new FormatException();
        }
        extra.ShadowTlsVersion = version;
        extra.ShadowTlsPassword = opts.Text("password");
        if (version >= 2 && string.IsNullOrEmpty(extra.ShadowTlsPassword))
            throw new FormatException();

        var pluginHost = opts.Text("host");
        var pluginAlpn = opts.ListOrScalar("alpn");
        var pluginSkipCertVerify = opts.FlagOptional("skip-cert-verify");
        var pluginFp = opts.Text("client-fingerprint");
        opts.EnsureEmpty();

        ReadTls(fields, stls, "servername", required: true);
        if (string.IsNullOrEmpty(stls.Sni) && pluginHost is { Length: > 0 })
            stls.Sni = pluginHost;
        if (string.IsNullOrEmpty(stls.Alpn) && pluginAlpn.Length > 0)
            stls.Alpn = string.Join(",", pluginAlpn);
        if (!hasTopSkipCertVerify && pluginSkipCertVerify.HasValue)
            stls.AllowInsecure = pluginSkipCertVerify.Value;
        if (string.IsNullOrEmpty(stls.Fingerprint) && pluginFp is { Length: > 0 })
            stls.Fingerprint = pluginFp;

        return stls;
    }

    private static void ReadSsh(Fields fields, Server server)
    {
        var extra = server.ProtoExtra;
        server.Username = fields.Required("username");
        server.Password = fields.Text("password") ?? string.Empty;
        var privateKey = fields.Text("private-key");
        if (privateKey is { Length: > 0 })
        {
            if (!ServerValidator.IsValidSshPrivateKey(privateKey))
                throw new FormatException();
            extra.SshPrivateKey = ServerValidator.NormalizeSshPrivateKey(privateKey);
        }
        var passphraseDash = fields.Text("private-key-passphrase");
        var passphraseUnderscore = fields.Text("private_key_passphrase");
        if (passphraseDash is { Length: > 0 } && passphraseUnderscore is { Length: > 0 } &&
            !string.Equals(passphraseDash, passphraseUnderscore, StringComparison.Ordinal))
            throw new FormatException();
        extra.SshPrivateKeyPassphrase = passphraseDash is { Length: > 0 } ? passphraseDash : passphraseUnderscore;
        if (string.IsNullOrEmpty(server.Password) && string.IsNullOrEmpty(extra.SshPrivateKey))
            throw new FormatException();

        var hostKeys = fields.ListOrScalar("host-key");
        if (hostKeys.Length > 0)
            extra.SshHostKey = string.Join("\n", hostKeys);
        var hostKeyAlgos = fields.ListOrScalar("host-key-algorithms");
        if (hostKeyAlgos.Length > 0)
            extra.SshHostKeyAlgorithms = string.Join(",", hostKeyAlgos);
        var clientVersion = fields.Text("client-version") ?? fields.Text("client_version");
        if (clientVersion is { Length: > 0 })
            extra.SshClientVersion = clientVersion.Trim();
    }

    private static void ReadTrustTunnel(Fields fields, Server server)
    {
        var extra = server.ProtoExtra;
        server.Username = fields.Text("username") ?? string.Empty;
        server.Password = fields.Text("password") ?? string.Empty;
        extra.TrustTunnelNameCertVerify = fields.Text("name-cert-verify");
        extra.TrustTunnelHealthCheck = fields.FlagOptional("health-check");
        extra.TrustTunnelQuic = fields.FlagOptional("quic");
        extra.CongestionControl = fields.Text("congestion-controller")?.Trim().ToLowerInvariant();
        extra.BbrProfile = fields.Text("bbr-profile")?.Trim().ToLowerInvariant();
        extra.TrustTunnelMaxConnections = fields.Number("max-connections");
        extra.TrustTunnelMinStreams = fields.Number("min-streams");
        extra.TrustTunnelMaxStreams = fields.Number("max-streams");
        ReadTls(fields, server, "sni", required: true);
    }

    private static void ReadMieru(Fields fields, Server server)
    {
        var extra = server.ProtoExtra;
        server.Username = fields.Required("username");
        server.Password = fields.Required("password");
        var transport = fields.Required("transport").Trim().ToUpperInvariant();
        if (transport is not ("TCP" or "UDP"))
            throw new FormatException();
        extra.MieruTransport = transport;

        var port = fields.Number("port");
        var portRange = fields.Text("port-range");
        if (port.HasValue && portRange is { Length: > 0 })
            throw new FormatException();

        if (portRange is { Length: > 0 })
        {
            if (!ServerValidator.IsValidSinglePortRange(portRange, out var startPort, out var endPort))
                throw new FormatException();
            extra.MieruPortRange = $"{startPort.ToString(CultureInfo.InvariantCulture)}-{endPort.ToString(CultureInfo.InvariantCulture)}";
            server.Port = startPort;
        }
        else
        {
            server.Port = port ?? 0;
        }

        var mux = fields.Text("multiplexing");
        if (mux is { Length: > 0 })
            extra.MieruMultiplexing = mux.Trim().ToUpperInvariant();

        var handshake = fields.Text("handshake-mode");
        if (handshake is { Length: > 0 })
            extra.MieruHandshakeMode = handshake.Trim().ToUpperInvariant();

        var trafficPattern = fields.Text("traffic-pattern");
        if (trafficPattern is { Length: > 0 })
            extra.MieruTrafficPattern = trafficPattern.Trim();
    }

    private static void ReadShadowQuic(Fields fields, Server server)
    {
        var extra = server.ProtoExtra;
        server.Username = fields.Required("username");
        server.Password = fields.Required("password");
        var sni = fields.Text("sni");
        var serverName = fields.Text("servername");
        if (sni is { Length: > 0 } && serverName is { Length: > 0 } && !string.Equals(sni, serverName, StringComparison.Ordinal))
            throw new FormatException();
        server.Sni = sni is { Length: > 0 } ? sni : serverName;
        var alpn = fields.ListOrScalar("alpn");
        if (alpn.Length > 0)
            server.Alpn = string.Join(",", alpn);
        var versions = fields.ListOrScalar("quic-versions");
        if (versions.Length > 0)
            extra.ShadowQuicVersions = string.Join(",", versions.Select(v => v.Trim().ToLowerInvariant()));
        extra.ShadowQuicUdpOverStream = fields.FlagOptional("udp-over-stream");
        extra.ShadowQuicZeroRtt = fields.FlagOptional("zero-rtt");
        extra.ShadowQuicKeepAliveInterval = fields.Number("keep-alive-interval");
        extra.CongestionControl = fields.Text("congestion-controller")?.Trim().ToLowerInvariant();
        extra.BbrProfile = fields.Text("bbr-profile")?.Trim().ToLowerInvariant();
        if (fields.Text("up") is { Length: > 0 } upRaw)
            extra.UpMbps = ReadMbps(upRaw);
        if (fields.Text("down") is { Length: > 0 } downRaw)
            extra.DownMbps = ReadMbps(downRaw);
        extra.ShadowQuicCwnd = fields.Number("cwnd");
        extra.ShadowQuicMaxDatagramFrameSize = fields.Number("max-datagram-frame-size");
        extra.ShadowQuicMaxOpenStreams = fields.Number("max-open-streams");
        extra.ShadowQuicRecvWindowConn = fields.Number("recv-window-conn");
        extra.ShadowQuicRecvWindow = fields.Number("recv-window");
        extra.ShadowQuicDisableMtuDiscovery = fields.FlagOptional("disable-mtu-discovery");
    }

    private static Server ReadWireGuardOrAmneziaWg(Fields fields, WireGuardServer server)
    {
        var extra = server.ProtoExtra;
        server.Address = fields.Text("server")?.Trim() ?? string.Empty;
        server.Port = fields.Number("port") ?? 0;

        Fields? peer = null;
        if (fields.TakeNode("peers") is { } peersNode)
        {
            if (peersNode is not YamlSequenceNode seq || seq.Children.Count != 1)
                throw new FormatException();
            peer = new Fields(seq.Children[0]);
            if (string.IsNullOrWhiteSpace(server.Address))
                server.Address = peer.Required("server").Trim();
            if (server.Port == 0)
                server.Port = peer.Number("port") ?? 0;
        }

        var ipv4 = fields.Text("ip");
        var ipv6 = fields.Text("ipv6");
        if (ipv4 == null && ipv6 == null ||
            ipv4 != null && !IsInterfaceAddress(ipv4, AddressFamily.InterNetwork, 32) ||
            ipv6 != null && !IsInterfaceAddress(ipv6, AddressFamily.InterNetworkV6, 128))
            throw new FormatException();
        var allowedIps = peer != null ? peer.List("allowed-ips") : fields.List("allowed-ips");
        if (allowedIps.Length == 0 && peer != null)
            allowedIps = fields.List("allowed-ips");
        if (allowedIps.Any(ip => ip is not ("0.0.0.0/0" or "::/0")) ||
            fields.Flag("remote-dns-resolve"))
            throw new FormatException();
        var topPublicKey = fields.Text("public-key");
        var topPsk = fields.Text("pre-shared-key");
        var topReserved = fields.TakeNode("reserved");

        extra.WgInterfaceAddress = ipv4 == null || ipv6 == null ? ipv4 ?? ipv6 : $"{ipv4},{ipv6}";
        server.Password = fields.Required("private-key");
        extra.WgPublicKey = peer?.Text("public-key") ?? topPublicKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(extra.WgPublicKey))
            throw new FormatException();
        extra.WgPresharedKey = peer?.Text("pre-shared-key") ?? topPsk;
        extra.WgMtu = fields.Number("mtu");
        var reservedNode = peer?.TakeNode("reserved") ?? topReserved;
        if (reservedNode is YamlSequenceNode reservedSeq)
        {
            var reserved = reservedSeq.Select(Fields.Scalar).ToArray();
            if (reserved.Length != 3 || reserved.Any(value => !byte.TryParse(value, out _)))
                throw new FormatException();
            extra.WgReserved = string.Join(",", reserved);
        }
        else if (reservedNode is YamlScalarNode { Value: { Length: > 0 } scalarReserved })
        {
            if (!ServerValidator.TryNormalizeWireGuardReserved(scalarReserved, out var normalized))
                throw new FormatException();
            extra.WgReserved = normalized;
        }
        else if (reservedNode != null)
            throw new FormatException();

        var awg = fields.Map("amnezia-wg-option");
        peer?.EnsureEmpty();
        if (awg.IsEmpty)
            return server;

        extra.AwgVersion = awg.Number("version");
        extra.AwgJc = awg.Number("jc");
        extra.AwgJmin = awg.Number("jmin");
        extra.AwgJmax = awg.Number("jmax");
        extra.AwgS1 = awg.Number("s1");
        extra.AwgS2 = awg.Number("s2");
        extra.AwgS3 = awg.Number("s3");
        extra.AwgS4 = awg.Number("s4");
        extra.AwgH1 = awg.Text("h1");
        extra.AwgH2 = awg.Text("h2");
        extra.AwgH3 = awg.Text("h3");
        extra.AwgH4 = awg.Text("h4");
        extra.AwgI1 = awg.Text("i1");
        extra.AwgI2 = awg.Text("i2");
        extra.AwgI3 = awg.Text("i3");
        extra.AwgI4 = awg.Text("i4");
        extra.AwgI5 = awg.Text("i5");
        extra.AwgHeaderProtectionKey = awg.Text("header-protection-key");
        extra.AwgContentPaddingAddition = awg.Text("content-padding-addition");
        extra.AwgRekeyAfterTime = awg.Text("rekey-after-time");
        extra.AwgRekeyTimeout = awg.Text("rekey-timeout");
        extra.AwgRejectAfterTime = awg.Text("reject-after-time");
        extra.AwgKeepaliveTimeout = awg.Text("keepalive-timeout");
        extra.AwgMaxHandshakeAttempts = awg.Text("max-handshake-attempts");
        extra.AwgRandomTrailers = awg.FlagOptional("random-trailers");
        extra.AwgDisableCookies = awg.FlagOptional("disable-cookies");
        awg.EnsureEmpty();

        return new AmneziaWGServer
        {
            Remarks = server.Remarks,
            Address = server.Address,
            Port = server.Port,
            Password = server.Password,
            ProtoExtra = extra
        };
    }

    private static int ParseFirstPort(string ports)
    {
        var first = ports.Split([',', '-', ':'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) && port is >= 1 and <= ushort.MaxValue
            ? port
            : throw new FormatException();
    }

    private static JsonNode? YamlToJsonNode(YamlNode node) => node switch
    {
        YamlMappingNode mapping => new JsonObject(mapping.Children.Select(pair =>
            new KeyValuePair<string, JsonNode?>(Fields.Scalar(pair.Key), YamlToJsonNode(pair.Value)))),
        YamlSequenceNode sequence => new JsonArray(sequence.Children.Select(YamlToJsonNode).ToArray()),
        YamlScalarNode { Value: null } => null,
        YamlScalarNode scalar when scalar.Style == ScalarStyle.Plain && bool.TryParse(scalar.Value, out var b) => JsonValue.Create(b),
        YamlScalarNode scalar when scalar.Style == ScalarStyle.Plain && long.TryParse(scalar.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) => JsonValue.Create(l),
        YamlScalarNode scalar when scalar.Style == ScalarStyle.Plain && double.TryParse(scalar.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => JsonValue.Create(d),
        YamlScalarNode scalar => JsonValue.Create(scalar.Value),
        _ => throw new FormatException()
    };

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
        return Hysteria2Util.TryParseMbps(value) ?? throw new FormatException();
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

        internal string? PeekText(string key) => _nodes.TryGetValue(key, out var node) ? Scalar(node) : null;
        internal string? Text(string key) => Take(key) is { } node ? Scalar(node) : null;
        internal string Required(string key) =>
            Text(key) is { } value && !string.IsNullOrWhiteSpace(value) ? value : throw new FormatException();
        internal Fields Map(string key) => new(Take(key));
        internal YamlNode? TakeNode(string key) => Take(key);
        internal string[] List(string key) => Take(key) switch
        {
            null => [],
            YamlSequenceNode sequence => sequence.Select(Scalar).ToArray(),
            _ => throw new FormatException()
        };

        internal string[] ListOrScalar(string key) => Take(key) switch
        {
            null => [],
            YamlSequenceNode sequence => sequence.Select(Scalar).ToArray(),
            YamlScalarNode { Value: { } value } => string.IsNullOrWhiteSpace(value) ? [] : [value],
            _ => throw new FormatException()
        };

        internal bool? FlagOptional(string key) => Text(key) is { } text
            ? (bool.TryParse(text, out var value) ? value : throw new FormatException())
            : null;

        internal bool Flag(string key) => FlagOptional(key) ?? false;

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
        internal static string Scalar(YamlNode node) =>
            node is YamlScalarNode { Value: { } value } ? value : throw new FormatException();
    }
}

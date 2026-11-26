using System.Net;
using System.Net.Sockets;
using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

internal static class MihomoConfig
{
    internal static Dictionary<string, object?> Build(Server server, string upstream, string listen, ushort port)
    {
        return new()
        {
            ["log-level"] = "info",
            ["mode"] = "rule",
            ["ipv6"] = true,
            ["listeners"] = new[] { new { name = "netch", type = "mixed", listen, port = port.ToString(), udp = true } },
            ["proxies"] = new[] { BuildProxy(server, upstream) },
            ["rules"] = new[] { "MATCH,upstream", "MATCH,REJECT" }
        };
    }

    internal static Dictionary<string, object?> BuildBatchProbeConfig(
        IReadOnlyList<(string Tag, Server Server, string Upstream)> entries,
        ushort listenPort,
        string secret)
    {
        var built = entries.Select(e => (e.Tag, Proxy: BuildProxy(e.Server, e.Upstream, e.Tag))).ToList();
        return BuildBatchProbeConfig(built, listenPort, secret);
    }

    internal static Dictionary<string, object?> BuildBatchProbeConfig(
        IReadOnlyList<(string Tag, Dictionary<string, object?> Proxy)> entries,
        ushort listenPort,
        string secret)
    {
        return new()
        {
            ["log-level"] = "info",
            ["mode"] = "rule",
            ["ipv6"] = true,
            ["listeners"] = new[]
            {
                new
                {
                    name = "probe",
                    type = "socks",
                    listen = "127.0.0.1",
                    port = listenPort,
                    udp = false,
                    users = entries.Select(e => new { username = e.Tag, password = secret }).ToArray()
                }
            },
            ["proxies"] = entries.Select(e => e.Proxy).ToArray(),
            ["rules"] = entries.Select(e => $"IN-USER,{e.Tag},{e.Tag}").Append("MATCH,REJECT").ToArray()
        };
    }

    internal static Dictionary<string, object?> BuildProxy(Server server, string upstream, string name = "upstream")
    {
        var extra = server.ProtoExtra;
        var proxy = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["type"] = server.ConfigType.ToString().ToLowerInvariant(),
            ["server"] = upstream,
            ["port"] = server.Port
        };
        if (server.ConfigType is not (EConfigType.HTTP or EConfigType.TUIC or EConfigType.SSH or EConfigType.ShadowTLS))
            proxy["udp"] = true;
        else if (server.ConfigType == EConfigType.ShadowTLS && extra.Uot == true)
            proxy["udp"] = true;

        switch (server)
        {
            case SocksServer:
            case HttpServer:
                if (server.Username.IsNotEmpty()) proxy["username"] = server.Username;
                if (server.Password.IsNotEmpty()) proxy["password"] = server.Password;
                if (server is HttpServer)
                {
                    var headers = SingboxController.BuildHeaders(extra.HttpHeaders);
                    if (headers.Count > 0)
                        proxy["headers"] = headers.ToDictionary(header => header.Key, header => header.Value.Single());
                    ConfigureTls(proxy, server, "sni");
                }
                else
                    proxy["type"] = "socks5";
                break;
            case ShadowsocksServer:
                ConfigureShadowsocks(proxy, server);
                break;
            case ShadowsocksRServer ssr:
                proxy["type"] = "ssr";
                proxy["cipher"] = ssr.EncryptMethod;
                proxy["password"] = ssr.Password;
                proxy["protocol"] = ssr.Protocol;
                proxy["protocol-param"] = ssr.ProtocolParam ?? "";
                proxy["obfs"] = ssr.OBFS;
                proxy["obfs-param"] = ssr.OBFSParam ?? "";
                break;
            case VLESSServer:
            case VMessServer:
            case TrojanServer:
                ConfigureV2ray(proxy, server);
                break;
            case WireGuardServer:
                ConfigureWireGuard(proxy, server);
                break;
            case Hysteria2Server:
                ConfigureTls(proxy, server, "sni", required: true);
                ConfigureHysteria(proxy, server);
                break;
            case TUICServer:
                ConfigureTls(proxy, server, "sni", required: true);
                // Preserve the existing sing-box TUIC behavior when the profile has no ALPN.
                if (!proxy.ContainsKey("alpn")) proxy["alpn"] = Array.Empty<string>();
                proxy["uuid"] = server.Username;
                proxy["password"] = server.Password;
                proxy["udp-relay-mode"] = extra.TuicUdpRelayMode.IsNullOrEmpty() ? "native" : extra.TuicUdpRelayMode;
                if (extra.TuicReduceRtt == true) proxy["reduce-rtt"] = true;
                if (extra.TuicDisableSni == true) proxy["disable-sni"] = true;
                proxy["congestion-controller"] = extra.CongestionControl.IsNullOrEmpty() ? "bbr" : extra.CongestionControl;
                break;
            case AnyTLSServer:
                ConfigureTls(proxy, server, "sni", required: true);
                proxy["password"] = server.Password;
                if (extra.AnyTlsIdleSessionCheckIntervalSeconds is { } checkInterval) proxy["idle-session-check-interval"] = checkInterval;
                if (extra.AnyTlsIdleSessionTimeoutSeconds is { } timeout) proxy["idle-session-timeout"] = timeout;
                if (extra.AnyTlsMinIdleSession is { } minIdle) proxy["min-idle-session"] = minIdle;
                break;
            case ShadowTLSServer:
                ConfigureShadowTls(proxy, server);
                break;
            case SSHServer:
                ConfigureSsh(proxy, server);
                break;
            case TrustTunnelServer:
                ConfigureTrustTunnel(proxy, server);
                break;
            case MieruServer:
                ConfigureMieru(proxy, server);
                break;
            case ShadowQUICServer:
                ConfigureShadowQuic(proxy, server);
                break;
            default:
                throw new MessageException(i18N.TranslateFormat("Mihomo does not support node type {0}.", server.ConfigType));
        }
        return proxy;
    }

    private static void ConfigureV2ray(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        if (server is TrojanServer)
        {
            if (extra.Flow.IsNotEmpty() && extra.Flow != "none")
                throw new MessageException(i18N.Translate("Remove the flow setting from this Trojan node."));
            proxy["password"] = server.Password;
        }
        else
        {
            if (server.Password.IsNullOrWhiteSpace())
                throw new MessageException(i18N.TranslateFormat("The {0} node is missing a UUID.", server.ConfigType));
            if (server.MuxEnabled ?? Global.Settings.V2RayConfig.CoreBasicItem.MuxEnabled)
                throw new MessageException(i18N.Translate("Disable legacy V2Ray Mux for this Mihomo node."));
            proxy["uuid"] = Guid.TryParse(server.Password, out var id) ? id.ToString() : server.Password.GenerateUUIDv5();
            if (server.ConfigType == EConfigType.VMess)
            {
                proxy["cipher"] = extra.VmessSecurity.IsNullOrEmpty() ? "auto" : extra.VmessSecurity;
                proxy["alterId"] = extra.AlterId ?? 0;
            }
            else
            {
                if (extra.Flow.IsNotEmpty() && extra.Flow != "none") proxy["flow"] = extra.Flow;
                if (extra.VlessEncryption.IsNotEmpty() && extra.VlessEncryption != "none")
                    proxy["encryption"] = extra.VlessEncryption;
            }
        }

        ConfigureTls(proxy, server, server is TrojanServer ? "sni" : "servername", server is TrojanServer);
        var network = server.Network?.Trim().ToLowerInvariant();
        var host = server.RequestHost?.Trim();
        var path = server.Path.IsNullOrEmpty() ? "/" : server.Path;
        var authority = $"{ProxyUri.Host(server.Address)}:{server.Port}";
        switch (network)
        {
            case null or "" or "tcp" or "raw" when server.HeaderType.IsNullOrEmpty() || server.HeaderType == "none":
                break;
            case "ws" or "httpupgrade":
                proxy["network"] = "ws";
                var defaultHost = network == "httpupgrade" && (server.StreamSecurity is "tls" or "reality")
                    ? ProxyUri.Host(server.Sni.IsNotEmpty() ? server.Sni : server.Address)
                    : authority;
                var ws = new Dictionary<string, object?>
                {
                    ["path"] = path,
                    ["headers"] = new Dictionary<string, string> { ["Host"] = host.IsNullOrEmpty() ? defaultHost : host }
                };
                if (network == "httpupgrade")
                    ws["v2ray-http-upgrade"] = true;
                else
                {
                    var earlyData = path.IndexOf("?ed=", StringComparison.Ordinal);
                    if (earlyData >= 0 && int.TryParse(path[(earlyData + 4)..], out var size) && size > 0)
                    {
                        ws["path"] = path[..earlyData];
                        ws["max-early-data"] = size;
                        ws["early-data-header-name"] = "Sec-WebSocket-Protocol";
                    }
                }
                proxy["ws-opts"] = ws;
                break;
            case "http" or "h2" when server is not TrojanServer:
                proxy["network"] = "h2";
                proxy["h2-opts"] = new { host = Utils.Utils.String2List(host) ?? [authority], path };
                break;
            case "grpc" when server.HeaderType != Constants.GrpcMultiMode && host.IsNullOrEmpty():
                proxy["network"] = "grpc";
                proxy["grpc-opts"] = new Dictionary<string, string> { ["grpc-service-name"] = server.Path ?? "" };
                break;
            case "kcp" when server.ConfigType == EConfigType.VMess:
                var kcp = Global.Settings.V2RayConfig.KcpItem;
                proxy["network"] = "mkcp";
                proxy["mkcp-opts"] = new Dictionary<string, object?>
                {
                    ["mtu"] = kcp.Mtu, ["tti"] = kcp.Tti, ["uplink-capacity"] = kcp.UplinkCapacity,
                    ["downlink-capacity"] = kcp.DownlinkCapacity, ["congestion"] = kcp.Congestion,
                    ["read-buffer"] = kcp.ReadBufferSize, ["write-buffer"] = kcp.WriteBufferSize,
                    ["seed"] = server.Path ?? "", ["header"] = server.HeaderType ?? "none"
                };
                break;
            case "xhttp" when server is VLESSServer:
                proxy["network"] = "xhttp";
                var xhttpHost = server.Sni.IsNotEmpty() && server.StreamSecurity == "tls" ? server.Sni : server.Address;
                proxy["xhttp-opts"] = new
                {
                    path,
                    host = host.IsNotEmpty() ? host : ProxyUri.Host(xhttpHost),
                    mode = Constants.XhttpMode.Contains(server.HeaderType) ? server.HeaderType : "auto"
                };
                break;
            default:
                throw new MessageException(i18N.TranslateFormat("Mihomo does not support the {0} transport for this node. Update the server transport.", network));
        }
    }

    private static void ConfigureShadowsocks(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        proxy["type"] = "ss";
        proxy["cipher"] = ShadowsocksUtil.NormalizeCipherName(extra.SsMethod);
        proxy["password"] = server.Password;
        if (extra.Uot == true)
        {
            proxy["udp-over-tcp"] = true;
            proxy["udp-over-tcp-version"] = 2;
        }
        var network = server.Network?.Trim().ToLowerInvariant();
        var plugin = extra.SsPlugin;
        var options = extra.SsPluginOptions?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var pluginHost = server.RequestHost.IsNullOrEmpty() ? server.Address : server.RequestHost;
        string? Option(string key) => options.LastOrDefault(option => option.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];
        if (plugin.IsNullOrEmpty())
        {
            if (server.HeaderType == Constants.TcpHeaderHttp)
                plugin = "obfs-local";
            else if (network == "ws" || server.StreamSecurity == "tls" && (network is null or "" or "tcp" or "raw"))
                plugin = "v2ray-plugin";
            else if (network is not (null or "" or "tcp" or "raw") || server.HeaderType.IsNotEmpty() && server.HeaderType != "none")
                throw new MessageException(i18N.TranslateFormat("Mihomo does not support the Shadowsocks transport {0}.", network));
        }
        switch (plugin)
        {
            case null or "":
                break;
            case "obfs-local" or "simple-obfs":
                proxy["plugin"] = "obfs";
                proxy["plugin-opts"] = new { mode = Option("obfs") ?? "http", host = Option("obfs-host") ?? pluginHost };
                break;
            case "v2ray-plugin" when Option("mode") is null or "websocket":
                if (Option("certRaw") != null)
                    throw new MessageException(i18N.Translate("The Shadowsocks plugin node requires a verifiable TLS certificate."));
                proxy["plugin"] = plugin;
                proxy["plugin-opts"] = new Dictionary<string, object?>
                {
                    ["mode"] = "websocket",
                    ["host"] = Option("host") ?? pluginHost,
                    ["path"] = server.Path.IsNotEmpty() ? server.Path : ShadowsocksUtil.DecodePluginPath(Option("path") ?? "/"),
                    ["tls"] = options.Contains("tls") || server.StreamSecurity == "tls",
                    ["skip-cert-verify"] = server.AllowInsecure ?? Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure,
                    ["mux"] = int.TryParse(Option("mux"), out var mux) && mux > 0
                };
                break;
            default:
                throw new MessageException(i18N.TranslateFormat("Mihomo does not support the Shadowsocks plugin {0}.", plugin));
        }
    }

    private static void ConfigureWireGuard(Dictionary<string, object?> proxy, Server server)
    {
        proxy["type"] = "wireguard";
        var extra = server.ProtoExtra;
        if (Utils.Utils.String2List(extra.WgInterfaceAddress) is not { Count: > 0 } addresses)
            throw new MessageException(i18N.Translate("The WireGuard node is missing a local interface address."));

        if (server is AmneziaWGServer && ServerValidator.HasDuplicateWireGuardAddressFamily(extra.WgInterfaceAddress))
            throw new MessageException(i18N.Translate("AmneziaWG supports at most one IPv4 and one IPv6 local address."));

        foreach (var address in addresses.Select(value => value.Trim()))
        {
            if (!IPAddress.TryParse(address.Split('/')[0], out var ip))
                throw new MessageException(i18N.Translate("The WireGuard local interface address is invalid."));
            proxy[ip.AddressFamily == AddressFamily.InterNetwork ? "ip" : "ipv6"] = address;
        }

        proxy["private-key"] = server.Password;
        proxy["public-key"] = extra.WgPublicKey.IsNullOrEmpty() ? server.PublicKey : extra.WgPublicKey;
        if (extra.WgPresharedKey.IsNotEmpty()) proxy["pre-shared-key"] = extra.WgPresharedKey;
        proxy["mtu"] = extra.WgMtu is > 0 ? extra.WgMtu : 1280;
        if (Utils.Utils.String2List(extra.WgReserved) is { Count: > 0 } reserved)
        {
            if (reserved.Count != 3 || reserved.Any(value => !byte.TryParse(value, out _)))
                throw new MessageException(i18N.Translate("WireGuard reserved must contain three values between 0 and 255."));
            proxy["reserved"] = reserved.Select(int.Parse).ToArray();
        }

        if (server is AmneziaWGServer)
        {
            var awg = new Dictionary<string, object?>();
            if (extra.AwgVersion is > 0) awg["version"] = extra.AwgVersion.Value;
            if (extra.AwgJc is >= 0) awg["jc"] = extra.AwgJc.Value;
            if (extra.AwgJmin is >= 0) awg["jmin"] = extra.AwgJmin.Value;
            if (extra.AwgJmax is >= 0) awg["jmax"] = extra.AwgJmax.Value;
            if (extra.AwgS1 is >= 0) awg["s1"] = extra.AwgS1.Value;
            if (extra.AwgS2 is >= 0) awg["s2"] = extra.AwgS2.Value;
            if (extra.AwgS3 is >= 0) awg["s3"] = extra.AwgS3.Value;
            if (extra.AwgS4 is >= 0) awg["s4"] = extra.AwgS4.Value;
            if (!string.IsNullOrWhiteSpace(extra.AwgH1)) awg["h1"] = extra.AwgH1.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgH2)) awg["h2"] = extra.AwgH2.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgH3)) awg["h3"] = extra.AwgH3.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgH4)) awg["h4"] = extra.AwgH4.Trim();
            if (extra.AwgI1.IsNotEmpty()) awg["i1"] = extra.AwgI1;
            if (extra.AwgI2.IsNotEmpty()) awg["i2"] = extra.AwgI2;
            if (extra.AwgI3.IsNotEmpty()) awg["i3"] = extra.AwgI3;
            if (extra.AwgI4.IsNotEmpty()) awg["i4"] = extra.AwgI4;
            if (extra.AwgI5.IsNotEmpty()) awg["i5"] = extra.AwgI5;
            if (!string.IsNullOrWhiteSpace(extra.AwgHeaderProtectionKey)) awg["header-protection-key"] = extra.AwgHeaderProtectionKey.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgContentPaddingAddition)) awg["content-padding-addition"] = extra.AwgContentPaddingAddition.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgRekeyAfterTime)) awg["rekey-after-time"] = extra.AwgRekeyAfterTime.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgRekeyTimeout)) awg["rekey-timeout"] = extra.AwgRekeyTimeout.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgRejectAfterTime)) awg["reject-after-time"] = extra.AwgRejectAfterTime.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgKeepaliveTimeout)) awg["keepalive-timeout"] = extra.AwgKeepaliveTimeout.Trim();
            if (!string.IsNullOrWhiteSpace(extra.AwgMaxHandshakeAttempts)) awg["max-handshake-attempts"] = extra.AwgMaxHandshakeAttempts.Trim();
            if (extra.AwgRandomTrailers == true) awg["random-trailers"] = true;
            if (extra.AwgDisableCookies == true) awg["disable-cookies"] = true;
            if (awg.Count > 0)
                proxy["amnezia-wg-option"] = awg;
        }
    }

    private static void ConfigureHysteria(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        var defaults = Global.Settings.V2RayConfig.HysteriaItem;
        if (extra.HopIntervalMax.IsNotEmpty() || extra.BbrProfile.IsNotEmpty())
            throw new MessageException(i18N.Translate("Mihomo does not support these Hysteria2 incremental options."));
        proxy["password"] = server.Password;
        var up = extra.UpMbps ?? defaults.UpMbps;
        var down = extra.DownMbps ?? defaults.DownMbps;
        if (up > 0) proxy["up"] = $"{up} Mbps";
        if (down > 0) proxy["down"] = $"{down} Mbps";
        if (Utils.Utils.String2List(extra.Ports) is { Count: > 0 } ports)
        {
            proxy["ports"] = string.Join(",", ports.Select(port => port.Trim().Replace(':', '-')));
            var interval = extra.HopInterval;
            proxy["hop-interval"] = interval.IsNullOrEmpty()
                ? Math.Max(defaults.HopInterval, 5).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : Hysteria2Util.TryParseHopSeconds(interval, out var seconds)
                    ? seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : throw new MessageException(i18N.Translate("Hop interval must be at least 5 seconds."));
        }
        if (extra.SalamanderPass.IsNotEmpty())
        {
            proxy["obfs"] = extra.Obfs == "gecko" ? "gecko" : "salamander";
            proxy["obfs-password"] = extra.SalamanderPass;
            if (extra.Obfs == "gecko")
            {
                if (int.TryParse(extra.GeckoMinPacketSize, out var min)) proxy["obfs-min-packet-size"] = min;
                if (int.TryParse(extra.GeckoMaxPacketSize, out var max)) proxy["obfs-max-packet-size"] = max;
            }
        }
        if (extra.Hy2RealmUrl.IsNotEmpty())
            throw new MessageException(i18N.Translate("This Hysteria2 Realm node is missing separate relay credentials."));
    }

    private static void ConfigureTls(Dictionary<string, object?> proxy, Server server, string sniKey, bool required = false)
    {
        var security = server.StreamSecurity?.Trim().ToLowerInvariant();
        if (security is not (null or "" or "none" or "tls" or "reality"))
            throw new MessageException(i18N.TranslateFormat("Mihomo does not support TLS type {0}.", security));
        if (server.CertSha.IsNotEmpty())
            throw new MessageException(i18N.Translate("Replace the certificate pin with the corresponding PEM certificate."));
        if (server.EchConfigList.IsNotEmpty() || server.EchForceQuery.IsNotEmpty() && server.EchForceQuery != "none")
            throw new MessageException(i18N.Translate("Mihomo cannot preserve this custom ECH configuration."));
        var reality = security == "reality";
        if (!required && !reality && security != "tls") return;
        var handshakeHost = Utils.Utils.String2List(server.RequestHost)?.FirstOrDefault()?.Trim();
        proxy[sniKey] = server.Sni.IsNotEmpty() ? server.Sni : handshakeHost ?? server.Address;
        if (server is VMessServer or VLESSServer or HttpServer)
            proxy["tls"] = true;
        if (server is not HttpServer && server.GetAlpn() is { Count: > 0 } alpn)
            proxy["alpn"] = alpn;

        var fingerprint = server.Fingerprint;
        if (reality)
        {
            if (server is not (VMessServer or VLESSServer or TrojanServer) || server.PublicKey.IsNullOrWhiteSpace() ||
                server.SpiderX.IsNotEmpty() && server.SpiderX != "/" || server.Mldsa65Verify.IsNotEmpty())
                throw new MessageException(i18N.Translate("Mihomo cannot preserve this REALITY configuration."));
            proxy["reality-opts"] = new Dictionary<string, string>
            {
                ["public-key"] = server.PublicKey,
                ["short-id"] = server.ShortId ?? ""
            };
            if (fingerprint.IsNullOrEmpty() || fingerprint == "none")
                fingerprint = Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint;
            if (fingerprint.IsNullOrEmpty() || fingerprint == "none")
                fingerprint = "chrome";
        }
        else
            proxy["skip-cert-verify"] = server.AllowInsecure ?? Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure;
        if (fingerprint.IsNotEmpty() && fingerprint != "none" && server is VMessServer or VLESSServer or TrojanServer or AnyTLSServer or ShadowTLSServer)
            proxy["client-fingerprint"] = fingerprint;
    }

    private static void ConfigureShadowTls(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        var version = extra.ShadowTlsVersion is 1 or 2 or 3 ? extra.ShadowTlsVersion.Value : 3;
        if (version == 1)
            throw new MessageException(i18N.Translate("Mihomo does not support ShadowTLS v1."));
        if (extra.ShadowTlsPassword.IsNullOrEmpty())
            throw new MessageException(i18N.Translate("Check the required protocol fields."));
        var security = server.StreamSecurity?.Trim().ToLowerInvariant();
        if (security == "reality")
            throw new MessageException(i18N.Translate("ShadowTLS does not support REALITY."));
        if (security is not (null or "" or "none" or "tls"))
            throw new MessageException(i18N.TranslateFormat("Mihomo does not support TLS type {0}.", security));
        if (server.CertSha.IsNotEmpty())
            throw new MessageException(i18N.Translate("Replace the certificate pin with the corresponding PEM certificate."));
        if (server.EchConfigList.IsNotEmpty() || (server.EchForceQuery.IsNotEmpty() && server.EchForceQuery != "none"))
            throw new MessageException(i18N.Translate("Mihomo cannot preserve this custom ECH configuration."));
        if (!ShadowsocksUtil.IsSupportedCipher(extra.SsMethod) ||
            !ServerValidator.IsValidShadowsocksPassword(extra.SsMethod, server.Password))
            throw new MessageException(i18N.Translate("Invalid Shadowsocks encryption method."));

        proxy["type"] = "ss";
        proxy["cipher"] = ShadowsocksUtil.NormalizeCipherName(extra.SsMethod);
        proxy["password"] = server.Password;
        if (extra.Uot == true)
        {
            proxy["udp-over-tcp"] = true;
            proxy["udp-over-tcp-version"] = 2;
        }

        var pluginOpts = new Dictionary<string, object?>
        {
            ["version"] = version,
            ["password"] = extra.ShadowTlsPassword,
            ["host"] = server.Sni.IsNotEmpty() ? server.Sni : server.Address
        };
        if (server.GetAlpn() is { Count: > 0 } alpn)
            pluginOpts["alpn"] = alpn;
        if (server.AllowInsecure ?? Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure)
            pluginOpts["skip-cert-verify"] = true;

        proxy["plugin"] = "shadow-tls";
        proxy["plugin-opts"] = pluginOpts;

        var fp = server.Fingerprint;
        if (fp.IsNotEmpty() && fp != "none")
            proxy["client-fingerprint"] = fp;
    }

    private static void ConfigureSsh(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        if (extra.SshClientVersion.IsNotEmpty())
            throw new MessageException(i18N.Translate("Mihomo does not support custom SSH client version."));

        proxy["type"] = "ssh";
        proxy["username"] = server.Username;
        if (server.Password.IsNotEmpty())
            proxy["password"] = server.Password;
        if (extra.SshPrivateKey.IsNotEmpty())
            proxy["private-key"] = ServerValidator.NormalizeSshPrivateKey(extra.SshPrivateKey);
        if (extra.SshPrivateKeyPassphrase.IsNotEmpty())
            proxy["private-key-passphrase"] = extra.SshPrivateKeyPassphrase;
        if (ServerValidator.SplitSshValues(extra.SshHostKey) is { Count: > 0 } hostKeys)
            proxy["host-key"] = hostKeys;
        if (ServerValidator.SplitSshValues(extra.SshHostKeyAlgorithms) is { Count: > 0 } algorithms)
            proxy["host-key-algorithms"] = algorithms;
    }

    private static void ConfigureTrustTunnel(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        var security = server.StreamSecurity?.Trim().ToLowerInvariant();
        if (security == "reality")
            throw new MessageException(i18N.Translate("TrustTunnel does not support REALITY."));
        if (security is not (null or "" or "none" or "tls"))
            throw new MessageException(i18N.TranslateFormat("Mihomo does not support TLS type {0}.", security));
        if (!string.IsNullOrWhiteSpace(server.CertSha))
            throw new MessageException(i18N.Translate("Replace the certificate pin with the corresponding PEM certificate."));
        if (!string.IsNullOrWhiteSpace(server.EchForceQuery) &&
            !string.Equals(server.EchForceQuery.Trim(), Constants.None, StringComparison.OrdinalIgnoreCase))
            throw new MessageException(i18N.Translate("Mihomo cannot preserve this custom ECH configuration."));
        if (extra.TrustTunnelMaxConnections is > 0 && extra.TrustTunnelMaxStreams is > 0)
            throw new MessageException(i18N.Translate("Max connections and max streams cannot both be set."));

        proxy["type"] = "trusttunnel";
        if (server.Username.IsNotEmpty())
            proxy["username"] = server.Username;
        if (server.Password.IsNotEmpty())
            proxy["password"] = server.Password;

        proxy["sni"] = !string.IsNullOrWhiteSpace(server.Sni) ? server.Sni.Trim() : server.Address;
        if (server.GetAlpn() is { Count: > 0 } alpn)
            proxy["alpn"] = alpn;
        proxy["skip-cert-verify"] = server.Cert.IsNullOrWhiteSpace() &&
            (server.AllowInsecure ?? Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure);
        if (!string.IsNullOrWhiteSpace(server.Fingerprint) &&
            !string.Equals(server.Fingerprint.Trim(), Constants.None, StringComparison.OrdinalIgnoreCase))
            proxy["client-fingerprint"] = server.Fingerprint.Trim();
        if (!string.IsNullOrWhiteSpace(extra.TrustTunnelNameCertVerify))
            proxy["name-cert-verify"] = extra.TrustTunnelNameCertVerify.Trim();
        if (!string.IsNullOrWhiteSpace(server.Cert))
            proxy["certificate"] = server.Cert.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (!string.IsNullOrWhiteSpace(server.EchConfigList))
        {
            proxy["ech-opts"] = new Dictionary<string, object?>
            {
                ["enable"] = true,
                ["config"] = server.EchConfigList.Trim()
            };
        }

        if (extra.TrustTunnelHealthCheck is { } healthCheck)
            proxy["health-check"] = healthCheck;
        if (extra.TrustTunnelQuic == true)
        {
            proxy["quic"] = true;
            if (!string.IsNullOrWhiteSpace(extra.CongestionControl))
                proxy["congestion-controller"] = extra.CongestionControl.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(extra.BbrProfile) &&
                (string.IsNullOrWhiteSpace(extra.CongestionControl) ||
                 extra.CongestionControl.Trim().Equals("bbr", StringComparison.OrdinalIgnoreCase)))
                proxy["bbr-profile"] = extra.BbrProfile.Trim().ToLowerInvariant();
        }
        if (extra.TrustTunnelMaxConnections is > 0)
            proxy["max-connections"] = extra.TrustTunnelMaxConnections.Value;
        if (extra.TrustTunnelMinStreams is > 0)
            proxy["min-streams"] = extra.TrustTunnelMinStreams.Value;
        if (extra.TrustTunnelMaxStreams is > 0)
            proxy["max-streams"] = extra.TrustTunnelMaxStreams.Value;
    }

    private static void ConfigureMieru(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        if (server.Username.IsNullOrWhiteSpace() || server.Password.IsNullOrEmpty())
            throw new MessageException(i18N.Translate("Check the required protocol fields."));

        var transport = (extra.MieruTransport ?? "TCP").Trim().ToUpperInvariant();
        if (transport is not ("TCP" or "UDP"))
            throw new MessageException(i18N.Translate("Mieru transport must be TCP or UDP."));

        proxy["type"] = "mieru";
        proxy["username"] = server.Username;
        proxy["password"] = server.Password;
        proxy["transport"] = transport;

        if (!string.IsNullOrWhiteSpace(extra.MieruPortRange))
        {
            proxy.Remove("port");
            proxy["port-range"] = extra.MieruPortRange.Trim().Replace(':', '-');
        }
        if (!string.IsNullOrWhiteSpace(extra.MieruMultiplexing))
            proxy["multiplexing"] = extra.MieruMultiplexing.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(extra.MieruHandshakeMode))
            proxy["handshake-mode"] = extra.MieruHandshakeMode.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(extra.MieruTrafficPattern))
            proxy["traffic-pattern"] = extra.MieruTrafficPattern.Trim();
    }

    private static void ConfigureShadowQuic(Dictionary<string, object?> proxy, Server server)
    {
        var extra = server.ProtoExtra;
        if (server.Username.IsNullOrWhiteSpace() || server.Password.IsNullOrEmpty())
            throw new MessageException(i18N.Translate("Check the required protocol fields."));

        proxy["type"] = "shadowquic";
        proxy["username"] = server.Username;
        proxy["password"] = server.Password;

        if (!string.IsNullOrWhiteSpace(server.Sni))
            proxy["sni"] = server.Sni.Trim();
        if (server.GetAlpn() is { Count: > 0 } alpn)
            proxy["alpn"] = alpn;
        if (Utils.Utils.String2List(extra.ShadowQuicVersions) is { Count: > 0 } versions)
            proxy["quic-versions"] = versions.Select(v => v.Trim().ToLowerInvariant()).ToArray();
        if (extra.ShadowQuicUdpOverStream == true)
            proxy["udp-over-stream"] = true;
        if (extra.ShadowQuicZeroRtt == true)
            proxy["zero-rtt"] = true;
        if (extra.ShadowQuicKeepAliveInterval is > 0)
            proxy["keep-alive-interval"] = extra.ShadowQuicKeepAliveInterval.Value;
        if (!string.IsNullOrWhiteSpace(extra.CongestionControl))
            proxy["congestion-controller"] = extra.CongestionControl.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(extra.BbrProfile) &&
            (string.IsNullOrWhiteSpace(extra.CongestionControl) ||
             extra.CongestionControl.Trim().Equals("bbr", StringComparison.OrdinalIgnoreCase)))
            proxy["bbr-profile"] = extra.BbrProfile.Trim().ToLowerInvariant();
        if (extra.UpMbps is > 0)
            proxy["up"] = $"{extra.UpMbps.Value} Mbps";
        if (extra.DownMbps is > 0)
            proxy["down"] = $"{extra.DownMbps.Value} Mbps";
        if (extra.ShadowQuicCwnd is > 0)
            proxy["cwnd"] = extra.ShadowQuicCwnd.Value;
        if (extra.ShadowQuicMaxDatagramFrameSize is > 0)
            proxy["max-datagram-frame-size"] = extra.ShadowQuicMaxDatagramFrameSize.Value;
        if (extra.ShadowQuicMaxOpenStreams is > 0)
            proxy["max-open-streams"] = extra.ShadowQuicMaxOpenStreams.Value;
        if (extra.ShadowQuicRecvWindowConn is > 0)
            proxy["recv-window-conn"] = extra.ShadowQuicRecvWindowConn.Value;
        if (extra.ShadowQuicRecvWindow is > 0)
            proxy["recv-window"] = extra.ShadowQuicRecvWindow.Value;
        if (extra.ShadowQuicDisableMtuDiscovery == true)
            proxy["disable-mtu-discovery"] = true;
    }
}

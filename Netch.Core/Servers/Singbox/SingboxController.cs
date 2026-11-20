using System.Net;
using System.Net.Sockets;
using Netch.Enums;
using Netch.Manager;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class SingboxController : Guard, IServerController
{
    public SingboxController() : base(FindExecutable())
    {
    }

    public override string Name => "sing-box";
    protected override IEnumerable<string> StartedKeywords => ["sing-box started"];
    protected override IEnumerable<string> FailedKeywords => ["FATAL"];

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public async Task<SocksServer> StartAsync(Server server)
    {
        if (CoreSelector.UsesXhttp(server))
            throw new MessageException(i18N.Translate("XHTTP nodes require Xray."));

        var config = BuildConfig(server);
        var configPath = Path.Combine(Global.NetchDir, Constants.TempConfig);
        Directory.CreateDirectory(Path.Combine(Global.NetchDir, "data"));
        await using (var fileStream = new FileStream(configPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(fileStream, config, Global.NewCustomJsonSerializerOptions());
        }

        Log.Information("Starting sing-box for {ConfigType}: {Address}:{Port}", server.ConfigType, server.Address, server.Port);
        try
        {
            await StartGuardAsync($"run --disable-color -c \"{configPath}\"");
        }
        catch (MessageException error)
        {
            var hint = server.ConfigType switch
            {
                Enums.EConfigType.MASQUE => " " + i18N.Translate("MASQUE requires sing-box 1.15.0-alpha.7 or newer with masque-client support."),
                Enums.EConfigType.Naive => " " + i18N.Translate("On Windows, keep libcronet.dll from the official package next to sing-box.exe."),
                _ => string.Empty
            };
            throw new MessageException(i18N.TranslateFormat("{0} See logging\\sing-box.log.{1}", error.Message, hint));
        }

        return new SocksServer(this.LocalAddress ?? Global.Settings.LocalAddress, this.Socks5LocalPort ?? Global.Settings.Socks5LocalPort, server.Address);
    }

    protected override void OnStartFailed() { }

    private Dictionary<string, object?> BuildConfig(Server server)
    {
        var proxy = BuildProxy(server);
        var direct = new Dictionary<string, object?> { ["type"] = "direct", ["tag"] = "direct" };
        var endpoint = server.ConfigType is EConfigType.MASQUE or EConfigType.WireGuard;
        var resolver = new Dictionary<string, object?> { ["type"] = "local", ["tag"] = "bootstrap" };
        if (IPAddress.TryParse(Global.Settings.OutboundDNS, out var dnsAddress))
        {
            resolver["type"] = "udp";
            resolver["server"] = dnsAddress.ToString();
        }
        var config = new Dictionary<string, object?>
        {
            ["log"] = new Dictionary<string, object?>
            {
                ["level"] = "debug",
                ["timestamp"] = true
            },
            ["inbounds"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "mixed",
                    ["tag"] = "socks-in",
                    ["listen"] = this.LocalAddress ?? Global.Settings.LocalAddress,
                    ["listen_port"] = this.Socks5LocalPort ?? Global.Settings.Socks5LocalPort
                }
            },
            ["dns"] = new { servers = new[] { resolver } },
            ["outbounds"] = endpoint ? new[] { direct } : new[] { proxy, direct },
            ["route"] = new Dictionary<string, object?>
            {
                // WireGuard opens both IPv4 and IPv6 sockets. Windows interface binding can reject its IPv6 socket.
                ["auto_detect_interface"] = server.ConfigType != EConfigType.WireGuard,
                ["default_domain_resolver"] = "bootstrap",
                ["rules"] = new[] { new Dictionary<string, string> { ["inbound"] = "socks-in", ["action"] = "sniff" } },
                ["final"] = "proxy"
            }
        };

        if (endpoint)
        {
            config["endpoints"] = new[] { proxy };
        }

        return config;
    }

    private Dictionary<string, object?> BuildProxy(Server server)
    {
        var extra = server.ProtoExtra;
        if (server.ConfigType == EConfigType.WireGuard)
            return BuildWireGuard(server);

        var tls = BuildTls(server);

        if (server.ConfigType == Enums.EConfigType.MASQUE)
        {
            var endpoint = new Dictionary<string, object?>
            {
                ["type"] = "masque-client",
                ["tag"] = "proxy",
                ["server"] = server.Address,
                ["server_port"] = server.Port,
                ["username"] = server.Username,
                ["password"] = server.Password,
                ["path"] = extra.MasquePath.IsNullOrEmpty() ? null : extra.MasquePath,
                ["tls"] = tls
            };
            var headers = BuildHeaders(extra.MasqueHeaders);
            if (headers.Count > 0) endpoint["headers"] = headers;
            return endpoint;
        }

        var outbound = new Dictionary<string, object?>
        {
            ["tag"] = "proxy",
            ["server"] = server.Address,
            ["server_port"] = server.Port,
            ["tls"] = tls
        };

        switch (server.ConfigType)
        {
            case EConfigType.SOCKS:
            case EConfigType.HTTP:
                outbound["type"] = server.ConfigType == EConfigType.HTTP ? "http" : "socks";
                outbound["username"] = server.Username;
                outbound["password"] = server.Password;
                if (server.ConfigType == EConfigType.HTTP)
                {
                    var headers = BuildHeaders(extra.HttpHeaders);
                    if (headers.Count > 0) outbound["headers"] = headers;
                }
                else
                {
                    outbound.Remove("tls");
                    if (extra.Uot == true) outbound["udp_over_tcp"] = true;
                }
                break;
            case EConfigType.VLESS:
            case EConfigType.VMess:
            case EConfigType.Trojan:
                ConfigureV2rayProtocol(outbound, server);
                break;
            case EConfigType.Shadowsocks:
                ConfigureShadowsocks(outbound, server);
                break;
            case EConfigType.Hysteria2:
                ConfigureHysteria(outbound, server);
                break;
            case EConfigType.TUIC:
                outbound["type"] = "tuic";
                outbound["uuid"] = server.Username;
                outbound["password"] = server.Password;
                outbound["congestion_control"] = extra.CongestionControl.IsNullOrEmpty() ? "bbr" : extra.CongestionControl;
                break;
            case Enums.EConfigType.AnyTLS:
                outbound["type"] = "anytls";
                outbound["password"] = server.Password;
                break;
            case Enums.EConfigType.Naive:
                outbound["type"] = "naive";
                outbound["username"] = server.Username;
                outbound["password"] = server.Password;
                if (extra.NaiveQuic == true)
                {
                    outbound["quic"] = true;
                    if (extra.CongestionControl.IsNotEmpty()) outbound["quic_congestion_control"] = extra.CongestionControl;
                }
                if (extra.InsecureConcurrency is > 0) outbound["insecure_concurrency"] = extra.InsecureConcurrency;
                if (extra.Uot == true) outbound["udp_over_tcp"] = true;
                break;
            default:
                throw new MessageException(i18N.TranslateFormat("Unknown sing-box node type {0}.", server.ConfigType));
        }

        return outbound;
    }

    private static void ConfigureV2rayProtocol(Dictionary<string, object?> outbound, Server server)
    {
        var extra = server.ProtoExtra;
        outbound["type"] = server.ConfigType.ToString().ToLowerInvariant();
        var transport = BuildTransport(server);
        if (transport != null)
            outbound["transport"] = transport;
        if (server.ConfigType == EConfigType.Trojan)
        {
            if (extra.Flow.IsNotEmpty() && extra.Flow != "none")
                throw new MessageException(i18N.Translate("Remove the unsupported flow setting from this sing-box Trojan node."));
            outbound["password"] = server.Password;
        }
        else
        {
            if (server.Password.IsNullOrWhiteSpace())
                throw new MessageException(i18N.TranslateFormat("The {0} node is missing a UUID.", server.ConfigType));
            outbound["uuid"] = Guid.TryParse(server.Password, out var id) ? id.ToString() : server.Password.GenerateUUIDv5();
            if (server.ConfigType == EConfigType.VLESS)
            {
                if (extra.VlessEncryption.IsNotEmpty() && extra.VlessEncryption != "none")
                    throw new MessageException(i18N.Translate("sing-box does not support this VLESS encryption setting."));
                if (extra.Flow.IsNotEmpty() && extra.Flow != "none")
                    outbound["flow"] = extra.Flow;
            }
            else
            {
                outbound["security"] = extra.VmessSecurity.IsNullOrEmpty() ? "auto" : extra.VmessSecurity;
                outbound["alter_id"] = extra.AlterId ?? 0;
            }
        }
        if (server.ConfigType is EConfigType.VLESS or EConfigType.VMess &&
            (server.MuxEnabled ?? Global.Settings.V2RayConfig.CoreBasicItem.MuxEnabled))
        {
            throw new MessageException(i18N.Translate("Disable legacy V2Ray Mux for this sing-box node."));
        }
    }

    private static Dictionary<string, object?>? BuildTransport(Server server)
    {
        var network = server.Network?.Trim().ToLowerInvariant();
        var host = server.RequestHost?.Trim();
        var path = server.Path.IsNullOrEmpty() ? "/" : server.Path;
        switch (network)
        {
            case null or "" or "tcp" or "raw":
                if (server.HeaderType.IsNullOrEmpty() || server.HeaderType == "none")
                    return null;
                throw new MessageException(i18N.Translate("sing-box cannot preserve legacy TCP camouflage. Update the server transport."));
            case "ws":
                var ws = new Dictionary<string, object?> { ["type"] = "ws", ["path"] = path };
                if (host.IsNotEmpty())
                    ws["headers"] = new Dictionary<string, string> { ["Host"] = host };
                // Xray encodes WebSocket early data as an ed query parameter.
                var earlyData = path.IndexOf("?ed=", StringComparison.Ordinal);
                if (earlyData >= 0 && int.TryParse(path[(earlyData + 4)..], out var size) && size > 0)
                {
                    ws["path"] = path[..earlyData];
                    ws["max_early_data"] = size;
                    ws["early_data_header_name"] = "Sec-WebSocket-Protocol";
                }
                return ws;
            case "httpupgrade":
                return new() { ["type"] = "httpupgrade", ["host"] = host, ["path"] = path };
            case "http" or "h2":
                return new() { ["type"] = "http", ["host"] = Utils.Utils.String2List(host), ["path"] = path };
            case "grpc":
                if (server.HeaderType == Constants.GrpcMultiMode || host.IsNotEmpty())
                    throw new MessageException(i18N.Translate("sing-box cannot preserve this gRPC multi or custom authority setting."));
                return new() { ["type"] = "grpc", ["service_name"] = server.Path ?? string.Empty };
            case "quic" when (host.IsNullOrEmpty() || host == "none")
                && (server.HeaderType.IsNullOrEmpty() || server.HeaderType == "none") && server.Path.IsNullOrEmpty():
                return new() { ["type"] = "quic" };
            default:
                throw new MessageException(i18N.TranslateFormat("sing-box does not support the {0} transport for this node. Update the server transport.", network));
        }
    }

    private static void ConfigureShadowsocks(Dictionary<string, object?> outbound, Server server)
    {
        var extra = server.ProtoExtra;
        outbound["type"] = "shadowsocks";
        outbound["method"] = extra.SsMethod?.ToLowerInvariant() switch
        {
            "plain" => "none",
            "chacha20-poly1305" => "chacha20-ietf-poly1305",
            "xchacha20-poly1305" => "xchacha20-ietf-poly1305",
            var method => method
        };
        outbound["password"] = server.Password;
        outbound.Remove("tls");
        var plugin = extra.SsPlugin;
        var options = extra.SsPluginOptions;
        if (plugin.IsNullOrEmpty())
        {
            var network = server.Network?.Trim().ToLowerInvariant();
            if (network is not (null or "" or "tcp" or "raw" or "ws" or "quic") ||
                (network == "quic" && (server.HeaderType.IsNotEmpty() && server.HeaderType != "none"
                    || server.RequestHost.IsNotEmpty() && server.RequestHost != "none" || server.Path.IsNotEmpty())))
                throw new MessageException(i18N.TranslateFormat("sing-box does not support the Shadowsocks transport {0}.", network));

            if (server.HeaderType == Constants.TcpHeaderHttp)
            {
                plugin = "obfs-local";
                options = $"obfs=http;obfs-host={server.RequestHost}";
            }
            else if (network is "ws" or "quic" || server.StreamSecurity == "tls")
            {
                plugin = "v2ray-plugin";
                options = $"mode={(network == "quic" ? "quic" : "websocket")};mux=0";
                if (server.RequestHost.IsNotEmpty()) options += $";host={server.RequestHost}";
                if (server.Path.IsNotEmpty() && network != "quic") options += $";path={server.Path}";
                if (server.StreamSecurity == "tls") options += ";tls";
            }
            else
            {
                BuildTransport(server);
            }
        }
        if (plugin.IsNotEmpty())
        {
            if (plugin is not ("obfs-local" or "v2ray-plugin"))
                throw new MessageException(i18N.TranslateFormat("sing-box does not support the Shadowsocks plugin {0}.", plugin));
            if (server.Cert.IsNotEmpty() || options?.Contains("certRaw=", StringComparison.OrdinalIgnoreCase) == true)
                throw new MessageException(i18N.Translate("The sing-box Shadowsocks plugin cannot preserve the custom TLS certificate."));
            outbound["plugin"] = plugin;
            outbound["plugin_opts"] = options;
        }
        if (extra.Uot == true) outbound["udp_over_tcp"] = true;
    }

    private static Dictionary<string, object?> BuildWireGuard(Server server)
    {
        var extra = server.ProtoExtra;
        var addresses = Utils.Utils.String2List(extra.WgInterfaceAddress);
        if (addresses is not { Count: > 0 })
            throw new MessageException(i18N.Translate("The WireGuard node is missing a local interface address."));
        var peer = new Dictionary<string, object?>
        {
            ["address"] = server.Address,
            ["port"] = server.Port,
            ["public_key"] = extra.WgPublicKey.IsNullOrEmpty() ? server.PublicKey : extra.WgPublicKey,
            ["pre_shared_key"] = extra.WgPresharedKey,
            ["allowed_ips"] = new[] { "0.0.0.0/0", "::/0" }
        };
        if (Utils.Utils.String2List(extra.WgReserved) is { Count: > 0 } reserved)
        {
            if (reserved.Count != 3 || reserved.Any(value => !byte.TryParse(value, out _)))
                throw new MessageException(i18N.Translate("WireGuard reserved must contain three values between 0 and 255."));
            peer["reserved"] = reserved.Select(int.Parse).ToArray();
        }

        return new()
        {
            ["type"] = "wireguard",
            ["tag"] = "proxy",
            ["address"] = addresses.Select(address =>
            {
                var value = address.Trim();
                return IPAddress.TryParse(value, out var ip)
                    ? $"{ip}/{(ip.AddressFamily == AddressFamily.InterNetwork ? 32 : 128)}"
                    : value;
            }).ToArray(),
            ["private_key"] = server.Password,
            ["mtu"] = extra.WgMtu is > 0 ? extra.WgMtu : 1280,
            ["peers"] = new[] { peer }
        };
    }

    private static void ConfigureHysteria(Dictionary<string, object?> outbound, Server server)
    {
        var extra = server.ProtoExtra;
        var defaults = Global.Settings.V2RayConfig.HysteriaItem;
        outbound["type"] = "hysteria2";
        outbound["password"] = server.Password;
        var up = extra.UpMbps ?? defaults.UpMbps;
        var down = extra.DownMbps ?? defaults.DownMbps;
        if (up > 0) outbound["up_mbps"] = up;
        if (down > 0) outbound["down_mbps"] = down;
        if (Utils.Utils.String2List(extra.Ports) is { Count: > 0 } ports)
        {
            outbound.Remove("server_port");
            outbound["server_ports"] = ports.Select(port =>
            {
                var range = port.Trim().Replace('-', ':');
                return range.Contains(':') ? range : $"{range}:{range}";
            }).ToArray();
            var interval = extra.HopInterval;
            outbound["hop_interval"] = interval.IsNullOrEmpty()
                ? $"{Math.Max(defaults.HopInterval, 5)}s"
                : int.TryParse(interval, out _) ? $"{interval}s" : interval;
        }
        if (extra.SalamanderPass.IsNotEmpty())
        {
            var type = extra.Obfs == "gecko" ? "gecko" : "salamander";
            var obfs = new Dictionary<string, object?> { ["type"] = type, ["password"] = extra.SalamanderPass };
            if (type == "gecko")
            {
                if (int.TryParse(extra.GeckoMinPacketSize, out var min)) obfs["min_packet_size"] = min;
                if (int.TryParse(extra.GeckoMaxPacketSize, out var max)) obfs["max_packet_size"] = max;
            }
            outbound["obfs"] = obfs;
        }
        if (extra.Hy2RealmUrl.IsNotEmpty())
            throw new MessageException(i18N.Translate("This Hysteria2 Realm configuration is missing separate credentials required by sing-box."));
    }

    private static Dictionary<string, object?>? BuildTls(Server server)
    {
        var security = server.StreamSecurity?.Trim().ToLowerInvariant();
        var reality = security == Constants.StreamSecurityReality;
        var required = server.ConfigType is EConfigType.TUIC or EConfigType.AnyTLS or EConfigType.Naive
            or EConfigType.MASQUE or EConfigType.Hysteria2;
        if (security is not (null or "" or "none" or "tls" or "reality"))
            throw new MessageException(i18N.TranslateFormat("sing-box does not support TLS type {0}.", security));

        var certs = CertPemManager.ParsePemChain(server.Cert);
        if (server.CertSha.IsNotEmpty() && certs.Count == 0)
            throw new MessageException(i18N.Translate("sing-box 1.14.4 cannot pin the full certificate SHA-256. Import the corresponding PEM certificate."));
        if (server.EchConfigList.IsNotEmpty() || (server.EchForceQuery.IsNotEmpty() && server.EchForceQuery != "none"))
            throw new MessageException(i18N.Translate("sing-box cannot preserve this custom ECH configuration."));
        if (!required && !reality && security != "tls")
        {
            if (certs.Count != 0)
                throw new MessageException(i18N.Translate("The node specifies a TLS certificate but TLS is disabled."));
            return null;
        }

        var host = Utils.Utils.String2List(server.RequestHost)?.FirstOrDefault()?.Trim();
        var tls = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["server_name"] = server.Sni.IsNotEmpty() ? server.Sni : host ?? server.Address
        };
        if (certs.Count > 0) tls["certificate"] = certs;
        if (server.ConfigType == EConfigType.Naive)
            return tls;

        tls["insecure"] = !reality && certs.Count == 0 &&
            (server.AllowInsecure ?? Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure);
        if (server.GetAlpn() is { Count: > 0 } alpn) tls["alpn"] = alpn;
        if (reality)
        {
            if ((server.SpiderX.IsNotEmpty() && server.SpiderX != "/") || server.Mldsa65Verify.IsNotEmpty())
                throw new MessageException(i18N.Translate("sing-box cannot preserve this REALITY extension."));
            tls["reality"] = new Dictionary<string, object?>
            {
                ["enabled"] = true, ["public_key"] = server.PublicKey, ["short_id"] = server.ShortId ?? string.Empty
            };
        }

        var fp = server.Fingerprint;
        if (reality && (fp.IsNullOrEmpty() || fp == "none"))
            fp = Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint.IsNullOrEmpty()
                ? "chrome" : Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint;
        if (fp.IsNotEmpty() && fp != "none")
            tls["utls"] = new { enabled = true, fingerprint = fp };
        return tls;
    }

    internal static Dictionary<string, List<string>> BuildHeaders(string? value)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (value.IsNullOrWhiteSpace()) return result;

        char[] separators = value.Contains('\r') || value.Contains('\n') ? ['\r', '\n'] : [';', '|'];
        foreach (var line in value.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0) continue;
            var name = line[..separator].Trim();
            var headerValue = line[(separator + 1)..].Trim();
            if (!result.TryGetValue(name, out var values))
            {
                values = [];
                result[name] = values;
            }
            values.Add(headerValue);
        }
        return result;
    }

    private static string FindExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Global.NetchDir, "bin", "sing-box.exe"),
            Path.Combine(Global.NetchDir, "bin", "sing_box", "sing-box.exe")
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}

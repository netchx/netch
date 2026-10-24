using Netch.Enums;
using Netch.Manager;
using Netch.Models;
using Netch.Utils;

#pragma warning disable VSTHRD200

namespace Netch.Servers;

public static class V2rayConfigUtils
{
    public static async Task<V2rayConfig> GenerateClientConfigAsync(Server server)
    {
        var v2rayConfig = new V2rayConfig();
        v2rayConfig.log = new Log4Ray
        {
            loglevel = Constants.LogLevels[2]
        };
        v2rayConfig.dns = new Dns4Ray()
        {
            servers = [
                new DnsServer4Ray
                {
                    address = Global.Settings.OutboundDNS,
                    domains = [$"domain:{server.Address}"],
                    skipFallback = true
                }
            ]

        };
        v2rayConfig.inbounds = [GenerateInbound()];

        v2rayConfig.outbounds = [GenerateOutbound(server)];


        return v2rayConfig;
    }

    private static Outbounds4Ray GenerateOutbound(Server server)
    {
        var outbound = new Outbounds4Ray
        {
            settings = new Outboundsettings4Ray(),
        };

        var muxEnabled = server.MuxEnabled ?? Global.Settings.V2RayConfig.CoreBasicItem.MuxEnabled;
        GenOutboundMux(outbound);
        switch (server)
        {
            case SocksServer socks:
            case HttpServer http:
                {
                    outbound.protocol = server is SocksServer ? "socks" : "http";
                    var authServer = server as dynamic;
                    outbound.settings.servers = [
                        new ServersItem4Ray
                        {
                            address = server.Address,
                            port = server.Port,
                            users = authServer.Auth() ? [
                            new SocksUsersItem4Ray
                            {
                                    user = server.Username,
                                    pass = server.Password,
                                    level = 1
                            }]: null
                        }
                    ];
                    //没有mux选项，强制关闭
                    GenOutboundMux(outbound);
                    break;
                }
            case VLESSServer vless:
                {
                    outbound.protocol = "vless";
                    outbound.settings.vnext = [

                        new VnextItem4Ray
                        {
                            address = server.Address,
                            port = server.Port,
                            users =[
                                new UsersItem4Ray
                                {
                                    id = vless.Password,
                                    encryption = vless.ProtoExtra.VlessEncryption
                                }
                            ]
                        }
                    ];

                    if (!string.IsNullOrWhiteSpace(vless.ProtoExtra.Flow))
                    {
                        outbound.settings.vnext[0].users[0].flow = vless.ProtoExtra.Flow;
                    }

                    GenOutboundMux(outbound, false, muxEnabled);
                    break;
                }
            case VMessServer vmess:
                {
                    outbound.protocol = "vmess";
                    if (vmess.ProtoExtra.VmessSecurity == "auto" && vmess.StreamSecurity != "none" && !Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure)
                    {
                        vmess.ProtoExtra.VmessSecurity = "zero";
                    }
                    outbound.settings.vnext = [
                        new VnextItem4Ray
                        {
                            address = server.Address,
                            port = server.Port,
                            users = [
                                new UsersItem4Ray()
                                {
                                    id = vmess.Password,
                                    alterId = vmess.ProtoExtra.AlterId??0,
                                    security = vmess.ProtoExtra.VmessSecurity
                                }
                            ]
                        }
                    ];

                    GenOutboundMux(outbound, muxEnabled, muxEnabled);
                    break;
                }
            case ShadowsocksServer ss:
                outbound.protocol = "shadowsocks";
                outbound.settings.servers =
                [
                    new ServersItem4Ray
                    {
                        address = server.Address,
                        port = server.Port,
                        password = ss.Password,
                        method = Constants.SsSecuritiesInXray.Contains( ss.ProtoExtra.SsMethod)?ss.ProtoExtra.SsMethod:"none",
                        ota = false,
                        level = 1
                    }
                ];
                //没有mux选项，强制关闭
                GenOutboundMux(outbound);
                break;
            case TrojanServer trojan:
                outbound.protocol = "trojan";
                outbound.settings.servers = [

                    new ServersItem4Ray() // I'm not serious
                    {
                        address = server.Address,
                        port = server.Port,
                        password = trojan.Password,
                        ota = false,
                        level = 1
                    }
                ];

                if (!string.IsNullOrWhiteSpace(trojan.ProtoExtra.Flow))
                {
                    outbound.settings.servers[0].flow = trojan.ProtoExtra.Flow;
                }

                //没有mux选项，强制关闭
                GenOutboundMux(outbound);
                break;
            case WireGuardServer wg:
                outbound.protocol = "wireguard";
                var address = wg.Address;
                if (Utils.Utils.IsIpv6(address))
                {
                    address = $"[{address}]";
                }
                outbound.settings.address = Utils.Utils.String2List(wg.ProtoExtra.WgInterfaceAddress);
                outbound.settings.secretKey = wg.Password;
                outbound.settings.reserved = Utils.Utils.String2List(wg.ProtoExtra.WgReserved)?.Select(int.Parse).ToList();
                outbound.settings.mtu = wg.ProtoExtra.WgMtu > 0 ? wg.ProtoExtra.WgMtu : Constants.TunMtus.First();
                outbound.settings.peers = [
                    new WireguardPeer4Ray
                    {
                        publicKey = wg.PublicKey,
                        endpoint = address + ":" + server.Port.ToString()
                    }
                ];
                break;
            case Hysteria2Server hysteria2Server:
                outbound.protocol = "hysteria";
                outbound.settings.address = server.Address;
                outbound.settings.port = server.Port;
                outbound.settings.version = 2;
                break;
            case MASQUEServer masque:
                outbound.protocol = "masque";
                outbound.settings.address = server.Address;
                outbound.settings.port = server.Port;
                break;
        }

        outbound.streamSettings = GenBoundStreamSettings(server, outbound);

        return outbound;
    }

    private static StreamSettings4Ray GenBoundStreamSettings(Server server, Outbounds4Ray outbound)
    {
        var streamSettings = new StreamSettings4Ray();
        try
        {
            var network = server.GetNetwork();
            if (server.ConfigType == EConfigType.Hysteria2)
            {
                network = "hysteria";
            }
            else if (server.ConfigType == EConfigType.MASQUE)
            {
                network = "masque";
            }
            streamSettings.network = network;
            var host = server.RequestHost.TrimEx();
            var path = server.Path.TrimEx();
            var sni = server.Sni.TrimEx();
            var useragent = "";
            if (!Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint.IsNullOrEmpty())
            {
                try
                {
                    useragent = Constants.UserAgentTexts[Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint];
                }
                catch (KeyNotFoundException)
                {
                    useragent = Constants.UserAgentTexts["chrome"];
                }
            }

            //if tls
            if (server.StreamSecurity == Constants.StreamSecurity)
            {
                streamSettings.security = server.StreamSecurity;

                TlsSettings4Ray tlsSettings = new()
                {
                    allowInsecure = server.AllowInsecure ?? Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure,
                    verifyPeerCertByName = (server.AllowInsecure ?? Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure)
                        ? (sni.IsNotEmpty() ? sni : host.IsNotEmpty() ? Utils.Utils.String2List(host)?.First() : server.Address)
                        : null,
                    alpn = server.GetAlpn(),
                    fingerprint = server.Fingerprint.IsNullOrEmpty() ? Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint : server.Fingerprint,
                    echConfigList = server.EchConfigList,
                    echForceQuery = server.EchForceQuery
                };
                if (!string.IsNullOrWhiteSpace(sni))
                {
                    tlsSettings.serverName = sni;
                }
                else if (!string.IsNullOrWhiteSpace(host))
                {
                    tlsSettings.serverName = Utils.Utils.String2List(host)?.First();
                }
                var certs = CertPemManager.ParsePemChain(server.Cert);
                if (certs.Count > 0)
                {
                    var certsettings = new List<CertificateSettings4Ray>();
                    foreach (var cert in certs)
                    {
                        var certPerLine = cert.Split("\n").ToList();
                        certsettings.Add(new CertificateSettings4Ray
                        {
                            certificate = certPerLine,
                            usage = "verify",
                        });
                    }
                    tlsSettings.certificates = certsettings;
                    tlsSettings.disableSystemRoot = true;
                    tlsSettings.allowInsecure = false;
                }
                else if (!server.CertSha.IsNullOrEmpty())
                {
                    tlsSettings.pinnedPeerCertSha256 = server.CertSha;
                    tlsSettings.allowInsecure = false;
                }
                streamSettings.tlsSettings = tlsSettings;
            }

            //if Reality
            if (server.StreamSecurity == Constants.StreamSecurityReality)
            {
                streamSettings.security = server.StreamSecurity;

                TlsSettings4Ray realitySettings = new()
                {
                    fingerprint = server.Fingerprint.IsNullOrEmpty() ? Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint : server.Fingerprint,
                    serverName = sni,
                    publicKey = server.PublicKey,
                    shortId = server.ShortId,
                    spiderX = server.SpiderX,
                    mldsa65Verify = server.Mldsa65Verify,
                    show = false,
                };

                streamSettings.realitySettings = realitySettings;
            }

            //streamSettings
            switch (network)
            {
                case nameof(ETransport.kcp):
                    KcpSettings4Ray kcpSettings = new()
                    {
                        mtu = Global.Settings.V2RayConfig.KcpItem.Mtu,
                        tti = Global.Settings.V2RayConfig.KcpItem.Tti
                    };

                    kcpSettings.uplinkCapacity = Global.Settings.V2RayConfig.KcpItem.UplinkCapacity;
                    kcpSettings.downlinkCapacity = Global.Settings.V2RayConfig.KcpItem.DownlinkCapacity;

                    kcpSettings.congestion = Global.Settings.V2RayConfig.KcpItem.Congestion;
                    kcpSettings.readBufferSize = Global.Settings.V2RayConfig.KcpItem.ReadBufferSize;
                    kcpSettings.writeBufferSize = Global.Settings.V2RayConfig.KcpItem.WriteBufferSize;
                    streamSettings.finalmask ??= new();
                    if (Constants.KcpHeaderMaskMap.TryGetValue(server.HeaderType, out var header))
                    {
                        streamSettings.finalmask.udp =
                        [
                            new Mask4Ray
                            {
                                type = header,
                                settings = server.HeaderType == "dns" && !host.IsNullOrEmpty() ? new MaskSettings4Ray { domain = host } : null
                            }
                        ];
                    }
                    streamSettings.finalmask.udp ??= [];
                    if (path.IsNullOrEmpty())
                    {
                        streamSettings.finalmask.udp.Add(new Mask4Ray
                        {
                            type = "mkcp-original"
                        });
                    }
                    else
                    {
                        streamSettings.finalmask.udp.Add(new Mask4Ray
                        {
                            type = "mkcp-aes128gcm",
                            settings = new MaskSettings4Ray { password = path }
                        });
                    }
                    streamSettings.kcpSettings = kcpSettings;
                    break;
                //ws
                case nameof(ETransport.ws):
                    WsSettings4Ray wsSettings = new();
                    wsSettings.headers = new Headers4Ray();

                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        wsSettings.host = host;
                    }
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        wsSettings.path = path;
                    }
                    if (!string.IsNullOrWhiteSpace(useragent))
                    {
                        wsSettings.headers.UserAgent = useragent;
                    }
                    streamSettings.wsSettings = wsSettings;

                    break;
                //httpupgrade
                case nameof(ETransport.httpupgrade):
                    HttpupgradeSettings4Ray httpupgradeSettings = new();

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        httpupgradeSettings.path = path;
                    }
                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        httpupgradeSettings.host = host;
                    }
                    streamSettings.httpupgradeSettings = httpupgradeSettings;

                    break;
                //xhttp
                case nameof(ETransport.xhttp):
                    streamSettings.network = ETransport.xhttp.ToString();
                    XhttpSettings4Ray xhttpSettings = new();

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        xhttpSettings.path = path;
                    }
                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        xhttpSettings.host = host;
                    }
                    if (!string.IsNullOrWhiteSpace(server.HeaderType) && Constants.XhttpMode.Contains(server.HeaderType))
                    {
                        xhttpSettings.mode = server.HeaderType;
                    }
                    if (!string.IsNullOrWhiteSpace(server.Extra))
                    {
                        xhttpSettings.extra = JsonUtils.ParseJson(server.Extra);
                    }

                    streamSettings.xhttpSettings = xhttpSettings;
                    GenOutboundMux(outbound);

                    break;
                //h2
                case nameof(ETransport.h2):
                    HttpSettings4Ray httpSettings = new();

                    if (host.IsNotEmpty())
                    {
                        httpSettings.host = Utils.Utils.String2List(host);
                    }
                    httpSettings.path = path;

                    streamSettings.httpSettings = httpSettings;

                    break;
                //quic
                case nameof(ETransport.quic):
                    QuicSettings4Ray quicsettings = new()
                    {
                        security = host,
                        key = path,
                        header = new Header4Ray
                        {
                            type = server.HeaderType
                        }
                    };
                    streamSettings.quicSettings = quicsettings;
                    if (server.StreamSecurity == Constants.StreamSecurity)
                    {
                        if (!string.IsNullOrWhiteSpace(sni))
                        {
                            streamSettings.tlsSettings.serverName = sni;
                        }
                        else
                        {
                            streamSettings.tlsSettings.serverName = server.Address;
                        }
                    }
                    break;

                case nameof(ETransport.grpc):
                    GrpcSettings4Ray grpcSettings = new()
                    {
                        authority = host ?? string.Empty,
                        serviceName = path,
                        multiMode = server.HeaderType == Constants.GrpcMultiMode,
                        idle_timeout = Global.Settings.V2RayConfig.GrpcItem.IdleTimeout,
                        health_check_timeout = Global.Settings.V2RayConfig.GrpcItem.HealthCheckTimeout,
                        permit_without_stream = Global.Settings.V2RayConfig.GrpcItem.PermitWithoutStream,
                        initial_windows_size = Global.Settings.V2RayConfig.GrpcItem.InitialWindowsSize,
                    };
                    streamSettings.grpcSettings = grpcSettings;
                    break;

                case "hysteria":
                    var protocolExtra = server.ProtoExtra;
                    var ports = protocolExtra?.Ports;
                    int? upMbps = protocolExtra?.UpMbps is { } su and >= 0
                        ? su
                        : Global.Settings.V2RayConfig.HysteriaItem.UpMbps;
                    int? downMbps = protocolExtra?.DownMbps is { } sd and >= 0
                        ? sd
                        : Global.Settings.V2RayConfig.HysteriaItem.UpMbps;
                    var hopInterval = !protocolExtra.HopInterval.IsNullOrEmpty()
                        ? protocolExtra.HopInterval
                        : (Global.Settings.V2RayConfig.HysteriaItem.HopInterval >= 5
                            ? Global.Settings.V2RayConfig.HysteriaItem.HopInterval
                            : Constants.Hysteria2DefaultHopInt).ToString();
                    HysteriaUdpHop4Ray? udpHop = null;
                    if (!ports.IsNullOrEmpty() &&
                        (ports.Contains(':') || ports.Contains('-') || ports.Contains(',')))
                    {
                        udpHop = new HysteriaUdpHop4Ray
                        {
                            ports = ports.Replace(':', '-'),
                            interval = hopInterval,
                        };
                    }
                    streamSettings.hysteriaSettings = new()
                    {
                        version = 2,
                        auth = server.Password,
                        up = upMbps > 0 ? $"{upMbps}mbps" : null,
                        down = downMbps > 0 ? $"{downMbps}mbps" : null,
                        udphop = udpHop,
                    };
                    if (!protocolExtra.SalamanderPass.IsNullOrEmpty())
                    {
                        streamSettings.finalmask ??= new();
                        streamSettings.finalmask.udp =
                        [
                            new Mask4Ray
                            {
                                type = "salamander",
                                settings = new MaskSettings4Ray { password = protocolExtra.SalamanderPass.TrimEx(), }
                            }
                        ];
                    }
                    break;

                case "masque":
                    var masqueHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Authorization"] = $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{server.Username}:{server.Password}"))}"
                    };
                    if (server.ProtoExtra.MasqueHeaders.IsNotEmpty())
                    {
                        foreach (var line in server.ProtoExtra.MasqueHeaders.Split(['\r', '\n', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            var index = line.IndexOf(':');
                            if (index > 0 && index < line.Length - 1)
                            {
                                masqueHeaders[line[..index].Trim()] = line[(index + 1)..].Trim();
                            }
                        }
                    }
                    streamSettings.masqueSettings = new MasqueSettings4Ray
                    {
                        path = server.ProtoExtra.MasquePath.IsNullOrEmpty() ? "/" : server.ProtoExtra.MasquePath,
                        headers = masqueHeaders
                    };
                    break;

                default:
                    //tcp
                    if (server.HeaderType == Constants.TcpHeaderHttp)
                    {
                        TcpSettings4Ray tcpSettings = new()
                        {
                            header = new Header4Ray
                            {
                                type = server.HeaderType
                            }
                        };

                        //request Host
                        var request = EmbedUtils.GetEmbedText(Constants.V2raySampleHttpRequestFileName);
                        var arrHost = host.Split(',');
                        var host2 = string.Join(",".AppendQuotes(), arrHost);
                        request = request.Replace("$requestHost$", $"{host2.AppendQuotes()}");
                        request = request.Replace("$requestUserAgent$", $"{useragent.AppendQuotes()}");
                        //Path
                        var pathHttp = @"/";
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            var arrPath = path.Split(',');
                            pathHttp = string.Join(",".AppendQuotes(), arrPath);
                        }
                        request = request.Replace("$requestPath$", $"{pathHttp.AppendQuotes()}");
                        tcpSettings.header.request = JsonUtils.Deserialize<object>(request);

                        streamSettings.tcpSettings = tcpSettings;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }

        return streamSettings;
    }

    public static string getUUID(string uuid)
    {
        if (uuid.Length == 36 || uuid.Length == 32)
        {
            return uuid;
        }
        return uuid.GenerateUUIDv5();
    }

    private static Inbounds4Ray GenerateInbound()
    {
        var inbound = new Inbounds4Ray();
        inbound.tag = EInboundProtocol.mixed.ToString();
        inbound.port = Global.Settings.Socks5LocalPort;
        inbound.protocol = EInboundProtocol.mixed.ToString();
        inbound.listen = Global.Settings.LocalAddress;

        inbound.settings = new Inboundsettings4Ray();
        inbound.settings.auth = "noauth";
        inbound.settings.udp = true;

        return inbound;
    }

    private static void GenOutboundMux(Outbounds4Ray outbound, bool enabledTCP = false, bool enabledUDP = false)
    {
        try
        {
            outbound.mux = new Mux4Ray();
            outbound.mux.enabled = false;
            outbound.mux.concurrency = -1;

            if (enabledTCP)
            {
                outbound.mux.enabled = true;
                outbound.mux.concurrency = Global.Settings.V2RayConfig.Mux4RayItem.Concurrency;
            }
            else if (enabledUDP)
            {
                outbound.mux.enabled = true;
                outbound.mux.xudpConcurrency = Global.Settings.V2RayConfig.Mux4RayItem.XudpConcurrency;
                outbound.mux.xudpProxyUDP443 = Global.Settings.V2RayConfig.Mux4RayItem.XudpProxyUDP443;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }
}

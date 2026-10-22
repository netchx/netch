using System.Text.Json;
using Netch.Models;
using Netch.Utils;

#pragma warning disable VSTHRD200

namespace Netch.Servers;

public static class V2rayConfigUtils
{
    public static async Task<V2rayConfig> GenerateClientConfigAsync(Server server, bool useXray = false)
    {
        if (useXray && Global.Settings.V2RayConfig.AllowInsecure &&
            (server is VMessServer { TLSSecureType: "tls" } || server is TrojanServer { TLSSecureType: "tls" }))
            throw new MessageException("This Xray version does not support Allow Insecure for TLS. Disable it in V2Ray settings.");

        var v2rayConfig = new V2rayConfig
        {
            inbounds = new object[]
            {
                new
                {
                    port = Global.Settings.Socks5LocalPort,
                    protocol = "socks",
                    listen = Global.Settings.LocalAddress,
                    settings = new
                    {
                        auth = "noauth",
                        udp = true
                    }
                }
            }
        };

        v2rayConfig.outbounds = new[] { await outbound(server, useXray) };

        return v2rayConfig;
    }

    private static async Task<Outbound> outbound(Server server, bool useXray)
    {
        var settings = new OutboundConfiguration();
        var outbound = new Outbound
        {
            settings = settings,
            mux = new Mux()
        };

        switch (server)
        {
            case Socks5Server socks:
            {
                outbound.protocol = "socks";
                settings.servers = new object[]
                {
                    new
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = socks.Auth()
                            ? new[]
                            {
                                new
                                {
                                    user = socks.Username,
                                    pass = socks.Password,
                                    level = 1
                                }
                            }
                            : null
                    }
                };
                if (!useXray)
                    settings.version = socks.Version;

                outbound.mux.enabled = false;
                outbound.mux.concurrency = -1;
                break;
            }
            case VLESSServer vless:
            {
                outbound.protocol = "vless";
                settings.vnext = new[]
                {
                    new VnextItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = new[]
                        {
                            new User
                            {
                                id = getUUID(vless.UserID),
                                flow = vless.TLSSecureType == "xtls" || vless.Flow == "xtls-rprx-vision" ? vless.Flow : "",
                                encryption = vless.EncryptMethod
                            }
                        }
                    }
                };

                if (!useXray)
                {
                    settings.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vless.PacketEncoding : "none";
                    outbound.mux.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vless.PacketEncoding : "none";
                }

                outbound.streamSettings = boundStreamSettings(vless, useXray);

                if (vless.TLSSecureType == "xtls" || vless.Flow == "xtls-rprx-vision")
                {
                    outbound.mux.enabled = false;
                    outbound.mux.concurrency = -1;
                }
                else
                {
                    outbound.mux.enabled = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                    outbound.mux.concurrency = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                }

                break;
            }
            case VMessServer vmess:
            {
                outbound.protocol = "vmess";
                if (!useXray && vmess.EncryptMethod == "auto" && vmess.TLSSecureType != "none" && !Global.Settings.V2RayConfig.AllowInsecure)
                {
                    vmess.EncryptMethod = "zero";
                }
                settings.vnext = new[]
                {
                    new VnextItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = new[]
                        {
                            new User
                            {
                                id = getUUID(vmess.UserID),
                                alterId = vmess.AlterID,
                                security = vmess.EncryptMethod
                            }
                        }
                    }
                };

                if (!useXray)
                {
                    settings.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vmess.PacketEncoding : "none";
                    outbound.mux.packetEncoding = Global.Settings.V2RayConfig.XrayCone ? vmess.PacketEncoding : "none";
                }

                outbound.streamSettings = boundStreamSettings(vmess, useXray);

                outbound.mux.enabled = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                outbound.mux.concurrency = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                break;
            }
            case ShadowsocksServer ss:
                outbound.protocol = "shadowsocks";
                settings.servers = new[]
                {
                    new ShadowsocksServerItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = ss.EncryptMethod,
                        password = ss.Password
                    }
                };
                if (!useXray)
                {
                    settings.plugin = ss.Plugin ?? "";
                    settings.pluginOpts = ss.PluginOption ?? "";
                }
                
                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;
             case ShadowsocksRServer ssr:
                outbound.protocol = "shadowsocks";
                settings.servers = new[]
                {
                    new ShadowsocksServerItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = ssr.EncryptMethod,
                        password = ssr.Password,
                    }
                };
                settings.plugin = "shadowsocksr";
                settings.pluginArgs = new string[]
                {
                    "--obfs=" + ssr.OBFS,
                    "--obfs-param=" + (ssr.OBFSParam ?? ""),
                    "--protocol=" + ssr.Protocol,
                    "--protocol-param=" + (ssr.ProtocolParam ?? "")
                };

                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;
             case TrojanServer trojan:
                if (!new TrojanUtil().CheckServer(trojan))
                    throw new MessageException("Unsupported Trojan server settings.");

                outbound.protocol = "trojan";
                settings.servers = new[]
                {
                    new ShadowsocksServerItem // I'm not serious
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = "",
                        password = trojan.Password,
                        flow = trojan.TLSSecureType == "xtls" ? trojan.Flow : ""
                    }
                };

                outbound.streamSettings = new StreamSettings
                {
                    network = trojan.TransferProtocol,
                    security = trojan.TLSSecureType
                };
                switch (trojan.TransferProtocol)
                {
                    case "tcp":
                        break;
                    case "ws":
                        outbound.streamSettings.wsSettings = new WsSettings
                        {
                            path = trojan.Path.ValueOrDefault() ?? "/",
                            headers = new { Host = trojan.TransportHost.ValueOrDefault() }
                        };
                        break;
                    case "grpc":
                        outbound.streamSettings.grpcSettings = new GrpcSettings
                        {
                            serviceName = trojan.Path ?? "",
                            multiMode = false
                        };
                        break;
                    case "h2":
                        outbound.streamSettings.httpSettings = new HttpSettings
                        {
                            path = trojan.Path.ValueOrDefault() ?? "/",
                            host = trojan.TransportHost.SplitOrDefault()
                        };
                        break;
                    case "httpupgrade":
                        outbound.streamSettings.httpupgradeSettings = new HttpUpgradeSettings
                        {
                            host = trojan.TransportHost,
                            path = trojan.Path.ValueOrDefault() ?? "/"
                        };
                        break;
                    case "xhttp":
                        outbound.streamSettings.xhttpSettings = new XhttpSettings
                        {
                            host = trojan.TransportHost,
                            path = trojan.Path.ValueOrDefault() ?? "/",
                            mode = trojan.TransportMode,
                            extra = XhttpExtra(trojan.TransportExtra)
                        };
                        break;
                    default:
                        throw new MessageException($"Unsupported Trojan transport: {trojan.TransferProtocol}");
                }
                if (trojan.TLSSecureType != "none")
                {
                    var tlsSettings = new TlsSettings
                    {
                        allowInsecure = !useXray && Global.Settings.V2RayConfig.AllowInsecure,
                        serverName = trojan.Host.ValueOrDefault() ?? trojan.Hostname,
                        alpn = SplitAlpn(trojan.Alpn),
                        fingerprint = trojan.Fingerprint
                    };

                    switch (trojan.TLSSecureType)
                    {
                        case "tls":
                            outbound.streamSettings.tlsSettings = tlsSettings;
                            break;
                        case "xtls":
                            outbound.streamSettings.xtlsSettings = tlsSettings;
                            break;
                        case "reality":
                            outbound.streamSettings.realitySettings = Reality(trojan.Host, trojan.Hostname,
                                trojan.Fingerprint, trojan.PublicKey, trojan.ShortId, trojan.SpiderX);
                            break;
                    }
                }

                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings.sockopt = new Sockopt
                    {
                        tcpFastOpen = true
                    };
                }
                break;
            case WireGuardServer wg:
                outbound.protocol = "wireguard";
                var wireGuardAddress = await server.AutoResolveHostnameAsync();
                var localAddresses = wg.LocalAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (useXray)
                {
                    outbound.settings = new
                    {
                        noKernelTun = true,
                        secretKey = wg.PrivateKey,
                        address = localAddresses,
                        peers = new[] { new
                        {
                            publicKey = wg.PeerPublicKey,
                            preSharedKey = wg.PreSharedKey,
                            endpoint = $"{ProxyUri.Host(wireGuardAddress)}:{server.Port}"
                        } },
                        mtu = wg.MTU
                    };
                }
                else
                {
                    settings.address = wireGuardAddress;
                    settings.port = server.Port;
                    settings.localAddresses = localAddresses;
                    settings.peerPublicKey = wg.PeerPublicKey;
                    settings.privateKey = wg.PrivateKey;
                    settings.preSharedKey = wg.PreSharedKey;
                    settings.mtu = wg.MTU;
                }

                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;

            case SSHServer ssh:
                outbound.protocol = "ssh";
                settings.address = await server.AutoResolveHostnameAsync();
                settings.port = server.Port;
                settings.user = ssh.User;
                settings.password = ssh.Password;
                settings.privateKey = ssh.PrivateKey;
                settings.publicKey = ssh.PublicKey;
                
                if (Global.Settings.V2RayConfig.TCPFastOpen)
                {
                    outbound.streamSettings = new StreamSettings
                    {
                        sockopt = new Sockopt
                        {
                            tcpFastOpen = true
                        }
                    };
                }
                break;
        }

        return outbound;
    }

    private static StreamSettings boundStreamSettings(VMessServer server, bool useXray)
    {
        // https://xtls.github.io/config/transports

        if (server is VLESSServer vless ? !new VLESSUtil().CheckServer(vless) : !new VMessUtil().CheckServer(server))
            throw new MessageException("Unsupported VMess/VLESS server settings.");

        var streamSettings = new StreamSettings
        {
            network = server.TransferProtocol,
            security = server.TLSSecureType
        };

        if (server.TLSSecureType != "none")
        {
            var tlsSettings = new TlsSettings
            {
                allowInsecure = !useXray && Global.Settings.V2RayConfig.AllowInsecure,
                serverName = server.ServerName.ValueOrDefault() ?? server.Host.SplitOrDefault()?[0],
                alpn = SplitAlpn(server.Alpn),
                fingerprint = server.Fingerprint
            };

            switch (server.TLSSecureType)
            {
                case "tls":
                    streamSettings.tlsSettings = tlsSettings;
                    break;
                case "xtls":
                    streamSettings.xtlsSettings = tlsSettings;
                    break;
                case "reality":
                    streamSettings.realitySettings = Reality(server.ServerName, server.Hostname,
                        server.Fingerprint, server.PublicKey, server.ShortId, server.SpiderX);
                    break;
            }
        }

        switch (server.TransferProtocol)
        {
            case "tcp":

                streamSettings.tcpSettings = new TcpSettings
                {
                    header = new
                    {
                        type = server.FakeType,
                        request = server.FakeType switch
                        {
                            "none" => null,
                            "http" => new
                            {
                                path = server.Path.SplitOrDefault(),
                                headers = new
                                {
                                    Host = server.Host.SplitOrDefault()
                                }
                            },
                            _ => throw new MessageException($"Invalid tcp type {server.FakeType}")
                        }
                    }
                };

                break;
            case "ws":

                streamSettings.wsSettings = new WsSettings
                {
                    path = server.Path.ValueOrDefault(),
                    headers = new
                    {
                        Host = server.Host.ValueOrDefault()
                    }
                };

                break;
            case "kcp":

                streamSettings.kcpSettings = new KcpSettings
                {
                    mtu = Global.Settings.V2RayConfig.KcpConfig.mtu,
                    tti = Global.Settings.V2RayConfig.KcpConfig.tti,
                    uplinkCapacity = Global.Settings.V2RayConfig.KcpConfig.uplinkCapacity,
                    downlinkCapacity = Global.Settings.V2RayConfig.KcpConfig.downlinkCapacity,
                    congestion = Global.Settings.V2RayConfig.KcpConfig.congestion,
                    readBufferSize = Global.Settings.V2RayConfig.KcpConfig.readBufferSize,
                    writeBufferSize = Global.Settings.V2RayConfig.KcpConfig.writeBufferSize,
                    header = new
                    {
                        type = server.FakeType
                    },
                    seed = server.Path.ValueOrDefault()
                };

                break;
            case "h2":

                streamSettings.httpSettings = new HttpSettings
                {
                    host = server.Host.SplitOrDefault(),
                    path = server.Path.ValueOrDefault()
                };

                break;
            case "quic":

                streamSettings.quicSettings = new QuicSettings
                {
                    security = server.QUICSecure,
                    key = server.QUICSecret,
                    header = new
                    {
                        type = server.FakeType
                    }
                };

                break;
            case "grpc":

                streamSettings.grpcSettings = new GrpcSettings
                {
                    serviceName = server.Path,
                    multiMode = server.FakeType == "multi"
                };

                break;
            case "httpupgrade":
                streamSettings.httpupgradeSettings = new HttpUpgradeSettings
                {
                    host = server.Host,
                    path = server.Path.ValueOrDefault() ?? "/"
                };
                break;
            case "xhttp":
                streamSettings.xhttpSettings = new XhttpSettings
                {
                    host = server.Host,
                    path = server.Path.ValueOrDefault() ?? "/",
                    mode = server.TransportMode,
                    extra = XhttpExtra(server.TransportExtra)
                };
                break;
            default:
                throw new MessageException($"transfer protocol \"{server.TransferProtocol}\" not implemented yet");
        }

        if (Global.Settings.V2RayConfig.TCPFastOpen)
        {
            streamSettings.sockopt = new Sockopt
            {
                tcpFastOpen = true
            };
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

    private static string[]? SplitAlpn(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static RealitySettings Reality(string? serverName, string hostname, string? fingerprint,
        string? publicKey, string? shortId, string? spiderX)
    {
        return new RealitySettings
        {
            serverName = serverName.ValueOrDefault() ?? hostname,
            fingerprint = fingerprint.ValueOrDefault() ?? "chrome",
            publicKey = publicKey ?? "",
            shortId = shortId ?? "",
            spiderX = spiderX ?? ""
        };
    }

    private static JsonElement? XhttpExtra(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        using var document = JsonDocument.Parse(value);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new MessageException("XHTTP extra must be a JSON object.");
        return document.RootElement.Clone();
    }
}

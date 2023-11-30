using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public static class CoreConfig
{
    public static async Task<Configuration> GenerateClientConfigAsync(Server server)
    {
        var localPort = Global.Settings.Socks5LocalPort;
        var localAddress = Global.Settings.LocalAddress;

        var Config = new Configuration
        {
            inbounds = GetInbounds(server, localPort, localAddress).ToArray(),
            outbounds = new[] { await OutboundAsync(server) }
        };

        return Config;
    }

    private static IEnumerable<object> GetInbounds(Server server, int localPort, string localAddress)
    {
        var inboundSettings = new { udp = true, auth = "noauth"/*, allowTransparent = false */ };
        var sniffingSettings = (Global.Settings.V2RayConfig.Sniffing/* && server.Sniffing   */) ? new { enabled = true, routeOnly = false, destOverride = new object[] { "http", "tls" } } : null;

        yield return new
        {
            port = localPort,
            protocol = "socks",
            listen = localAddress,
            settings = inboundSettings,
            sniffing = (Global.Settings.V2RayConfig.Sniffing/* && server.Sniffing   */ && server is VisionServer vision && vision.TLSSecureType == "reality") ? new { enabled = true, routeOnly = true, destOverride = new object[] { "http", "tls", "quic" } } : sniffingSettings
        };

        if (Global.Settings.V2RayConfig.AllowHttp/* && server.AllowHttp */)
        {
            yield return new
            {
                protocol = "http",
                port = localPort + 1,
                listen = localAddress,
                settings = inboundSettings,
                sniffing = sniffingSettings
            };
        }
    }

    private static async Task<Outbound> OutboundAsync(Server server)
    {
        var outbound = new Outbound
        {
            settings = new OutboundConfiguration(),
            mux = new Mux()
        };

        switch (server)
        {
            case Socks5Server socks:
            {
                outbound.protocol = "socks";
                outbound.settings.servers = new object[]
                {
                    new
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = socks.Auth() ? new[]
                        {
                            new
                            {
                                user = socks.Username,
                                pass = socks.Password,
                                level = 1
                            }
                        } : null
                    }
                };
                outbound.settings.version = socks.Version;

                outbound.mux = null; 
//              outbound.mux.enabled = false;
//              outbound.mux.concurrency = -1;

                break;
            }
            case VisionServer vision:
            {
                outbound.protocol = "vless";
                outbound.settings.vnext = new[]
                {
                    new VnextItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = new[]
                        {
                            new User
                            {
                                id = GetUUID(vision.UserID),
                                encryption = vision.EncryptMethod,
                                flow = vision.Flow.ValueOrDefault()
                            }
                        }
                    }
                };

                if (vision.PacketEncoding != "none")
                {
                    outbound.settings.packetEncoding = Global.Settings.V2RayConfig.XrayFullCone ? vision.PacketEncoding : "none";
                }

                outbound.streamSettings = BoundStreamSettings(vision);

                if (vision.UseMux != true)
                {
                    outbound.mux = null;
                }
                else
                {
                    outbound.mux = new Mux
                    {
                        enabled = vision.UseMux ?? Global.Settings.V2RayConfig.UseMux,
                        concurrency = vision.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1,
                        packetEncoding = (vision.PacketEncoding != "none") ? (Global.Settings.V2RayConfig.XrayFullCone ? vision.PacketEncoding : "none") : null
                    };
                }

                break;
            }
            case VLESSServer vless:
            {
                outbound.protocol = "vless";
                outbound.settings.vnext = new[]
                {
                    new VnextItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = new[]
                        {
                            new User
                            {
                                id = GetUUID(vless.UserID),
                                encryption = vless.EncryptMethod,
                                flow = vless.TLSSecureType == "xtls" ? "xtls-rprx-direct" : ""
                            }
                        }
                    }
                };

                if (vless.PacketEncoding != "none")
                {
                    outbound.settings.packetEncoding = Global.Settings.V2RayConfig.XrayFullCone ? vless.PacketEncoding : "none";
                }

                outbound.streamSettings = BoundStreamSettings(vless);

                if (vless.UseMux != true)
                {
                    outbound.mux = null;
                }
                else
                {
                    if (vless.TLSSecureType != "tls")
                    {
                        outbound.mux = null;
                    }
                    else
                    {
                        outbound.mux = new Mux
                        {
                            enabled = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux,
                            concurrency = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1,
                            packetEncoding = (vless.PacketEncoding != "none") ? (Global.Settings.V2RayConfig.XrayFullCone ? vless.PacketEncoding : "none") : null
                        };
                    }
                }

                break;
            }
            case VMessServer vmess:
            {
                outbound.protocol = "vmess";
                if (vmess.EncryptMethod == "auto" && vmess.TLSSecureType != "none" && !Global.Settings.V2RayConfig.AllowInsecure)
                {
                    vmess.EncryptMethod = "zero";
                }

                outbound.settings.vnext = new[]
                {
                    new VnextItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        users = new[]
                        {
                            new User
                            {
                                id = GetUUID(vmess.UserID),
                                alterId = vmess.AlterID,
                                security = vmess.EncryptMethod
                            }
                        }
                    }
                };

                if (vmess.PacketEncoding != "none")
                {
                    outbound.settings.packetEncoding = Global.Settings.V2RayConfig.XrayFullCone ? vmess.PacketEncoding : "none";
                }

                outbound.streamSettings = BoundStreamSettings(vmess);

                if (vmess.UseMux != true)
                {
                    outbound.mux = null;
                }
                else
                {
                    outbound.mux = new Mux
                    {
                        enabled = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux,
                        concurrency = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1,
                        packetEncoding = (vmess.PacketEncoding != "none") ? (Global.Settings.V2RayConfig.XrayFullCone ? vmess.PacketEncoding : "none") : null
                    };
                }

                break;
            }
            case ShadowsocksServer ss:
            {
                outbound.protocol = "shadowsocks";
                outbound.settings.servers = new[]
                {
                    new ShadowsocksServerItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = ss.EncryptMethod,
                        password = ss.Password
                    }
                };

                outbound.settings.plugin = ss.Plugin ?? "";
                outbound.settings.pluginOpts = ss.PluginOption ?? "";
                
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
            case ShadowsocksRServer ssr:
            {
                outbound.protocol = "shadowsocks";
                outbound.settings.servers = new[]
                {
                    new ShadowsocksServerItem
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = ssr.EncryptMethod,
                        password = ssr.Password,
                    }
                };

                outbound.settings.plugin = "shadowsocksr";
                outbound.settings.pluginArgs = new string[]
                {
                    "--obfs=" + ssr.OBFS,
                    "--obfs-param=" + ssr.OBFSParam ?? "",
                    "--protocol=" + ssr.Protocol,
                    "--protocol-param=" + ssr.ProtocolParam ?? ""
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
            }
            case TrojanServer trojan:
            { 
                outbound.protocol = "trojan";
                outbound.settings.servers = new[]
                {
                    new ShadowsocksServerItem // I'm not serious
                    {
                        address = await server.AutoResolveHostnameAsync(),
                        port = server.Port,
                        method = "",
                        password = trojan.Password,
                        flow = trojan.TLSSecureType == "xtls" ? "xtls-rprx-direct" : ""
                    }
                };

                outbound.streamSettings = new StreamSettings
                {
                    network = "tcp",
                    security = trojan.TLSSecureType
                };

                if (trojan.TLSSecureType != "none")
                {
                    var tlsSettings = new TlsSettings
                    {
                        allowInsecure = Global.Settings.V2RayConfig.AllowInsecure,
                        serverName = trojan.Host ?? ""
                    };

                    switch (trojan.TLSSecureType)
                    {
                        case "tls":
                            outbound.streamSettings.tlsSettings = tlsSettings;
                            break;
                        case "xtls":
                            outbound.streamSettings.xtlsSettings = tlsSettings;
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
            }
            case WireGuardServer wg:
            {
                outbound.protocol = "wireguard";
                outbound.settings.address = await server.AutoResolveHostnameAsync();
                outbound.settings.port = server.Port;
                outbound.settings.localAddresses = wg.LocalAddresses.SplitOrDefault();
                outbound.settings.peerPublicKey = wg.PeerPublicKey;
                outbound.settings.privateKey = wg.PrivateKey;
                outbound.settings.preSharedKey = wg.PreSharedKey;
                outbound.settings.mtu = wg.MTU;

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

            case SSHServer ssh:
            {
                outbound.protocol = "ssh";
                outbound.settings.address = await server.AutoResolveHostnameAsync();
                outbound.settings.port = server.Port;
                outbound.settings.user = ssh.User;
                outbound.settings.password = ssh.Password;
                outbound.settings.privateKey = ssh.PrivateKey;
                outbound.settings.publicKey = ssh.PublicKey;
                
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
        }

        return outbound;
    }

    private static StreamSettings BoundStreamSettings(VMessServer server)
    {
        // https://xtls.github.io/config/transports

        var streamSettings = new StreamSettings
        {
            network = server.TransferProtocol,
            security = server.TLSSecureType
        };

        if (server.TLSSecureType != "none")
        {
            var tlsSettings = new TlsSettings
            {
                fingerprint = Global.Settings.V2RayConfig.Fingerprint,
                serverName = server.ServerName.ValueOrDefault() ?? server.Host.SplitOrDefault()?[0]
            };

            if (server.TLSSecureType == "tls")
            {
                tlsSettings.allowInsecure = Global.Settings.V2RayConfig.AllowInsecure;
                tlsSettings.alpn = server.Alpn.SplitOrDefault() ?? Global.Settings.V2RayConfig.Alpn[2]?.Split(',').Select(s => s.Trim()).ToArray();
            }
            else if (server is VisionServer vision && server.TLSSecureType == "reality")
            {
                tlsSettings.publicKey = vision.PublicKey.ValueOrDefault();
                tlsSettings.spiderX = vision.SpiderX.ValueOrDefault();
                tlsSettings.shortId = vision.ShortId.ValueOrDefault();
            }

            switch (server.TLSSecureType)
            {
                case "tls":
                    streamSettings.tlsSettings = tlsSettings;
                    break;
                case "xtls":
                    streamSettings.xtlsSettings = tlsSettings;
                    break;
                case "reality":
                    streamSettings.realitySettings = tlsSettings;
                    break;
            }
        }

        switch (server.TransferProtocol)
        {
            case "tcp":

                if (server is not VisionServer)
                {
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

                }

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

    public static string GetUUID(string uuid)
    {
        if (uuid.Length == 36 || uuid.Length == 32)
        {
            return uuid;
        }
        return uuid.GenerateUUIDv5();
    }
}
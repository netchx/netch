using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers.V2ray.Models;
using Netch.Utils;
using V2rayConfig = Netch.Servers.V2ray.Models.V2rayConfig;

#pragma warning disable VSTHRD200

namespace Netch.Servers.Utils
{
    public static class V2rayConfigUtils
    {
        public static async Task<V2rayConfig> GenerateClientConfigAsync(Server server)
        {
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
                            udp = true
                        }
                    }
                }
            };

            v2rayConfig.outbounds = new[] { await outbound(server) };

            return v2rayConfig;
        }

        private static async Task<Outbound> outbound(Server server)
        {
            var outbound = new Outbound
            {
                settings = new OutboundConfiguration(),
                mux = new Mux()
            };

            switch (server)
            {
                case Socks5 socks5:
                {
                    outbound.protocol = "socks";
                    outbound.settings.servers = new object[]
                    {
                        new
                        {
                            address = await server.AutoResolveHostnameAsync(),
                            port = server.Port,
                            users = socks5.Auth()
                                ? new[]
                                {
                                    new
                                    {
                                        user = socks5.Username,
                                        pass = socks5.Password,
                                        level = 1
                                    }
                                }
                                : null
                        }
                    };

                    outbound.mux.enabled = false;
                    outbound.mux.concurrency = -1;
                    break;
                }
                case VLESS vless:
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
                                    id = vless.UserID,
                                    flow = vless.Flow.ValueOrDefault(),
                                    encryption = vless.EncryptMethod
                                }
                            }
                        }
                    };

                    outbound.streamSettings = boundStreamSettings(vless);

                    if (vless.TLSSecureType == "xtls")
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
                case VMess vmess:
                {
                    outbound.protocol = "vmess";
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
                                    id = vmess.UserID,
                                    alterId = vmess.AlterID,
                                    security = vmess.EncryptMethod
                                }
                            }
                        }
                    };

                    outbound.streamSettings = boundStreamSettings(vmess);

                    outbound.mux.enabled = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                    outbound.mux.concurrency = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                    break;
                }
            }

            return outbound;
        }

        private static StreamSettings boundStreamSettings(VMess server)
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
                    allowInsecure = Global.Settings.V2RayConfig.AllowInsecure,
                    serverName = server.ServerName.ValueOrDefault() ?? server.Hostname
                };

                switch (server.TLSSecureType)
                {
                    case "tls":
                        streamSettings.tlsSettings = tlsSettings;
                        break;
                    case "xtls":
                        streamSettings.xtlsSettings = tlsSettings;
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
                default:
                    throw new MessageException($"transfer protocol \"{server.TransferProtocol}\" not implemented yet");
            }

            return streamSettings;
        }
    }
}
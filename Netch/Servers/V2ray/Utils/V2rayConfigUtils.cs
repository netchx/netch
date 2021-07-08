using System.Diagnostics;
using System.Text.Json;
using Netch.Models;
using Netch.Servers.V2ray.Models;
using Netch.Utils;
using V2rayConfig = Netch.Servers.V2ray.Models.V2rayConfig;

namespace Netch.Servers.Utils
{
    public static class V2rayConfigUtils
    {
        public static string GenerateClientConfig(Server server)
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

            outbound(server, ref v2rayConfig);

            return JsonSerializer.Serialize(v2rayConfig, Global.NewDefaultJsonSerializerOptions);
        }

        private static void outbound(Server server, ref V2rayConfig v2rayConfig)
        {
            var outbound = new Outbound
            {
                settings = new OutboundConfiguration(),
                mux = new Mux(),
                streamSettings = new StreamSettings
                {
                    network = "tcp"
                }
            };

            v2rayConfig.outbounds = new[] { outbound };

            switch (server)
            {
                case Socks5 socks5:
                {
                    outbound.protocol = "socks";
                    outbound.settings.servers = new object[]
                    {
                        new
                        {
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
                                : null,
                            address = server.AutoResolveHostname(),
                            port = server.Port
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
                            address = server.AutoResolveHostname(),
                            port = server.Port,
                            users = new[]
                            {
                                new User
                                {
                                    id = vless.UserID,
                                    alterId = 0,
                                    flow = vless.Flow.ValueOrDefault(),
                                    encryption = vless.EncryptMethod
                                }
                            }
                        }
                    };

                    var streamSettings = outbound.streamSettings;
                    boundStreamSettings(vless, ref streamSettings);

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
                            address = server.AutoResolveHostname(),
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

                    var streamSettings = outbound.streamSettings;
                    boundStreamSettings(vmess, ref streamSettings);

                    outbound.mux.enabled = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                    outbound.mux.concurrency = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                    break;
                }
            }
        }

        private static void boundStreamSettings(VMess server, ref StreamSettings streamSettings)
        {
            // https://xtls.github.io/config/transports

            streamSettings.network = server.TransferProtocol;
            streamSettings.security = server.TLSSecureType;

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
                            request = server.FakeType is "http"
                                ? new
                                {
                                    path = server.Path.SplitOrDefault(),
                                    headers = new
                                    {
                                        Host = server.Host.SplitOrDefault()
                                    }
                                }
                                : null
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
                    Trace.Assert(false);
                    break;
            }
        }
    }
}
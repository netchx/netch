using System.Collections.Generic;
using Netch.Models;
using Netch.Servers.VMess.Models;
using Newtonsoft.Json;
using V2rayConfig = Netch.Servers.VMess.Models.V2rayConfig;

namespace Netch.Servers.VMess.Utils
{
    public static class V2rayConfigUtils
    {
        public static string GenerateClientConfig(Server server, Mode mode)
        {
            try
            {
                var v2rayConfig = new V2rayConfig();

                inbound(server, ref v2rayConfig);

                routing(server, mode, ref v2rayConfig);

                outbound(server, ref v2rayConfig);

                return JsonConvert.SerializeObject(v2rayConfig);
            }
            catch
            {
                return "";
            }
        }

        private static void inbound(Server server, ref V2rayConfig v2rayConfig)
        {
            try
            {
                var inbound = new Inbounds
                {
                    port = Global.Settings.Socks5LocalPort,
                    protocol = "socks",
                    listen = Global.Settings.LocalAddress,
                    settings = new Inboundsettings
                    {
                        udp = true
                    }
                };

                v2rayConfig.inbounds = new List<Inbounds>
                {
                    inbound
                };
            }
            catch
            {
                // ignored
            }
        }

        private static void routing(Server server, Mode mode, ref V2rayConfig v2rayConfig)
        {
            try
            {
                RulesItem rulesItem;
                if (mode.BypassChina)
                {
                    rulesItem = new RulesItem
                    {
                        type = "field",
                        ip = new List<string>
                        {
                            "geoip:cn",
                            "geoip:private"
                        },
                        domain = new List<string>
                        {
                            "geosite:cn"
                        },
                        outboundTag = "direct"
                    };
                }
                else
                {
                    rulesItem = new RulesItem
                    {
                        type = "field",
                        ip = new List<string>
                        {
                            "geoip:private"
                        },
                        outboundTag = "direct"
                    };
                }

                v2rayConfig.routing = new Routing
                {
                    rules = new List<RulesItem>
                    {
                        rulesItem
                    }
                };
            }
            catch
            {
                // ignored
            }
        }

        private static void outbound(Server server, ref V2rayConfig v2rayConfig)
        {
            try
            {
                var outbound = new Outbounds
                {
                    settings = new Outboundsettings(),
                    mux = new Mux(),
                    streamSettings = new StreamSettings
                    {
                        network = "tcp"
                    }
                };

                switch (server)
                {
                    case Socks5.Socks5 socks5:
                    {
                        var serversItem = new ServersItem
                        {
                            users = new List<SocksUsersItem>()
                        };
                        outbound.settings.servers = new List<ServersItem>
                        {
                            serversItem
                        };

                        serversItem.address = server.AutoResolveHostname();
                        serversItem.port = server.Port;
                        serversItem.method = null;
                        serversItem.password = null;

                        if (socks5.Auth())
                        {
                            var socksUsersItem = new SocksUsersItem
                            {
                                user = socks5.Username,
                                pass = socks5.Password,
                                level = 1
                            };

                            serversItem.users.Add(socksUsersItem);
                        }

                        outbound.mux.enabled = false;
                        outbound.mux.concurrency = -1;
                        outbound.protocol = "socks";
                        break;
                    }
                    case VLESS.VLESS vless:
                    {
                        var vnextItem = new VnextItem
                        {
                            users = new List<UsersItem>()
                        };
                        outbound.settings.vnext = new List<VnextItem>
                        {
                            vnextItem
                        };

                        vnextItem.address = server.AutoResolveHostname();
                        vnextItem.port = server.Port;

                        var usersItem = new UsersItem();
                        vnextItem.users.Add(usersItem);

                        usersItem.id = vless.UserID;
                        usersItem.alterId = 0;
                        usersItem.flow = string.Empty;
                        usersItem.encryption = vless.EncryptMethod;

                        outbound.mux.enabled = vless.UseMux;
                        outbound.mux.concurrency = vless.UseMux ? 8 : -1;

                        var streamSettings = outbound.streamSettings;
                        boundStreamSettings(vless, ref streamSettings);

                        if (vless.TransferProtocol == "xtls")
                        {
                            usersItem.flow = string.IsNullOrEmpty(vless.Flow) ? "xtls-rprx-origin" : vless.Flow;

                            outbound.mux.enabled = false;
                            outbound.mux.concurrency = -1;
                        }

                        outbound.protocol = "vless";
                        outbound.settings.servers = null;
                        break;
                    }
                    case VMess vmess:
                    {
                        var vnextItem = new VnextItem
                        {
                            users = new List<UsersItem>()
                        };
                        outbound.settings.vnext = new List<VnextItem>
                        {
                            vnextItem
                        };

                        vnextItem.address = server.AutoResolveHostname();
                        vnextItem.port = server.Port;

                        var usersItem = new UsersItem();
                        vnextItem.users.Add(usersItem);

                        usersItem.id = vmess.UserID;
                        usersItem.alterId = vmess.AlterID;
                        usersItem.security = vmess.EncryptMethod;

                        outbound.mux.enabled = vmess.UseMux;
                        outbound.mux.concurrency = vmess.UseMux ? 8 : -1;

                        var streamSettings = outbound.streamSettings;
                        boundStreamSettings(vmess, ref streamSettings);

                        outbound.protocol = "vmess";
                        break;
                    }
                }

                v2rayConfig.outbounds = new List<Outbounds> {outbound};
            }
            catch
            {
                // ignored
            }
        }

        private static void boundStreamSettings(VMess server, ref StreamSettings streamSettings)
        {
            try
            {
                streamSettings.network = server.TransferProtocol;
                var host = server.Host;
                if (server.TLSSecure ?? Global.Settings.V2RayConfig.AllowInsecure)
                {
                    streamSettings.security = "tls";

                    var tlsSettings = new TlsSettings
                    {
                        allowInsecure = true
                    };
                    if (!string.IsNullOrWhiteSpace(host))
                    {
                        tlsSettings.serverName = host;
                    }

                    streamSettings.tlsSettings = tlsSettings;
                }
                else
                {
                    streamSettings.security = "";
                }

                switch (server.TransferProtocol)
                {
                    case "kcp":
                        var kcpSettings = new KcpSettings
                        {
                            mtu = Global.Settings.V2RayConfig.KcpConfig.mtu,
                            tti = Global.Settings.V2RayConfig.KcpConfig.tti,
                            uplinkCapacity = Global.Settings.V2RayConfig.KcpConfig.uplinkCapacity,
                            downlinkCapacity = Global.Settings.V2RayConfig.KcpConfig.downlinkCapacity,
                            congestion = Global.Settings.V2RayConfig.KcpConfig.congestion,
                            readBufferSize = Global.Settings.V2RayConfig.KcpConfig.readBufferSize,
                            writeBufferSize = Global.Settings.V2RayConfig.KcpConfig.writeBufferSize,
                            header = new Header
                            {
                                type = server.FakeType
                            },
                            seed = !string.IsNullOrWhiteSpace(server.Path) ? server.Path : null
                        };


                        streamSettings.kcpSettings = kcpSettings;
                        break;
                    case "ws":
                        var path = server.Path;
                        var wsSettings = new WsSettings
                        {
                            connectionReuse = true,
                            headers = !string.IsNullOrWhiteSpace(host)
                                ? new Headers
                                {
                                    Host = host
                                }
                                : null,
                            path = !string.IsNullOrWhiteSpace(path) ? path : null
                        };

                        streamSettings.wsSettings = wsSettings;
                        break;
                    case "h2":
                        var httpSettings = new HttpSettings
                        {
                            host = new List<string>
                            {
                                string.IsNullOrWhiteSpace(server.Host) ? server.Hostname : server.Host
                            },
                            path = server.Path
                        };

                        streamSettings.httpSettings = httpSettings;
                        break;
                    case "quic":
                        var quicSettings = new QuicSettings
                        {
                            security = host,
                            key = server.Path,
                            header = new Header
                            {
                                type = server.FakeType
                            }
                        };
                        if (server.TLSSecure ?? Global.Settings.V2RayConfig.AllowInsecure)
                        {
                            streamSettings.tlsSettings.serverName = server.Hostname;
                        }

                        streamSettings.quicSettings = quicSettings;
                        break;
                    case "xtls":
                        streamSettings.security = server.TransferProtocol;

                        var xtlsSettings = new TlsSettings
                        {
                            allowInsecure = true
                        };
                        if (!string.IsNullOrWhiteSpace(host))
                        {
                            xtlsSettings.serverName = host;
                        }

                        streamSettings.xtlsSettings = xtlsSettings;
                        break;
                    default:
                        if (server.FakeType == "http")
                        {
                            var tcpSettings = new TcpSettings
                            {
                                connectionReuse = true,
                                header = new Header
                                {
                                    type = server.FakeType,
                                    request = new TCPRequest
                                    {
                                        path = string.IsNullOrWhiteSpace(server.Path) ? "/" : server.Path,
                                        headers = new TCPRequestHeaders
                                        {
                                            Host = string.IsNullOrWhiteSpace(server.Host) ? server.Hostname : server.Host
                                        }
                                    }
                                }
                            };

                            streamSettings.tcpSettings = tcpSettings;
                        }

                        break;
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
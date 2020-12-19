using System.Collections.Generic;
using System.Linq;
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

                outbound(server, mode, ref v2rayConfig);

                return JsonConvert.SerializeObject(v2rayConfig, Formatting.Indented, new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
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
                var directRuleObject = new RulesItem
                {
                    type = "field",
                    ip = new List<string>(),
                    domain = new List<string>(),
                    outboundTag = "direct"
                };

                var blockRuleObject = new RulesItem
                {
                    type = "field",
                    ip = new List<string>(),
                    domain = new List<string>(),
                    outboundTag = "block"
                };

                if (mode.BypassChina)
                {
                    switch (mode.Type)
                    {
                        case 0:
                            directRuleObject.ip.Add("geoip:cn");
                            break;
                        case 1:
                        case 2:
                            if (Global.Flags.SupportFakeDns && Global.Settings.TUNTAP.UseFakeDNS)
                                directRuleObject.domain.Add("geosite:cn");
                            else
                                directRuleObject.ip.Add("geoip:cn");
                            break;
                        default:
                            directRuleObject.domain.Add("geosite:cn");
                            break;
                    }
                }

                if (mode.Type is 0 or 1 or 2)
                {
                    blockRuleObject.ip.Add("geoip:private");
                }

                v2rayConfig.routing = new Routing
                {
                    rules = new List<RulesItem>()
                };

                static bool CheckRuleItem(ref RulesItem rulesItem)
                {
                    bool ipResult, domainResult;
                    if (!(ipResult = rulesItem.ip.Any()))
                    {
                        rulesItem.ip = null;
                    }

                    if (!(domainResult = rulesItem.domain.Any()))
                    {
                        rulesItem.domain = null;
                    }

                    return ipResult || domainResult;
                }

                if (CheckRuleItem(ref directRuleObject))
                    v2rayConfig.routing.rules.Add(directRuleObject);
                if (CheckRuleItem(ref blockRuleObject))
                    v2rayConfig.routing.rules.Add(blockRuleObject);
            }
            catch
            {
                // ignored
            }
        }

        private static void outbound(Server server, Mode mode, ref V2rayConfig v2rayConfig)
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
                        outbound.settings.servers = new List<ServersItem>
                        {
                            new ServersItem
                            {
                                users = socks5.Auth()
                                    ? new List<SocksUsersItem>
                                    {
                                        new SocksUsersItem
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
                        outbound.protocol = "socks";
                        break;
                    }
                    case VLESS.VLESS vless:
                    {
                        var vnextItem = new VnextItem
                        {
                            users = new List<UsersItem>(),
                            address = server.AutoResolveHostname(),
                            port = server.Port
                        };
                        outbound.settings.vnext = new List<VnextItem> {vnextItem};

                        var usersItem = new UsersItem
                        {
                            id = vless.UserID,
                            alterId = 0,
                            flow = string.Empty,
                            encryption = vless.EncryptMethod
                        };
                        vnextItem.users.Add(usersItem);

                        var streamSettings = outbound.streamSettings;
                        boundStreamSettings(vless, ref streamSettings);

                        if (vless.TLSSecureType == "xtls")
                        {
                            usersItem.flow = string.IsNullOrEmpty(vless.Flow) ? "xtls-rprx-origin" : vless.Flow;

                            outbound.mux.enabled = false;
                            outbound.mux.concurrency = -1;
                        }
                        else
                        {
                            outbound.mux.enabled = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                            outbound.mux.concurrency = vless.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                        }

                        outbound.protocol = "vless";
                        outbound.settings.servers = null;
                        break;
                    }
                    case VMess vmess:
                    {
                        var vnextItem = new VnextItem
                        {
                            users = new List<UsersItem>(),
                            address = server.AutoResolveHostname(),
                            port = server.Port
                        };
                        outbound.settings.vnext = new List<VnextItem> {vnextItem};

                        var usersItem = new UsersItem
                        {
                            id = vmess.UserID,
                            alterId = vmess.AlterID,
                            security = vmess.EncryptMethod
                        };
                        vnextItem.users.Add(usersItem);

                        var streamSettings = outbound.streamSettings;
                        boundStreamSettings(vmess, ref streamSettings);

                        outbound.mux.enabled = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux;
                        outbound.mux.concurrency = vmess.UseMux ?? Global.Settings.V2RayConfig.UseMux ? 8 : -1;
                        outbound.protocol = "vmess";
                        break;
                    }
                }

                v2rayConfig.outbounds = new List<Outbounds>
                {
                    outbound,
                    new Outbounds
                    {
                        tag = "direct", protocol = "freedom"
                    },
                    new Outbounds
                    {
                        tag = "block", protocol = "blackhole"
                    }
                };
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

                if ((streamSettings.security = server.TLSSecureType) != "")
                {
                    var tlsSettings = new TlsSettings
                    {
                        allowInsecure = Global.Settings.V2RayConfig.AllowInsecure,
                        serverName = !string.IsNullOrWhiteSpace(server.Host) ? server.Host : null
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
                        var wsSettings = new WsSettings
                        {
                            headers = !string.IsNullOrWhiteSpace(server.Host)
                                ? new Headers {Host = server.Host}
                                : null,
                            path = !string.IsNullOrWhiteSpace(server.Path) ? server.Path : null
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
                            security = server.Host,
                            key = server.Path,
                            header = new Header
                            {
                                type = server.FakeType
                            }
                        };

                        if (server.TLSSecureType != "")
                        {
                            // tls or xtls
                            streamSettings.tlsSettings.serverName = server.Hostname;
                        }

                        streamSettings.quicSettings = quicSettings;
                        break;
                    default:
                        if (server.FakeType == "http")
                        {
                            var tcpSettings = new TcpSettings
                            {
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
using System.Collections.Generic;
using System.IO;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.VMess.Models;
using Newtonsoft.Json;

namespace Netch.Servers.VMess
{
    public class VMessController : Guard, IServerController
    {
        public VMessController()
        {
            StartedKeywords.Add("started");
            StoppedKeywords.AddRange(new[] {"config file not readable", "failed to"});
        }


        public override string Name { get; protected set; } = "V2Ray";
        public override string MainFile { get; protected set; } = "v2ray.exe";
        public int? Socks5LocalPort { get; set; }
        public string LocalAddress { get; set; }


        public bool Start(Server s, Mode mode)
        {
            var server = (VMess) s;
            File.WriteAllText("data\\last.json", JsonConvert.SerializeObject(new VMessConfig.Config()
            {
                inbounds = new List<VMessConfig.Inbounds>
                {
                    new VMessConfig.Inbounds
                    {
                        settings = new VMessConfig.InboundSettings(),
                        port = Socks5LocalPort ?? Global.Settings.Socks5LocalPort,
                        listen = LocalAddress ?? Global.Settings.LocalAddress
                    }
                },
                outbounds = new List<VMessConfig.Outbounds>
                {
                    new VMessConfig.Outbounds
                    {
                        settings = new VMessConfig.OutboundSettings
                        {
                            vnext = new List<VMessConfig.VNext>
                            {
                                new VMessConfig.VNext
                                {
                                    address = server.Hostname,
                                    port = server.Port,
                                    users = new List<VMessConfig.User>
                                    {
                                        new VMessConfig.User
                                        {
                                            id = server.UserID,
                                            alterId = server.AlterID,
                                            security = server.EncryptMethod
                                        }
                                    }
                                }
                            }
                        },
                        streamSettings = new VMessConfig.StreamSettings
                        {
                            network = server.TransferProtocol,
                            security = server.TLSSecure ? "tls" : string.Empty,
                            wsSettings = server.TransferProtocol == "ws"
                                ? new VMessConfig.WebSocketSettings
                                {
                                    path = server.Path == string.Empty ? "/" : server.Path,
                                    headers = new VMessConfig.WSHeaders
                                    {
                                        Host = server.Host == string.Empty ? server.Hostname : server.Host
                                    }
                                }
                                : null,
                            tcpSettings = server.FakeType == "http"
                                ? new VMessConfig.TCPSettings
                                {
                                    header = new VMessConfig.TCPHeaders
                                    {
                                        type = server.FakeType,
                                        request = new VMessConfig.TCPRequest
                                        {
                                            path = server.Path == string.Empty ? "/" : server.Path,
                                            headers = new VMessConfig.TCPRequestHeaders
                                            {
                                                Host = server.Host == string.Empty ? server.Hostname : server.Host
                                            }
                                        }
                                    }
                                }
                                : null,
                            kcpSettings = server.TransferProtocol == "kcp"
                                ? new VMessConfig.KCPSettings
                                {
                                    header = new VMessConfig.TCPHeaders
                                    {
                                        type = server.FakeType
                                    }
                                }
                                : null,
                            quicSettings = server.TransferProtocol == "quic"
                                ? new VMessConfig.QUICSettings
                                {
                                    security = server.QUICSecure,
                                    key = server.QUICSecret,
                                    header = new VMessConfig.TCPHeaders
                                    {
                                        type = server.FakeType
                                    }
                                }
                                : null,
                            httpSettings = server.TransferProtocol == "h2"
                                ? new VMessConfig.HTTPSettings
                                {
                                    host = server.Host == "" ? server.Hostname : server.Host,
                                    path = server.Path == "" ? "/" : server.Path
                                }
                                : null,
                            tlsSettings = new VMessConfig.TLSSettings
                            {
                                allowInsecure = true,
                                serverName = server.Host == "" ? server.Hostname : server.Host
                            }
                        },
                        mux = new VMessConfig.OutboundMux
                        {
                            enabled = server.UseMux
                        }
                    },
                    mode.Type == 0 || mode.Type == 1 || mode.Type == 2
                        ? new VMessConfig.Outbounds
                        {
                            tag = "TUNTAP",
                            protocol = "freedom"
                        }
                        : new VMessConfig.Outbounds
                        {
                            tag = "direct",
                            protocol = "freedom"
                        }
                },
                routing = new VMessConfig.Routing
                {
                    rules = new List<VMessConfig.RoutingRules>
                    {
                        mode.BypassChina
                            ? new VMessConfig.RoutingRules
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
                            }
                            : new VMessConfig.RoutingRules
                            {
                                type = "field",
                                ip = new List<string>
                                {
                                    "geoip:private"
                                },
                                outboundTag = "direct"
                            }
                    }
                }
            }));

            if (StartInstanceAuto("-config ..\\data\\last.json"))
            {
                if (File.Exists("data\\last.json")) File.Delete("data\\last.json");
                return true;
            }

            return false;
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
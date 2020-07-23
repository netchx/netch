using System.Collections.Generic;
using System.IO;
using System.Threading;
using Netch.Models;
using Netch.Utils;
using Newtonsoft.Json;
using VMess = Netch.Models.Information.VMess;

namespace Netch.Controllers
{
    public class VMessController : EncryptedProxy
    {
        public VMessController()
        {
            Name = "V2Ray";
            MainFile = "v2ray.exe";
            StartedKeywords("started");
            StoppedKeywords("config file not readable", "failed to");
        }

        public override bool Start(Server server, Mode mode)
        {
            File.WriteAllText("data\\last.json", JsonConvert.SerializeObject(new VMess.Config
            {
                inbounds = new List<VMess.Inbounds>
                {
                    new VMess.Inbounds
                    {
                        settings = new VMess.InboundSettings(),
                        port = Global.Settings.Socks5LocalPort,
                        listen = Global.Settings.LocalAddress
                    }
                },
                outbounds = new List<VMess.Outbounds>
                {
                    new VMess.Outbounds
                    {
                        settings = new VMess.OutboundSettings
                        {
                            vnext = new List<VMess.VNext>
                            {
                                new VMess.VNext
                                {
                                    address = server.Hostname,
                                    port = server.Port,
                                    users = new List<VMess.User>
                                    {
                                        new VMess.User
                                        {
                                            id = server.UserID,
                                            alterId = server.AlterID,
                                            security = server.EncryptMethod
                                        }
                                    }
                                }
                            }
                        },
                        streamSettings = new VMess.StreamSettings
                        {
                            network = server.TransferProtocol,
                            security = server.TLSSecure ? "tls" : string.Empty,
                            wsSettings = server.TransferProtocol == "ws"
                                ? new VMess.WebSocketSettings
                                {
                                    path = server.Path == string.Empty ? "/" : server.Path,
                                    headers = new VMess.WSHeaders
                                    {
                                        Host = server.Host == string.Empty ? server.Hostname : server.Host
                                    }
                                }
                                : null,
                            tcpSettings = server.FakeType == "http"
                                ? new VMess.TCPSettings
                                {
                                    header = new VMess.TCPHeaders
                                    {
                                        type = server.FakeType,
                                        request = new VMess.TCPRequest
                                        {
                                            path = server.Path == string.Empty ? "/" : server.Path,
                                            headers = new VMess.TCPRequestHeaders
                                            {
                                                Host = server.Host == string.Empty ? server.Hostname : server.Host
                                            }
                                        }
                                    }
                                }
                                : null,
                            kcpSettings = server.TransferProtocol == "kcp"
                                ? new VMess.KCPSettings
                                {
                                    header = new VMess.TCPHeaders
                                    {
                                        type = server.FakeType
                                    }
                                }
                                : null,
                            quicSettings = server.TransferProtocol == "quic"
                                ? new VMess.QUICSettings
                                {
                                    security = server.QUICSecure,
                                    key = server.QUICSecret,
                                    header = new VMess.TCPHeaders
                                    {
                                        type = server.FakeType
                                    }
                                }
                                : null,
                            httpSettings = server.TransferProtocol == "h2"
                                ? new VMess.HTTPSettings
                                {
                                    host = server.Host == "" ? server.Hostname : server.Host,
                                    path = server.Path == "" ? "/" : server.Path
                                }
                                : null,
                            tlsSettings = new VMess.TLSSettings
                            {
                                allowInsecure = true,
                                serverName = server.Host == "" ? server.Hostname : server.Host
                            }
                        },
                        mux = new VMess.OutboundMux
                        {
                            enabled = server.UseMux
                        }
                    },
                    mode.Type == 0 || mode.Type == 1 || mode.Type == 2
                        ? new VMess.Outbounds
                        {
                            tag = "TUNTAP",
                            protocol = "freedom"
                        }
                        : new VMess.Outbounds
                        {
                            tag = "direct",
                            protocol = "freedom"
                        }
                },
                routing = new VMess.Routing
                {
                    rules = new List<VMess.RoutingRules>
                    {
                        mode.BypassChina
                            ? new VMess.RoutingRules
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
                            : new VMess.RoutingRules
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

            Instance = GetProcess();
            Instance.StartInfo.Arguments = "-config ..\\data\\last.json";

            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            State = State.Starting;
            Instance.Start();
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == State.Started)
                {
                    if (File.Exists("data\\last.json")) File.Delete("data\\last.json");

                    return true;
                }

                if (State == State.Stopped)
                {
                    Logging.Error("V2Ray 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Logging.Error("V2Ray 进程启动超时");
            Stop();
            return false;
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Netch.Controllers
{
    public class VMessController
    {
        /// <summary>
        ///		进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        ///		当前状态
        /// </summary>
        public Objects.State State = Objects.State.Waiting;

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
            foreach (var proc in Process.GetProcessesByName("v2ray"))
            {
                try
                {
                    proc.Kill();
                }
                catch (Exception)
                {
                    // 跳过
                }
            }

            if (!File.Exists("bin\\v2ray.exe") || !File.Exists("bin\\v2ctl.exe"))
            {
                return false;
            }

            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }

            File.WriteAllText("data\\last.json", Newtonsoft.Json.JsonConvert.SerializeObject(new Objects.Information.VMess.Config()
            {
                inbounds = new List<Objects.Information.VMess.Inbounds>()
                {
                    new Objects.Information.VMess.Inbounds()
                    {
                        settings = new Objects.Information.VMess.InboundSettings()
                    }
                },
                outbounds = new List<Objects.Information.VMess.Outbounds>()
                {
                    new Objects.Information.VMess.Outbounds()
                    {
                        settings = new Objects.Information.VMess.OutboundSettings()
                        {
                            vnext = new List<Objects.Information.VMess.VNext>()
                            {
                                new Objects.Information.VMess.VNext()
                                {
                                    address = server.Address,
                                    port = server.Port,
                                    users = new List<Objects.Information.VMess.User>
                                    {
                                        new Objects.Information.VMess.User()
                                        {
                                            id = server.UserID,
                                            alterId = server.AlterID,
                                            security = server.EncryptMethod
                                        }
                                    }
                                }
                            }
                        },
                        streamSettings = new Objects.Information.VMess.StreamSettings()
                        {
                            network = server.TransferProtocol,
                            security = server.TLSSecure == true ? "tls" : "",
                            wsSettings = server.TransferProtocol == "ws" ? new Objects.Information.VMess.WebSocketSettings()
                            {
                                path = server.Path == "" ? "/" : server.Path,
                                headers = new Objects.Information.VMess.WSHeaders()
                                {
                                    Host = server.Host == "" ? server.Address : server.Host
                                }
                            } : null,
                            tcpSettings = server.FakeType == "http" ? new Objects.Information.VMess.TCPSettings()
                            {
                                header = new Objects.Information.VMess.TCPHeaders()
                                {
                                    type = server.FakeType,
                                    request = new Objects.Information.VMess.TCPRequest()
                                    {
                                        path = server.Path == "" ? "/" : server.Path,
                                        headers = new Objects.Information.VMess.TCPRequestHeaders()
                                        {
                                            Host = server.Host == "" ? server.Address : server.Host
                                        }
                                    }
                                }
                            } : null,
                            kcpSettings = server.TransferProtocol == "kcp" ? new Objects.Information.VMess.KCPSettings()
                            {
                                header = new Objects.Information.VMess.TCPHeaders()
                                {
                                    type = server.FakeType
                                }
                            } : null,
                            quicSettings = server.TransferProtocol == "quic" ? new Objects.Information.VMess.QUICSettings()
                            {
                                security = server.QUICSecurity,
                                key = server.QUICSecret,
                                header = new Objects.Information.VMess.TCPHeaders()
                                {
                                    type = server.FakeType
                                }
                            } : null,
                            httpSettings = server.TransferProtocol == "h2" ? new Objects.Information.VMess.HTTPSettings()
                            {
                                host = server.Host == "" ? server.Address : server.Host,
                                path = server.Path == "" ? "/" : server.Path
                            } : null,
                            tlsSettings = new Objects.Information.VMess.TLSSettings()
                        },
                        mux = new Objects.Information.VMess.OutboundMux()
                    },
                    new Objects.Information.VMess.Outbounds()
                    {
                        tag = "direct",
                        protocol = "freedom",
                        settings = null,
                        streamSettings = null,
                        mux = null
                    }
                },
                routing = new Objects.Information.VMess.Routing()
                {
                    rules = new List<Objects.Information.VMess.RoutingRules>()
                    {
                        mode.BypassChina == true ? new Objects.Information.VMess.RoutingRules()
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
                        } : new Objects.Information.VMess.RoutingRules()
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

            // 清理上一次的日志文件，防止淤积占用磁盘空间
            if (Directory.Exists("logging"))
            {
                if (File.Exists("logging\\v2ray.log"))
                {
                    File.Delete("logging\\v2ray.log");
                }
            }

            Instance = MainController.GetProcess();
            Instance.StartInfo.FileName = "bin\\v2ray.exe";
            Instance.StartInfo.Arguments = "-config ..\\data\\last.json";

            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            State = Objects.State.Starting;
            Instance.Start();
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == Objects.State.Started)
                {
                    if (File.Exists("data\\last.json"))
                    {
                        File.Delete("data\\last.json");
                    }
                    return true;
                }

                if (State == Objects.State.Stopped)
                {
                    Utils.Logging.Info("V2Ray 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Utils.Logging.Info("V2Ray 进程启动超时");
            Stop();
            return false;
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            try
            {
                if (Instance != null && !Instance.HasExited)
                {
                    Instance.Kill();
                }
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(e.Data))
            {
                File.AppendAllText("logging\\v2ray.log", $"{e.Data}\r\n");

                if (State == Objects.State.Starting)
                {
                    if (Instance.HasExited)
                    {
                        State = Objects.State.Stopped;
                    }
                    else if (e.Data.Contains("started"))
                    {
                        State = Objects.State.Started;
                    }
                    else if (e.Data.Contains("config file not readable") || e.Data.Contains("failed to"))
                    {
                        State = Objects.State.Stopped;
                    }
                }
            }
        }
    }
}

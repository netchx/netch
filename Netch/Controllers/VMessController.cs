using Netch.Forms;
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
        public Models.State State = Models.State.Waiting;

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Models.Server server, Models.Mode mode)
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting V2ray")}");
            if (!File.Exists("bin\\v2ray.exe") || !File.Exists("bin\\v2ctl.exe"))
            {
                return false;
            }

            File.WriteAllText("data\\last.json", Newtonsoft.Json.JsonConvert.SerializeObject(new Models.Information.VMess.Config
            {
                inbounds = new List<Models.Information.VMess.Inbounds>
                {
                    new Models.Information.VMess.Inbounds
                    {
                        settings = new Models.Information.VMess.InboundSettings(),
                        port = Global.Settings.Socks5LocalPort,
                        listen = Global.Settings.LocalAddress
                    }
                },
                outbounds = new List<Models.Information.VMess.Outbounds>
                {
                    new Models.Information.VMess.Outbounds
                    {
                        settings = new Models.Information.VMess.OutboundSettings
                        {
                            vnext = new List<Models.Information.VMess.VNext>
                            {
                                new Models.Information.VMess.VNext
                                {
                                    address = server.Hostname,
                                    port = server.Port,
                                    users = new List<Models.Information.VMess.User>
                                    {
                                        new Models.Information.VMess.User
                                        {
                                            id = server.UserID,
                                            alterId = server.AlterID,
                                            security = server.EncryptMethod
                                        }
                                    }
                                }
                            }
                        },
                        streamSettings = new Models.Information.VMess.StreamSettings
                        {
                            network = server.TransferProtocol,
                            security = server.TLSSecure ? "tls" : "",
                            wsSettings = server.TransferProtocol == "ws" ? new Models.Information.VMess.WebSocketSettings
                            {
                                path = server.Path == "" ? "/" : server.Path,
                                headers = new Models.Information.VMess.WSHeaders
                                {
                                    Host = server.Host == "" ? server.Hostname : server.Host
                                }
                            } : null,
                            tcpSettings = server.FakeType == "http" ? new Models.Information.VMess.TCPSettings
                            {
                                header = new Models.Information.VMess.TCPHeaders
                                {
                                    type = server.FakeType,
                                    request = new Models.Information.VMess.TCPRequest
                                    {
                                        path = server.Path == "" ? "/" : server.Path,
                                        headers = new Models.Information.VMess.TCPRequestHeaders
                                        {
                                            Host = server.Host == "" ? server.Hostname : server.Host
                                        }
                                    }
                                }
                            } : null,
                            kcpSettings = server.TransferProtocol == "kcp" ? new Models.Information.VMess.KCPSettings
                            {
                                header = new Models.Information.VMess.TCPHeaders
                                {
                                    type = server.FakeType
                                }
                            } : null,
                            quicSettings = server.TransferProtocol == "quic" ? new Models.Information.VMess.QUICSettings
                            {
                                security = server.QUICSecure,
                                key = server.QUICSecret,
                                header = new Models.Information.VMess.TCPHeaders
                                {
                                    type = server.FakeType
                                }
                            } : null,
                            httpSettings = server.TransferProtocol == "h2" ? new Models.Information.VMess.HTTPSettings
                            {
                                host = server.Host == "" ? server.Hostname : server.Host,
                                path = server.Path == "" ? "/" : server.Path
                            } : null,
                            tlsSettings = new Models.Information.VMess.TLSSettings
                            {
                                allowInsecure = true,
                                serverName = server.Host == "" ? server.Hostname : server.Host
                            }
                        },
                        mux = new Models.Information.VMess.OutboundMux
                        {
                            enabled = server.UseMux
                        }
                    }
                },
                routing = new Models.Information.VMess.Routing
                {
                    rules = new List<Models.Information.VMess.RoutingRules>
                    {
                        mode.BypassChina ? new Models.Information.VMess.RoutingRules
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
                        } : new Models.Information.VMess.RoutingRules
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

            State = Models.State.Starting;
            Instance.Start();
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == Models.State.Started)
                {
                    if (File.Exists("data\\last.json"))
                    {
                        File.Delete("data\\last.json");
                    }
                    return true;
                }

                if (State == Models.State.Stopped)
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
                    Instance.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                File.AppendAllText("logging\\v2ray.log", $"{e.Data}\r\n");

                if (State == Models.State.Starting)
                {
                    if (Instance.HasExited)
                    {
                        State = Models.State.Stopped;
                    }
                    else if (e.Data.Contains("started"))
                    {
                        State = Models.State.Started;
                    }
                    else if (e.Data.Contains("config file not readable") || e.Data.Contains("failed to"))
                    {
                        State = Models.State.Stopped;
                    }
                }
            }
        }
    }
}

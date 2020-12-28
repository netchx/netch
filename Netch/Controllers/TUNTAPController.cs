using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;

namespace Netch.Controllers
{
    public class TUNTAPController : Guard, IModeController
    {
        
        /// <summary>
        ///     服务器 IP 地址
        /// </summary>
        private IPAddress _serverAddresses;

        /// <summary>
        ///     本地 DNS 服务控制器
        /// </summary>
        public DNSController DNSController = new DNSController();

        public TUNTAPController()
        {
            StartedKeywords.Add("Running");
            StoppedKeywords.AddRange(new[] {"failed", "invalid vconfig file"});
        }

        public override string Name { get; protected set; } = "tun2socks";
        public override string MainFile { get; protected set; } = "tun2socks.exe";

        public bool Start(in Mode mode)
        {
            var server = MainController.Server;
            // 查询服务器 IP 地址
            _serverAddresses = DNS.Lookup(server.Hostname);

            // 查找出口适配器
            if (!Utils.Utils.SearchOutboundAdapter())
            {
                return false;
            }

            // 查找并安装 TAP 适配器
            if (!SearchTapAdapter())
            {
                if (!AddTap())
                {
                    Logging.Error("Tap 适配器安装失败");
                    return false;
                }

                SearchTapAdapter();
            }


            SetupRouteTable(mode);

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", Name));

            string dns;
            if (Global.Settings.TUNTAP.UseCustomDNS)
            {
                if (Global.Settings.TUNTAP.DNS.Any())
                {
                    dns = DNS.Join(Global.Settings.TUNTAP.DNS);
                }
                else
                {
                    Global.Settings.TUNTAP.DNS.Add("1.1.1.1");
                    dns = "1.1.1.1";
                }
            }
            else
            {
                try
                {
                    MainController.PortCheckAndShowMessageBox(53, "DNS");
                }
                catch
                {
                    return false;
                }

                if (!DNSController.Start())
                {
                    Logging.Error("AioDNS 启动失败");
                    return false;
                }

                dns = "127.0.0.1";
            }

            var argument = new StringBuilder();
            if (server is Socks5 socks5 && !socks5.Auth())
                argument.Append($"-proxyServer {server.AutoResolveHostname()}:{server.Port} ");
            else
                argument.Append($"-proxyServer 127.0.0.1:{Global.Settings.Socks5LocalPort} ");

            argument.Append(
                $"-tunAddr {Global.Settings.TUNTAP.Address} -tunMask {Global.Settings.TUNTAP.Netmask} -tunGw {Global.Settings.TUNTAP.Gateway} -tunDns {dns} -tunName \"{TUNTAP.GetName(Global.TUNTAP.ComponentID)}\" ");

            if (Global.Settings.TUNTAP.UseFakeDNS && Global.Flags.SupportFakeDns)
                argument.Append("-fakeDns ");

            return StartInstanceAuto(argument.ToString(), ProcessPriorityClass.RealTime);
        }

        /// <summary>
        ///     TUN/TAP停止
        /// </summary>
        public override void Stop()
        {
            var tasks = new[]
            {
                Task.Run(StopInstance),
                Task.Run(ClearRouteTable),
                Task.Run(DNSController.Stop)
            };
            Task.WaitAll(tasks);
        }

        private readonly List<string> _directIPs = new List<string>();

        private readonly List<string> _proxyIPs = new List<string>();

        /// <summary>
        ///     设置绕行规则
        /// </summary>
        /// <returns>是否设置成功</returns>
        private void SetupRouteTable(Mode mode)
        {
            Global.MainForm.StatusText(i18N.Translate("SetupBypass"));
            Logging.Info("设置路由规则");

            #region Rule IPs

            switch (mode.Type)
            {
                case 1:
                    // 代理规则
                    Logging.Info("代理 → 规则 IP");
                    RouteAction(Action.Create, mode.FullRule, RouteType.TUNTAP);

                    //处理 NAT 类型检测，由于协议的原因，无法仅通过域名确定需要代理的 IP，自己记录解析了返回的 IP，仅支持默认检测服务器
                    if (Global.Settings.STUN_Server == "stun.stunprotocol.org")
                    {
                        try
                        {
                            Logging.Info("代理 → STUN 服务器 IP");
                            RouteAction(Action.Create,
                                new[]
                                {
                                    Dns.GetHostAddresses(Global.Settings.STUN_Server)[0],
                                    Dns.GetHostAddresses("stunresponse.coldthunder11.com")[0]
                                }.Select(ip => $"{ip}/32"),
                                RouteType.TUNTAP);
                        }
                        catch
                        {
                            Logging.Info("NAT 类型测试域名解析失败，将不会被添加到代理列表");
                        }
                    }

                    if (Global.Settings.TUNTAP.ProxyDNS)
                    {
                        Logging.Info("代理 → 自定义 DNS");
                        if (Global.Settings.TUNTAP.UseCustomDNS)
                        {
                            RouteAction(Action.Create,
                                Global.Settings.TUNTAP.DNS.Select(ip => $"{ip}/32"),
                                RouteType.TUNTAP);
                        }
                        else
                        {
                            RouteAction(Action.Create,
                                new[] {"1.1.1.1", "8.8.8.8", "9.9.9.9", "185.222.222.222"}.Select(ip => $"{ip}/32"),
                                RouteType.TUNTAP);
                        }
                    }

                    break;
                case 2:
                    // 绕过规则

                    // 将 TUN/TAP 网卡权重放到最高
                    Process.Start(new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"interface ip set interface {Global.TUNTAP.Index} metric=0",
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = true,
                            CreateNoWindow = true
                        }
                    );

                    Logging.Info("绕行 → 规则 IP");
                    RouteAction(Action.Create, mode.FullRule, RouteType.Outbound);
                    break;
            }

            #endregion

            Logging.Info("绕行 → 服务器 IP");
            if (!IPAddress.IsLoopback(_serverAddresses))
                RouteAction(Action.Create, $"{_serverAddresses}/32", RouteType.Outbound);

            Logging.Info("绕行 → 全局绕过 IP");
            RouteAction(Action.Create, Global.Settings.BypassIPs, RouteType.Outbound);

            if (mode.Type == 2)
            {
                // 绕过规则
                Logging.Info("代理 → 全局");
                RouteAction(Action.Create, "0.0.0.0/0", RouteType.TUNTAP);
            }
        }


        /// <summary>
        ///     清除绕行规则
        /// </summary>
        private bool ClearRouteTable()
        {
            RouteAction(Action.Delete, _directIPs, RouteType.Outbound);
            RouteAction(Action.Delete, _proxyIPs, RouteType.TUNTAP);
            _directIPs.Clear();
            _proxyIPs.Clear();
            return true;
        }


        public bool TestFakeDNS()
        {
            try
            {
                InitInstance("-h");
                Instance.Start();
                return Instance.StandardError.ReadToEnd().Contains("-fakeDns");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     搜索出口和TUNTAP适配器
        /// </summary>
        public static bool SearchTapAdapter()
        {
            Global.TUNTAP.Adapter = null;
            Global.TUNTAP.Index = -1;
            Global.TUNTAP.ComponentID = TUNTAP.GetComponentID();

            // 搜索 TUN/TAP 适配器的索引
            if (string.IsNullOrEmpty(Global.TUNTAP.ComponentID))
            {
                Logging.Info("TAP 适配器未安装");
                return false;
            }

            // 根据 ComponentID 寻找 Tap适配器
            try
            {
                var adapter = NetworkInterface.GetAllNetworkInterfaces().First(_ => _.Id == Global.TUNTAP.ComponentID);
                Global.TUNTAP.Adapter = adapter;
                Global.TUNTAP.Index = adapter.GetIPProperties().GetIPv4Properties().Index;
                Logging.Info(
                    $"TAP 适配器：{adapter.Name} {adapter.Id} {adapter.Description}, index: {Global.TUNTAP.Index}");
                return true;
            }
            catch (Exception e)
            {
                var msg = e switch
                {
                    InvalidOperationException _ => $"找不到标识符为 {Global.TUNTAP.ComponentID} 的 TAP 适配器: {e.Message}",
                    NetworkInformationException _ => $"获取 Tap 适配器信息错误: {e.Message}",
                    _ => $"Tap 适配器其他异常: {e}"
                };
                Logging.Error(msg);
                return false;
            }
        }

        private static bool AddTap()
        {
            TUNTAP.addtap();
            // 给点时间，不然立马安装完毕就查找适配器可能会导致找不到适配器ID
            Thread.Sleep(1000);
            if (string.IsNullOrEmpty(Global.TUNTAP.ComponentID = TUNTAP.GetComponentID()))
            {
                Logging.Error("找不到 TAP 适配器，驱动可能安装失败");
                return false;
            }

            return true;
        }


        private enum RouteType
        {
            Outbound,
            TUNTAP
        }

        private enum Action
        {
            Create,
            Delete
        }

        private void RouteAction(Action action, in IEnumerable<string> ipNetworks, RouteType routeType,
            int metric = 0)
        {
            foreach (var address in ipNetworks)
            {
                RouteAction(action, address, routeType, metric);
            }
        }

        private bool RouteAction(Action action, in string ipNetwork, RouteType routeType, int metric = 0)
        {
            string gateway;
            int index;
            switch (routeType)
            {
                case RouteType.Outbound:
                    gateway = Global.Outbound.Gateway.ToString();
                    index = Global.Outbound.Index;
                    break;
                case RouteType.TUNTAP:
                    gateway = Global.Settings.TUNTAP.Gateway;
                    index = Global.TUNTAP.Index;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(routeType), routeType, null);
            }

            string network;
            ushort cidr;
            try
            {
                var s = ipNetwork.Split('/');
                network = s[0];
                cidr = ushort.Parse(s[1]);
            }
            catch
            {
                Logging.Warning($"Failed to parse rule {ipNetwork}");
                return false;
            }

            bool result;
            switch (action)
            {
                case Action.Create:
                {
                    result = NativeMethods.CreateRoute(network, cidr, gateway, index, metric);
                    switch (routeType)
                    {
                        case RouteType.Outbound:
                            _directIPs.Add(ipNetwork);
                            break;
                        case RouteType.TUNTAP:
                            _proxyIPs.Add(ipNetwork);
                            break;
                    }

                    break;
                }
                case Action.Delete:
                    result = NativeMethods.DeleteRoute(network, cidr, gateway, index, metric);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            if (!result)
            {
                Logging.Warning($"Failed to {action} Route on {routeType} Adapter: {ipNetwork} metric {metric}");
            }

            return result;
        }
    }
}
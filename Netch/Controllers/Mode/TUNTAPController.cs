using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Controllers
{
    public class TUNTAPController : ModeController
    {
        // ByPassLan IP
        private readonly List<string> _bypassLanIPs = new List<string> {"10.0.0.0/8", "172.16.0.0/16", "192.168.0.0/16"};

        private Mode _savedMode = new Mode();
        private Server _savedServer = new Server();

        /// <summary>
        ///     服务器 IP 地址
        /// </summary>
        private IPAddress[] _serverAddresses = new IPAddress[0];

        /// <summary>
        ///     本地 DNS 服务控制器
        /// </summary>
        public DNSController pDNSController = new DNSController();

        public TUNTAPController()
        {
            Name = "Tap";
            MainFile = "tun2socks.exe";
            StartedKeywords("Running");
            StoppedKeywords("failed", "invalid vconfig file");
        }

        /// <summary>
        ///     配置 TUNTAP 适配器
        /// </summary>
        private bool Configure()
        {
            // 查询服务器 IP 地址
            var destination = Dns.GetHostAddressesAsync(_savedServer.Hostname);
            if (destination.Wait(1000))
            {
                if (destination.Result.Length == 0) return false;

                _serverAddresses = destination.Result;
            }

            // 搜索出口
            return SearchAdapters();
        }

        private readonly List<IPNetwork> _directIPs = new List<IPNetwork>();
        private readonly List<IPNetwork> _proxyIPs = new List<IPNetwork>();

        /// <summary>
        ///     设置绕行规则
        /// </summary>
        /// <returns>是否设置成功</returns>
        private bool SetupRouteTable()
        {
            Logging.Info("收集路由表规则");
            Global.MainForm.StatusText(i18N.Translate("SetupBypass"));

            Logging.Info("绕行 → 全局绕过 IP");
            _directIPs.AddRange(Global.Settings.BypassIPs.Select(IPNetwork.Parse));

            Logging.Info("绕行 → 服务器 IP");
            _directIPs.AddRange(_serverAddresses.Where(address => !IPAddress.IsLoopback(address)).Select(address => IPNetwork.Parse(address.ToString(), 32)));

            if (_savedMode.BypassChina)
            {
                Logging.Info("绕行 → 中国 IP");
                _directIPs.AddRange(Encoding.UTF8.GetString(Resources.CNIP).Split('\n').Select(IPNetwork.Parse));
            }

            Logging.Info("绕行 → 局域网 IP");
            _directIPs.AddRange(_bypassLanIPs.Select(IPNetwork.Parse));

            switch (_savedMode.Type)
            {
                case 1:
                    // 代理规则
                    Logging.Info("代理 → 规则 IP");
                    _proxyIPs.AddRange(_savedMode.Rule.Select(IPNetwork.Parse));

                    //处理 NAT 类型检测，由于协议的原因，无法仅通过域名确定需要代理的 IP，自己记录解析了返回的 IP，仅支持默认检测服务器
                    if (Global.Settings.STUN_Server == "stun.stunprotocol.org")
                        try
                        {
                            Logging.Info("代理 → STUN 服务器 IP");
                            _proxyIPs.AddRange(new[]
                            {
                                Dns.GetHostAddresses(Global.Settings.STUN_Server)[0],
                                Dns.GetHostAddresses("stunresponse.coldthunder11.com")[0]
                            }.Select(ip => IPNetwork.Parse(ip.ToString(), 32)));
                        }
                        catch
                        {
                            Logging.Info("NAT 类型测试域名解析失败，将不会被添加到代理列表");
                        }

                    if (Global.Settings.TUNTAP.ProxyDNS)
                    {
                        Logging.Info("代理 → 自定义 DNS");
                        if (Global.Settings.TUNTAP.UseCustomDNS)
                        {
                            var dns = string.Empty;
                            foreach (var value in Global.Settings.TUNTAP.DNS)
                            {
                                dns += value;
                                dns += ',';
                            }

                            dns = dns.Trim();
                            dns = dns.Substring(0, dns.Length - 1);
                            RouteAction(Action.Create, dns, 32, RouteType.TUNTAP);
                        }
                        else
                        {
                            _proxyIPs.AddRange(new[] {"1.1.1.1", "8.8.8.8", "9.9.9.9", "185.222.222.222"}.Select(ip => IPNetwork.Parse(ip, 32)));
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
                    _directIPs.AddRange(_savedMode.Rule.Select(IPNetwork.Parse));

                    Logging.Info("代理 → 全局");
                    if (!RouteAction(Action.Create, IPNetwork.Parse("0.0.0.0", 0), RouteType.TUNTAP))
                    {
                        State = State.Stopped;
                        return false;
                    }

                    break;
            }

            Logging.Info("设置路由规则");
            RouteAction(Action.Create, _directIPs, RouteType.Gateway);
            RouteAction(Action.Create, _proxyIPs, RouteType.TUNTAP);

            return true;
        }


        /// <summary>
        ///     清除绕行规则
        /// </summary>
        private bool ClearBypass()
        {
            switch (_savedMode.Type)
            {
                case 1:
                    break;
                case 2:
                    RouteAction(Action.Delete, "0.0.0.0", 0, RouteType.TUNTAP, 10);
                    break;
            }

            RouteAction(Action.Delete, _directIPs, RouteType.Gateway);
            RouteAction(Action.Delete, _proxyIPs, RouteType.TUNTAP);
            _directIPs.Clear();
            _proxyIPs.Clear();
            return true;
        }

        public override bool Start(Server server, Mode mode)
        {
            Global.MainForm.StatusText(i18N.Translate("Starting Tap"));

            _savedMode = mode;
            _savedServer = server;

            if (!Configure()) return false;

            SetupRouteTable();

            Instance = GetProcess();

            var adapterName = TUNTAP.GetName(Global.TUNTAP.ComponentID);

            string dns;
            //V2ray使用Unbound本地DNS会导致查询异常缓慢故此V2ray不启动unbound而是使用自定义DNS
            //if (Global.Settings.TUNTAP.UseCustomDNS || server.Type.Equals("VMess"))
            if (Global.Settings.TUNTAP.UseCustomDNS)
            {
                dns = string.Empty;
                foreach (var value in Global.Settings.TUNTAP.DNS)
                {
                    dns += value;
                    dns += ',';
                }

                dns = dns.Trim();
                dns = dns.Substring(0, dns.Length - 1);
            }
            else
            {
                pDNSController.Start();
                dns = "127.0.0.1";
            }

            if (Global.Settings.TUNTAP.UseFakeDNS) dns += " -fakeDns";

            if (server.Type == "Socks5")
                Instance.StartInfo.Arguments = $"-proxyServer {server.Hostname}:{server.Port} -tunAddr {Global.Settings.TUNTAP.Address} -tunMask {Global.Settings.TUNTAP.Netmask} -tunGw {Global.Settings.TUNTAP.Gateway} -tunDns {dns} -tunName \"{adapterName}\"";
            else
                Instance.StartInfo.Arguments = $"-proxyServer 127.0.0.1:{Global.Settings.Socks5LocalPort} -tunAddr {Global.Settings.TUNTAP.Address} -tunMask {Global.Settings.TUNTAP.Netmask} -tunGw {Global.Settings.TUNTAP.Gateway} -tunDns {dns} -tunName \"{adapterName}\"";

            Instance.ErrorDataReceived += OnOutputDataReceived;
            Instance.OutputDataReceived += OnOutputDataReceived;

            State = State.Starting;
            Instance.Start();
            Instance.BeginErrorReadLine();
            Instance.BeginOutputReadLine();
            Instance.PriorityClass = ProcessPriorityClass.RealTime;

            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                switch (State)
                {
                    case State.Started:
                        return true;
                    case State.Stopped:
                        Stop();
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        ///     TUN/TAP停止
        /// </summary>
        public override void Stop()
        {
            var tasks = new[]
            {
                Task.Factory.StartNew(StopInstance),
                Task.Factory.StartNew(ClearBypass),
                Task.Factory.StartNew(pDNSController.Stop)
            };
            Task.WaitAll(tasks);
        }

        /// <summary>
        ///     搜索出口和TUNTAP适配器
        /// </summary>
        private static bool SearchAdapters()
        {
            // 寻找出口适配器
            if (Win32Native.GetBestRoute(BitConverter.ToUInt32(IPAddress.Parse("114.114.114.114").GetAddressBytes(), 0), 0, out var pRoute) != 0)
            {
                Logging.Error("GetBestRoute 搜索失败(找不到出口适配器)");
                return false;
            }

            Global.Adapter.Index = pRoute.dwForwardIfIndex;

            // 搜索 TUN/TAP 适配器的索引
            if (string.IsNullOrEmpty(Global.TUNTAP.ComponentID = TUNTAP.GetComponentID()))
            {
                Logging.Info("找不到 TAP 适配器");
                if (MessageBoxX.Show(i18N.Translate("TUN/TAP driver is not detected. Is it installed now?"), confirm: true) == DialogResult.OK)
                {
                    Configuration.addtap();
                    // 给点时间，不然立马安装完毕就查找适配器可能会导致找不到适配器ID
                    Thread.Sleep(1000);
                    if (string.IsNullOrEmpty(Global.TUNTAP.ComponentID = TUNTAP.GetComponentID()))
                    {
                        Logging.Error("找不到 TAP 适配器，驱动可能安装失败");
                        return false;
                    }
                }
                else
                {
                    Logging.Info("取消安装 TAP 驱动 ");
                    return false;
                }
            }

            try
            {
                try
                {
                    var adapter = NetworkInterface.GetAllNetworkInterfaces().First(_ => _.GetIPProperties().GetIPv4Properties().Index == Global.Adapter.Index);
                    Global.Adapter.Address = adapter.GetIPProperties().UnicastAddresses.First(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork).Address;
                    Global.Adapter.Gateway = new IPAddress(pRoute.dwForwardNextHop);
                    Logging.Info($"出口 IPv4 地址：{Global.Adapter.Address}");
                    Logging.Info($"出口 网关 地址：{Global.Adapter.Gateway}");
                    Logging.Info($"出口适配器：{adapter.Name} {adapter.Id} {adapter.Description}, index: {Global.Adapter.Index}");
                    // Ex NetworkInformationException: 此接口不支持 IPv4 协议。

                    // Ex NetworkInformationException: Windows 系统函数调用失败。
                    // Ex System.ArgumentNullException: source 或 predicate 为 null。
                }
                catch (Exception e)
                {
                    if (e is InvalidOperationException)
                        Logging.Error($"找不到网络接口索引为 {Global.Adapter.Index} 的出口适配器");
                    throw;
                }

                try
                {
                    var adapter = NetworkInterface.GetAllNetworkInterfaces().First(_ => _.Id == Global.TUNTAP.ComponentID);
                    Global.TUNTAP.Adapter = adapter;
                    Global.TUNTAP.Index = adapter.GetIPProperties().GetIPv4Properties().Index;
                    Logging.Info($"TAP 适配器：{adapter.Name} {adapter.Id} {adapter.Description}, index: {Global.TUNTAP.Index}");
                }
                catch (Exception e)
                {
                    if (e is InvalidOperationException)
                        Logging.Error($"找不到标识符为 {Global.TUNTAP.ComponentID} 的 TAP 适配器");
                    throw;
                }

                return true;
            }
            catch (InvalidOperationException)
            {
                // 理论上如果得到了网络接口的索引/网络适配器的标识符不会找不到网卡
                // 只是异常处理
                Logging.Info("所有适配器:\n" +
                             NetworkInterface.GetAllNetworkInterfaces().Aggregate(string.Empty, (current, adapter)
                                 => current + $"{adapter.Name} {adapter.Id} {adapter.Description}, index: {Global.TUNTAP.Index}{Global.EOF}"));
                return false;
            }
            catch (NetworkInformationException e)
            {
                if (e.ErrorCode == 10043)
                    MessageBoxX.Show("适配器未开启IPv4协议", LogLevel.ERROR, owner: Global.MainForm);
                return false;
            }
        }


        private enum RouteType
        {
            Gateway,
            TUNTAP
        }

        private enum Action
        {
            Create,
            Delete
        }

        private static bool RouteAction(Action action, IEnumerable<IPNetwork> ipNetworks, RouteType routeType, int metric = 0)
        {
            return ipNetworks.All(address => RouteAction(action, address, routeType, metric));
        }

        private static bool RouteAction(Action action, string address, byte cidr, RouteType routeType, int metric = 0)
        {
            return RouteAction(action, IPNetwork.Parse(address, cidr), routeType, metric);
        }

        private static bool RouteAction(Action action, IPNetwork ipNetwork, RouteType routeType, int metric = 0)
        {
            string gateway;
            int index;
            switch (routeType)
            {
                case RouteType.Gateway:
                    gateway = Global.Adapter.Gateway.ToString();
                    index = Global.Adapter.Index;
                    break;
                case RouteType.TUNTAP:
                    gateway = Global.Settings.TUNTAP.Gateway;
                    index = Global.TUNTAP.Index;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(routeType), routeType, null);
            }

            return action switch
            {
                Action.Create => NativeMethods.CreateRoute(ipNetwork.Network.ToString(), ipNetwork.Cidr, gateway, index, metric),
                Action.Delete => NativeMethods.DeleteRoute(ipNetwork.Network.ToString(), ipNetwork.Cidr, gateway, index, metric),
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
            };
        }
    }
}
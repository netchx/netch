using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;
using Vanara.PInvoke;
using static Netch.Controllers.TUNInterop;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace Netch.Controllers
{
    public class TUNController : IModeController
    {
        private readonly List<string> _directIPs = new();

        private readonly List<string> _proxyIPs = new();
        /// <summary>
        ///     服务器 IP 地址
        /// </summary>
        private IPAddress _serverAddresses = null!;

        /// <summary>
        ///     本地 DNS 服务控制器
        /// </summary>
        public readonly DNSController DNSController = new();

        public string Name { get; } = "tun2socks";

        private readonly OutboundAdapter _outboundAdapter = new();
        private IAdapter _tunAdapter = null!;
        private readonly TUNInterop _tunInterop = new();

        public void Start(in Mode mode)
        {
            var server = MainController.Server!;
            _serverAddresses = DnsUtils.Lookup(server.Hostname)!; // server address have been cached when MainController.Start

            _tunInterop.Dial(NameList.TYPE_ADAPMTU, "1500");
            _tunInterop.Dial(NameList.TYPE_BYPBIND, "10.0.0.100");
            _tunInterop.Dial(NameList.TYPE_BYPLIST, "disabled");


            #region Server

            _tunInterop.Dial(NameList.TYPE_TCPREST, "");
            _tunInterop.Dial(NameList.TYPE_TCPTYPE, "Socks5");

            _tunInterop.Dial(NameList.TYPE_UDPREST, "");
            _tunInterop.Dial(NameList.TYPE_UDPTYPE, "Socks5");

            if (server is Socks5 socks5)
            {
                _tunInterop.Dial(NameList.TYPE_TCPHOST, $"{server.AutoResolveHostname()}:{server.Port}");

                _tunInterop.Dial(NameList.TYPE_UDPHOST, $"{server.AutoResolveHostname()}:{server.Port}");

                if (socks5.Auth())
                {
                    _tunInterop.Dial(NameList.TYPE_TCPUSER, socks5.Username!);
                    _tunInterop.Dial(NameList.TYPE_TCPPASS, socks5.Password!);

                    _tunInterop.Dial(NameList.TYPE_UDPUSER, socks5.Username!);
                    _tunInterop.Dial(NameList.TYPE_UDPPASS, socks5.Password!);
                }
            }
            else
            {
                _tunInterop.Dial(NameList.TYPE_TCPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");

                _tunInterop.Dial(NameList.TYPE_UDPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");
            }

            #endregion

            #region DNS

            List<string> dns;
            if (Global.Settings.WinTUN.UseCustomDNS)
            {
                dns = Global.Settings.WinTUN.DNS.Any() ? Global.Settings.WinTUN.DNS : Global.Settings.WinTUN.DNS = new List<string> {"1.1.1.1"};
            }
            else
            {
                MainController.PortCheck(53, "DNS");
                DNSController.Start();
                dns = new List<string> {"127.0.0.1"};
            }

            _tunInterop.Dial(NameList.TYPE_DNSADDR, DnsUtils.Join(dns));

            #endregion

            Console.WriteLine("tun2socks init");
            _tunInterop.Init();

            _tunAdapter = new TunAdapter();

            NativeMethods.CreateUnicastIP((int) AddressFamily.InterNetwork, Global.Settings.WinTUN.Address, 24, _tunAdapter.InterfaceIndex);
            SetupRouteTable(mode);
        }

        /// <summary>
        ///     TUN/TAP停止
        /// </summary>
        public void Stop()
        {
            var tasks = new[]
            {
                Task.Run(() =>
                {
                    _tunInterop.Free();
                    // _tunInterop.Unload();
                }),
                Task.Run(ClearRouteTable),
                Task.Run(DNSController.Stop)
            };

            Task.WaitAll(tasks);
        }

        /// <summary>
        ///     设置绕行规则
        /// </summary>
        /// <returns>是否设置成功</returns>
        private void SetupRouteTable(Mode mode)
        {
            Global.MainForm.StatusText(i18N.Translate("SetupBypass"));
            Logging.Info("设置路由规则");

            Logging.Info("绕行 → 服务器 IP");
            if (!IPAddress.IsLoopback(_serverAddresses))
                RouteAction(Action.Create, $"{_serverAddresses}/32", RouteType.Outbound);

            Logging.Info("绕行 → 全局绕过 IP");
            RouteAction(Action.Create, Global.Settings.BypassIPs, RouteType.Outbound);

            #region Rule IPs

            switch (mode.Type)
            {
                case 1:
                    // 代理规则 IP
                    Logging.Info("代理 → 规则 IP");
                    RouteAction(Action.Create, mode.FullRule, RouteType.TUNTAP);

                    if (Global.Settings.WinTUN.ProxyDNS)
                    {
                        Logging.Info("代理 → 自定义 DNS");
                        if (Global.Settings.WinTUN.UseCustomDNS)
                            RouteAction(Action.Create, Global.Settings.WinTUN.DNS.Select(ip => $"{ip}/32"), RouteType.TUNTAP);
                        else
                            RouteAction(Action.Create, $"{Global.Settings.AioDNS.OtherDNS}/32", RouteType.TUNTAP);
                    }

                    break;
                case 2:
                    // 绕过规则 IP

                    Logging.Info("绕行 → 规则 IP");
                    RouteAction(Action.Create, mode.FullRule, RouteType.Outbound);
                    break;
            }

            #endregion

            if (mode.Type == 2)
            {
                Logging.Info("代理 → 全局");
                SetInterface(RouteType.TUNTAP, 0);
                RouteAction(Action.Create, "0.0.0.0/0", RouteType.TUNTAP);
            }
        }

        private void SetInterface(RouteType routeType, int? metric = null)
        {
            var adapter = routeType == RouteType.Outbound ? _outboundAdapter : _tunAdapter;

            var arguments = $"interface ip set interface {adapter.InterfaceIndex} ";
            if (metric != null)
                arguments += $"metric={metric} ";

            Utils.Utils.ProcessRunHiddenAsync("netsh", arguments).Wait();
        }

        /// <summary>
        ///     清除绕行规则
        /// </summary>
        private bool ClearRouteTable()
        {
            var mode = MainController.Mode!;
            RouteAction(Action.Delete, _directIPs, RouteType.Outbound);
            RouteAction(Action.Delete, _proxyIPs, RouteType.TUNTAP);
            _directIPs.Clear();
            _proxyIPs.Clear();
            if (mode.Type == 2)
            {
                SetInterface(RouteType.Outbound);
            }

            return true;
        }

        #region Package

        private void RouteAction(Action action, in IEnumerable<string> ipNetworks, RouteType routeType, int metric = 0)
        {
            foreach (var address in ipNetworks)
                RouteAction(action, address, routeType, metric);
        }

        private bool RouteAction(Action action, in string ipNetwork, RouteType routeType, int metric = 0)
        {
            var s = ipNetwork.Split('/');
            if (s.Length != 2)
            {
                Logging.Warning($"Failed to parse rule {ipNetwork}");
                return false;
            }

            IAdapter adapter;
            List<string> ipList;

            switch (routeType)
            {
                case RouteType.TUNTAP:
                    adapter = _tunAdapter;
                    ipList = _proxyIPs;
                    break;
                case RouteType.Outbound:
                    adapter = _outboundAdapter;
                    ipList = _directIPs;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(routeType), routeType, null);
            }

            string network = s[0];
            var cidr = ushort.Parse(s[1]);
            string gateway = adapter.Gateway.ToString();
            var index = adapter.InterfaceIndex;

            bool result;
            switch (action)
            {
                case Action.Create:
                    result = NativeMethods.CreateRoute((int) AddressFamily.InterNetwork, network, cidr, gateway, index, metric);
                    ipList.Add(ipNetwork);
                    break;
                case Action.Delete:
                    result = NativeMethods.DeleteRoute((int) AddressFamily.InterNetwork, network, cidr, gateway, index, metric);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            Logging.Debug($"{action}Route(\"{network}\", {cidr}, \"{gateway}\", {index}, {metric})");
            if (!result)
                Logging.Warning($"Failed to invoke {action}Route(\"{network}\", {cidr}, \"{gateway}\", {index}, {metric})");

            return result;
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

        #endregion
    }
}
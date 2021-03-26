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
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace Netch.Controllers
{
    public class TUNController : Guard, IModeController
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
        public DNSController DNSController = new();

        protected override IEnumerable<string> StartedKeywords { get; set; } = new[] {"Started"};

        protected override IEnumerable<string> StoppedKeywords { get; set; } = new List<string>();

        public override string MainFile { get; protected set; } = "tun2socks.exe";

        protected override Encoding InstanceOutputEncoding { get; } = Encoding.UTF8;

        public override string Name { get; } = "tun2socks";

        private readonly OutboundAdapter _outboundAdapter = new();
        private IAdapter _tunAdapter = null!;

        public void Start(in Mode mode)
        {
            var server = MainController.Server!;
            _serverAddresses = DnsUtils.Lookup(server.Hostname)!; // server address have been cached when MainController.Start

            var parameter = new WinTun2socksParameter();

            if (server is Socks5 socks5)
            {
                parameter.hostname = $"{server.AutoResolveHostname()}:{server.Port}";
                if (socks5.Auth())
                {
                    parameter.username = socks5.Username!;
                    parameter.password = socks5.Password!;
                }
            }
            else
                parameter.hostname = $"127.0.0.1:{Global.Settings.Socks5LocalPort}";

            MainFile = "tun2socks.exe";
            StartInstanceAuto(parameter.ToString());
            _tunAdapter = new TunAdapter();

            NativeMethods.CreateUnicastIP((int)AddressFamily.InterNetwork, "100.64.0.100", 24, _tunAdapter.InterfaceIndex);
            SetupRouteTable(mode);
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

        public bool TestFakeDNS()
        {
            try
            {
                InitInstance("-h");
                Instance!.Start();
                return Instance.StandardError.ReadToEnd().Contains("-fakeDns");
            }
            catch
            {
                return false;
            }
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

                    result = NativeMethods.CreateRoute((int)AddressFamily.InterNetwork, network, cidr, gateway, index, metric);
#if DEBUG
                    Console.WriteLine($"CreateRoute(\"{network}\", {cidr}, \"{gateway}\", {index}, {metric})");
#endif
                    ipList.Add(ipNetwork);
                    break;
                case Action.Delete:
                    result = NativeMethods.DeleteRoute((int)AddressFamily.InterNetwork, network, cidr, gateway, index, metric);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            if (!result)
                Logging.Warning($"Failed to {action} Route on {routeType} Adapter: {ipNetwork} metric {metric}");

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

        [Verb]
        public class Tap2SocksParameter : ParameterBase
        {
            public string? proxyServer { get; set; }

            public string? tunAddr { get; set; }

            public string? tunMask { get; set; }

            public string? tunGw { get; set; }

            public string? tunDns { get; set; }

            public string? tunName { get; set; }

            public bool fakeDns { get; set; }
        }

        [Verb]
        class WinTun2socksParameter : ParameterBase
        {
            public string? bind { get; set; } = "10.0.0.100";

            [Quote]
            public string? list { get; set; } = "disabled";

            public string? hostname { get; set; }

            [Optional]
            public string? username { get; set; }

            [Optional]
            public string? password { get; set; }

            public string? dns { get; set; } = "1.1.1.1:53";

            public int? mtu { get; set; } = 1500;
        }

        #endregion
    }
}
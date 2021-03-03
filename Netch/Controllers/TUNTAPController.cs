using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;

namespace Netch.Controllers
{
    public class TUNTAPController : Guard, IModeController
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

        protected override IEnumerable<string> StartedKeywords { get; } = new[] {"Running"};

        protected override IEnumerable<string> StoppedKeywords { get; } = new[] {"failed", "invalid vconfig file"};

        public override string MainFile { get; protected set; } = "tun2socks.exe";

        protected override Encoding InstanceOutputEncoding { get; } = Encoding.UTF8;

        public override string Name { get; } = "tun2socks";

        private readonly OutboundAdapter _outbound = new();
        private TapAdapter _tap = null!;

        public void Start(in Mode mode)
        {
            var server = MainController.Server!;
            _serverAddresses = DnsUtils.Lookup(server.Hostname)!; // server address have been cached when MainController.Start

            if (TUNTAP.GetComponentID() == null)
                TUNTAP.AddTap();

            _tap = new TapAdapter();

            List<string> dns;
            if (Global.Settings.TUNTAP.UseCustomDNS)
            {
                dns = Global.Settings.TUNTAP.DNS.Any() ? Global.Settings.TUNTAP.DNS : Global.Settings.TUNTAP.DNS = new List<string> {"1.1.1.1"};
            }
            else
            {
                MainController.PortCheck(53, "DNS");
                DNSController.Start();
                dns = new List<string> {"127.0.0.1"};
            }

            SetupRouteTable(mode);

            Global.MainForm.StatusText(i18N.TranslateFormat("Starting {0}", Name));

            var argument = new StringBuilder();
            if (server is Socks5 socks5 && !socks5.Auth())
                argument.Append($"-proxyServer {server.AutoResolveHostname()}:{server.Port} ");
            else
                argument.Append($"-proxyServer 127.0.0.1:{Global.Settings.Socks5LocalPort} ");

            argument.Append(
                $"-tunAddr {Global.Settings.TUNTAP.Address} -tunMask {Global.Settings.TUNTAP.Netmask} -tunGw {Global.Settings.TUNTAP.Gateway} -tunDns {DnsUtils.Join(dns)} -tunName \"{TUNTAP.GetName(_tap.ComponentID)}\" ");

            if (Global.Settings.TUNTAP.UseFakeDNS && Global.Flags.SupportFakeDns)
                argument.Append("-fakeDns ");

            StartInstanceAuto(argument.ToString(), ProcessPriorityClass.RealTime);
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

            #region Rule IPs

            switch (mode.Type)
            {
                case 1:
                    // 代理规则 IP
                    Logging.Info("代理 → 规则 IP");
                    RouteAction(Action.Create, mode.FullRule, RouteType.TUNTAP);

                    if (Global.Settings.TUNTAP.ProxyDNS)
                    {
                        Logging.Info("代理 → 自定义 DNS");
                        if (Global.Settings.TUNTAP.UseCustomDNS)
                            RouteAction(Action.Create, Global.Settings.TUNTAP.DNS.Select(ip => $"{ip}/32"), RouteType.TUNTAP);
                        else
                            RouteAction(Action.Create, $"{Global.Settings.AioDNS.OtherDNS}/32", RouteType.TUNTAP);
                    }

                    break;
                case 2:
                    // 绕过规则 IP

                    // 将 TUN/TAP 网卡权重放到最高
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"interface ip set interface {_tap.Index} metric=0",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });

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
                Instance!.Start();
                return Instance.StandardError.ReadToEnd().Contains("-fakeDns");
            }
            catch
            {
                return false;
            }
        }

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
                    adapter = _tap;
                    ipList = _proxyIPs;
                    break;
                case RouteType.Outbound:
                    adapter = _outbound;
                    ipList = _directIPs;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(routeType), routeType, null);
            }

            string network = s[0];
            var cidr = ushort.Parse(s[1]);
            string gateway = adapter.Gateway.ToString();
            var index = adapter.Index;

            bool result;
            switch (action)
            {
                case Action.Create:
                    result = NativeMethods.CreateRoute(network, cidr, gateway, index, metric);
                    ipList.Add(ipNetwork);
                    break;
                case Action.Delete:
                    result = NativeMethods.DeleteRoute(network, cidr, gateway, index, metric);
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
    }
}
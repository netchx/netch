using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netch.Interfaces;
using Netch.Interops;
using Netch.Models;
using Netch.Models.Adapter;
using Netch.Servers.Socks5;
using Netch.Utils;
using static Netch.Interops.tun2socks;

namespace Netch.Controllers
{
    public class TUNController : IModeController
    {
        private readonly List<string> _directIPs = new();

        private readonly List<string> _proxyIPs = new();

        public readonly DNSController DNSController = new();

        public string Name { get; } = "tun2socks";

        private readonly OutboundAdapter _outboundAdapter = new();
        private IAdapter _tunAdapter = null!;
        private IPAddress _serverAddresses = null!;

        private const string DummyDns = "6.6.6.6";

        public void Start(in Mode mode)
        {
            var server = MainController.Server!;
            _serverAddresses = DnsUtils.Lookup(server.Hostname)!; // server address have been cached when MainController.Start

            CheckDriver();

            Dial(NameList.TYPE_ADAPMTU, "1500");
            Dial(NameList.TYPE_BYPBIND, _outboundAdapter.Address.ToString());
            Dial(NameList.TYPE_BYPLIST, "disabled");

            #region Server

            Dial(NameList.TYPE_TCPREST, "");
            Dial(NameList.TYPE_TCPTYPE, "Socks5");

            Dial(NameList.TYPE_UDPREST, "");
            Dial(NameList.TYPE_UDPTYPE, "Socks5");

            if (server is Socks5 socks5)
            {
                Dial(NameList.TYPE_TCPHOST, $"{server.AutoResolveHostname()}:{server.Port}");

                Dial(NameList.TYPE_UDPHOST, $"{server.AutoResolveHostname()}:{server.Port}");

                if (socks5.Auth())
                {
                    Dial(NameList.TYPE_TCPUSER, socks5.Username!);
                    Dial(NameList.TYPE_TCPPASS, socks5.Password!);

                    Dial(NameList.TYPE_UDPUSER, socks5.Username!);
                    Dial(NameList.TYPE_UDPPASS, socks5.Password!);
                }
            }
            else
            {
                Dial(NameList.TYPE_TCPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");

                Dial(NameList.TYPE_UDPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");
            }

            #endregion

            #region DNS

            if (Global.Settings.TUNTAP.UseCustomDNS)
            {
                Dial(NameList.TYPE_DNSADDR, Global.Settings.TUNTAP.HijackDNS);
            }
            else
            {
                MainController.PortCheck(Global.Settings.AioDNS.ListenPort, "DNS");
                DNSController.Start();
                Dial(NameList.TYPE_DNSADDR, $"127.0.0.1:{Global.Settings.AioDNS.ListenPort}");
            }

            #endregion

            Global.Logger.Debug("tun2socks init");
            Init();

            _tunAdapter = new TunAdapter();
            switch (mode.Type)
            {
                case 1 when Global.Settings.TUNTAP.ProxyDNS:
                case 2:
                    _tunAdapter.NetworkInterface.SetDns(DummyDns);
                    break;
            }

            RouteHelper.CreateUnicastIP(AddressFamily.InterNetwork,
                Global.Settings.TUNTAP.Address,
                (byte)Utils.Utils.SubnetToCidr(Global.Settings.TUNTAP.Netmask),
                _tunAdapter.InterfaceIndex);

            SetupRouteTable(mode);
        }

        private readonly string BinDriver = Path.Combine(Global.NetchDir, @"bin\wintun.dll");
        private readonly string SysDriver = $@"{Environment.SystemDirectory}\wintun.dll";

        private void CheckDriver()
        {
            var binHash = Utils.Utils.SHA256CheckSum(BinDriver);
            var sysHash = Utils.Utils.SHA256CheckSum(SysDriver);
            Global.Logger.Info("自带 wintun.dll Hash: " + binHash);
            Global.Logger.Info("系统 wintun.dll Hash: " + sysHash);
            if (binHash == sysHash)
                return;

            try
            {
                File.Copy(BinDriver, SysDriver, true);
            }
            catch (Exception e)
            {
                Global.Logger.Error(e.ToString());
                throw new MessageException($"Failed to copy wintun.dll to system directory: {e.Message}");
            }
        }

        /// <summary>
        ///     TUN/TAP停止
        /// </summary>
        public void Stop()
        {
            var tasks = new[]
            {
                Task.Run(Free),
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
            Global.Logger.Info("设置路由规则");

            Global.Logger.Info("绕行 → 服务器 IP");
            if (!IPAddress.IsLoopback(_serverAddresses))
                RouteAction(Action.Create, $"{_serverAddresses}/32", RouteType.Outbound);

            Global.Logger.Info("绕行 → 全局绕过 IP");
            RouteAction(Action.Create, Global.Settings.TUNTAP.BypassIPs, RouteType.Outbound);

            #region Rule IPs

            switch (mode.Type)
            {
                case 1:
                    // 代理规则 IP
                    Global.Logger.Info("代理 → 规则 IP");
                    RouteAction(Action.Create, mode.FullRule, RouteType.TUNTAP);

                    if (Global.Settings.TUNTAP.ProxyDNS)
                    {
                        Global.Logger.Info("代理 → 占位 DNS");
                        RouteAction(Action.Create, $"{DummyDns}/32", RouteType.TUNTAP);

                        if (!Global.Settings.TUNTAP.UseCustomDNS)
                        {
                            Global.Logger.Info("代理 → AioDNS OtherDNS");
                            var otherDns = Global.Settings.AioDNS.OtherDNS;
                            RouteAction(Action.Create, $"{otherDns[..otherDns.IndexOf(':')]}/32", RouteType.TUNTAP);
                        }
                    }

                    break;
                case 2:
                    // 绕过规则 IP

                    Global.Logger.Info("绕行 → 规则 IP");
                    RouteAction(Action.Create, mode.FullRule, RouteType.Outbound);
                    break;
            }

            #endregion

            if (mode.Type == 2)
            {
                Global.Logger.Info("代理 → 全局");
                SetInterface(RouteType.TUNTAP, 0);
                RouteAction(Action.Create, "0.0.0.0/0", RouteType.TUNTAP, record: false);
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

        private void RouteAction(Action action, in IEnumerable<string> ipNetworks, RouteType routeType, int metric = 0, bool record = true)
        {
            foreach (var address in ipNetworks)
                RouteAction(action, address, routeType, metric);
        }

        private bool RouteAction(Action action, in string ipNetwork, RouteType routeType, int metric = 0, bool record = true)
        {
            #region

            if (!TryParseIPNetwork(ipNetwork, out var ip, out var cidr))
                return false;

            IAdapter adapter = routeType switch
            {
                RouteType.Outbound => _outboundAdapter,
                RouteType.TUNTAP => _tunAdapter,
                _ => throw new ArgumentOutOfRangeException(nameof(routeType), routeType, null)
            };

            List<string> ipList = routeType switch
            {
                RouteType.Outbound => _directIPs,
                RouteType.TUNTAP => _proxyIPs,
                _ => throw new ArgumentOutOfRangeException(nameof(routeType), routeType, null)
            };

            string gateway = adapter.Gateway.ToString();
            var index = adapter.InterfaceIndex;

            #endregion

            bool result;
            switch (action)
            {
                case Action.Create:
                    result = RouteHelper.CreateRoute(AddressFamily.InterNetwork, ip, (byte)cidr, gateway, index, metric);
                    if (record)
                        ipList.Add(ipNetwork);

                    break;
                case Action.Delete:
                    result = RouteHelper.DeleteRoute(AddressFamily.InterNetwork, ip, (byte)cidr, gateway, index, metric);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }

            Global.Logger.Debug($"{action}Route(\"{ip}\", {cidr}, \"{gateway}\", {index}, {metric})");
            if (!result)
                Global.Logger.Warning($"Failed to invoke {action}Route(\"{ip}\", {cidr}, \"{gateway}\", {index}, {metric})");

            return result;
        }

        bool TryParseIPNetwork(string ipNetwork, out string ip, out int cidr)
        {
            ip = null!;
            cidr = 0;

            var s = ipNetwork.Split('/');
            if (s.Length != 2)
            {
                Global.Logger.Warning($"Failed to parse rule {ipNetwork}");
                return false;
            }

            ip = s[0];
            cidr = int.Parse(s[1]);
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

        #endregion
    }
}
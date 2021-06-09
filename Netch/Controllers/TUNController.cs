using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Interops;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;
using Serilog;
using static Netch.Interops.tun2socks;

namespace Netch.Controllers
{
    public class TUNController : IModeController
    {
        private const string DummyDns = "6.6.6.6";

        private readonly DNSController _aioDnsController = new();

        private Mode _mode = null!;

        private NetRoute _outbound;

        private IPAddress? _serverRemoteAddress;

        private NetRoute _tun;

        public string Name => "tun2socks";

        public void Start(in Mode mode)
        {
            _mode = mode;
            var server = MainController.Server!;
            _serverRemoteAddress = DnsUtils.Lookup(server.Hostname);

            if (_serverRemoteAddress != null && IPAddress.IsLoopback(_serverRemoteAddress))
                _serverRemoteAddress = null;

            _outbound = NetRoute.GetBestRouteTemplate(out var address);
            CheckDriver();

            Dial(NameList.TYPE_ADAPMTU, "1500");
            Dial(NameList.TYPE_BYPBIND, address.ToString());
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
                _aioDnsController.Start();
                Dial(NameList.TYPE_DNSADDR, $"127.0.0.1:{Global.Settings.AioDNS.ListenPort}");
            }

            #endregion

            if (!Init())
                throw new MessageException("tun2socks start failed.");

            var tunIndex = (int)RouteHelper.ConvertLuidToIndex(tun_luid());
            _tun = NetRoute.TemplateBuilder(IPAddress.Parse(Global.Settings.TUNTAP.Gateway), tunIndex);

            RouteHelper.CreateUnicastIP(AddressFamily.InterNetwork,
                Global.Settings.TUNTAP.Address,
                (byte)Misc.SubnetToCidr(Global.Settings.TUNTAP.Netmask),
                (ulong)tunIndex);

            SetupRouteTable(mode);
        }

        public void Stop()
        {
            var tasks = new[]
            {
                Task.Run(Free),
                Task.Run(ClearRouteTable),
                Task.Run(_aioDnsController.Stop)
            };

            Task.WaitAll(tasks);
        }

        private void CheckDriver()
        {
            string binDriver = Path.Combine(Global.NetchDir, @"bin\wintun.dll");
            string sysDriver = $@"{Environment.SystemDirectory}\wintun.dll";

            var binHash = Misc.Sha256CheckSum(binDriver);
            var sysHash = Misc.Sha256CheckSum(sysDriver);
            Log.Information("自带 wintun.dll Hash: {Hash}", binHash);
            Log.Information("系统 wintun.dll Hash: {Hash}", sysHash);
            if (binHash == sysHash)
                return;

            try
            {
                Log.Information("Copy wintun.dll to System Directory");
                File.Copy(binDriver, sysDriver, true);
            }
            catch (Exception e)
            {
                Log.Error(e, "复制 wintun.dll 异常");
                throw new MessageException($"Failed to copy wintun.dll to system directory: {e.Message}");
            }
        }

        #region Route

        private void SetupRouteTable(Mode mode)
        {
            Global.MainForm.StatusText(i18N.Translate("Setup Route Table Rule"));
            Log.Information("设置路由规则");

            // Server Address
            if (_serverRemoteAddress != null)
                RouteUtils.CreateRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));

            // Global Bypass IPs
            RouteUtils.CreateRouteFill(_outbound, Global.Settings.TUNTAP.BypassIPs);

            var tunNetworkInterface = NetworkInterfaceUtils.Get(_tun.InterfaceIndex);
            switch (mode.Type)
            {
                case ModeType.ProxyRuleIPs:
                    // rules
                    RouteUtils.CreateRouteFill(_tun, mode.GetRules());

                    if (Global.Settings.TUNTAP.ProxyDNS)
                    {
                        tunNetworkInterface.SetDns(DummyDns);
                        // proxy dummy dns
                        RouteUtils.CreateRoute(_tun.FillTemplate(DummyDns, 32));

                        if (!Global.Settings.TUNTAP.UseCustomDNS)
                            // proxy AioDNS other dns
                            RouteUtils.CreateRoute(_tun.FillTemplate(Misc.GetHostFromUri(Global.Settings.AioDNS.OtherDNS), 32));
                    }

                    break;
                case ModeType.BypassRuleIPs:
                    RouteUtils.CreateRouteFill(_outbound, mode.GetRules());

                    tunNetworkInterface.SetDns(DummyDns);

                    if (!Global.Settings.TUNTAP.UseCustomDNS)
                        // bypass AioDNS other dns
                        RouteUtils.CreateRoute(_outbound.FillTemplate(Misc.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), 32));

                    NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex, 0);
                    RouteUtils.CreateRoute(_tun.FillTemplate("0.0.0.0", 0));
                    break;
            }
        }

        private void ClearRouteTable()
        {
            if (_serverRemoteAddress != null)
                RouteUtils.DeleteRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));

            RouteUtils.DeleteRouteFill(_outbound, Global.Settings.TUNTAP.BypassIPs);

            switch (_mode.Type)
            {
                case ModeType.BypassRuleIPs:
                    RouteUtils.DeleteRouteFill(_outbound, _mode.GetRules());
                    NetworkInterfaceUtils.SetInterfaceMetric(_outbound.InterfaceIndex);
                    break;
            }
        }

        #endregion
    }
}
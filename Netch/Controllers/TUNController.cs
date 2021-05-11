using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;
using Netch.Interops;
using static Netch.Interops.tun2socks;

namespace Netch.Controllers
{
    public class TUNController : IModeController
    {
        public string Name => "tun2socks";

        private const string DummyDns = "6.6.6.6";

        private readonly DNSController _aioDnsController = new();

        private NetRoute _outbound;

        private NetRoute _tun;

        private IPAddress _serverAddresses = null!;

        private Mode _mode = null!;

        public void Start(in Mode mode)
        {
            _mode = mode;
            var server = MainController.Server!;
            _serverAddresses = DnsUtils.Lookup(server.Hostname)!; // server address have been cached when MainController.Start

            IPAddress address;
            (_outbound, address) = NetRoute.GetBestRouteTemplate();
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
                throw new MessageException("tun2socks start failed, reboot your system and start again.");

            var tunIndex = (int)RouteHelper.ConvertLuidToIndex(tun_luid());
            _tun = NetRoute.TemplateBuilder(Global.Settings.TUNTAP.Gateway, tunIndex);

            RouteHelper.CreateUnicastIP(AddressFamily.InterNetwork,
                Global.Settings.TUNTAP.Address,
                (byte)Utils.Utils.SubnetToCidr(Global.Settings.TUNTAP.Netmask),
                (ulong)tunIndex);

            SetupRouteTable(mode);
        }

        #region Route

        private void SetupRouteTable(Mode mode)
        {
            Global.MainForm.StatusText(i18N.Translate("Setup Route Table Rule"));
            Global.Logger.Info("设置路由规则");

            // Server Address
            if (!IPAddress.IsLoopback(_serverAddresses))
                RouteUtils.CreateRoute(_outbound.FillTemplate(_serverAddresses.ToString(), 32));

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
                            RouteUtils.CreateRoute(_tun.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.OtherDNS), 32));
                    }

                    break;
                case ModeType.BypassRuleIPs:
                    RouteUtils.CreateRouteFill(_outbound, mode.GetRules());

                    tunNetworkInterface.SetDns(DummyDns);

                    if (!Global.Settings.TUNTAP.UseCustomDNS)
                        // bypass AioDNS other dns
                        RouteUtils.CreateRoute(_tun.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), 32));

                    NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex, 0);
                    RouteUtils.CreateRoute(_tun.FillTemplate("0.0.0.0", 0));
                    break;
            }
        }

        private void ClearRouteTable()
        {
            if (!IPAddress.IsLoopback(_serverAddresses))
                RouteUtils.DeleteRoute(_outbound.FillTemplate(_serverAddresses.ToString(), 32));

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

            var binHash = Utils.Utils.SHA256CheckSum(binDriver);
            var sysHash = Utils.Utils.SHA256CheckSum(sysDriver);
            Global.Logger.Info("自带 wintun.dll Hash: " + binHash);
            Global.Logger.Info("系统 wintun.dll Hash: " + sysHash);
            if (binHash == sysHash)
                return;

            try
            {
                Global.Logger.Info("Copy wintun.dll to System Directory");
                File.Copy(binDriver, sysDriver, true);
            }
            catch (Exception e)
            {
                Global.Logger.Error(e.ToString());
                throw new MessageException($"Failed to copy wintun.dll to system directory: {e.Message}");
            }
        }
    }
}
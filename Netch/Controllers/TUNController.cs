using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Interops;
using Netch.Models;
using Netch.Servers;
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
        private IPAddress? _serverRemoteAddress;
        private TUNConfig _tunConfig = null!;

        private NetRoute _tun;
        private NetRoute _outbound;

        public string Name => "tun2socks";

        public async Task StartAsync(Server server, Mode mode)
        {
            _mode = mode;
            _tunConfig = Global.Settings.TUNTAP;

            if (server is Socks5Bridge socks5Bridge)
                _serverRemoteAddress = await DnsUtils.LookupAsync(socks5Bridge.RemoteHostname);

            if (_serverRemoteAddress != null && IPAddress.IsLoopback(_serverRemoteAddress))
                _serverRemoteAddress = null;

            _outbound = NetRoute.GetBestRouteTemplate();
            CheckDriver();

            Dial(NameList.TYPE_ADAPMTU, "1500");
            Dial(NameList.TYPE_BYPBIND, _outbound.Gateway);
            Dial(NameList.TYPE_BYPLIST, "disabled");

            #region Server

            Dial(NameList.TYPE_TCPREST, "");
            Dial(NameList.TYPE_TCPTYPE, "Socks5");

            Dial(NameList.TYPE_UDPREST, "");
            Dial(NameList.TYPE_UDPTYPE, "Socks5");

            if (server is Socks5 socks5)
            {
                Dial(NameList.TYPE_TCPHOST, $"{await socks5.AutoResolveHostnameAsync()}:{socks5.Port}");

                Dial(NameList.TYPE_UDPHOST, $"{await socks5.AutoResolveHostnameAsync()}:{socks5.Port}");

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
                Trace.Assert(false);
            }

            #endregion

            #region DNS

            if (_tunConfig.UseCustomDNS)
            {
                Dial(NameList.TYPE_DNSADDR, _tunConfig.HijackDNS);
            }
            else
            {
                await _aioDnsController.StartAsync();
                Dial(NameList.TYPE_DNSADDR, $"127.0.0.1:{Global.Settings.AioDNS.ListenPort}");
            }

            #endregion

            if (!Init())
                throw new MessageException("tun2socks start failed.");

            var tunIndex = (int)RouteHelper.ConvertLuidToIndex(tun_luid());
            _tun = NetRoute.TemplateBuilder(_tunConfig.Gateway, tunIndex);

            RouteHelper.CreateUnicastIP(AddressFamily.InterNetwork,
                _tunConfig.Address,
                (byte)Utils.Utils.SubnetToCidr(_tunConfig.Netmask),
                (ulong)tunIndex);

            SetupRouteTable();
        }

        public async Task StopAsync()
        {
            var tasks = new[]
            {
                FreeAsync(),
                Task.Run(ClearRouteTable),
                _aioDnsController.StopAsync()
            };

            await Task.WhenAll(tasks);
        }

        private void CheckDriver()
        {
            string binDriver = Path.Combine(Global.NetchDir, Constants.WintunDllFile);
            string sysDriver = $@"{Environment.SystemDirectory}\wintun.dll";

            var binHash = Utils.Utils.SHA256CheckSum(binDriver);
            var sysHash = Utils.Utils.SHA256CheckSum(sysDriver);
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

        private void SetupRouteTable()
        {
            Global.MainForm.StatusText(i18N.Translate("Setup Route Table Rule"));

            // Server Address
            if (_serverRemoteAddress != null)
                RouteUtils.CreateRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));

            // Global Bypass IPs
            RouteUtils.CreateRouteFill(_outbound, _tunConfig.BypassIPs);

            var tunNetworkInterface = NetworkInterfaceUtils.Get(_tun.InterfaceIndex);
            switch (_mode.Type)
            {
                case ModeType.ProxyRuleIPs:
                    // rules
                    RouteUtils.CreateRouteFill(_tun, _mode.GetRules());

                    if (_tunConfig.ProxyDNS)
                    {
                        tunNetworkInterface.SetDns(DummyDns);
                        // proxy dummy dns
                        RouteUtils.CreateRoute(_tun.FillTemplate(DummyDns, 32));

                        if (!_tunConfig.UseCustomDNS)
                            // proxy AioDNS other dns
                            RouteUtils.CreateRoute(_tun.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.OtherDNS), 32));
                    }

                    break;
                case ModeType.BypassRuleIPs:
                    RouteUtils.CreateRouteFill(_outbound, _mode.GetRules());

                    tunNetworkInterface.SetDns(DummyDns);

                    if (!_tunConfig.UseCustomDNS)
                        // bypass AioDNS other dns
                        RouteUtils.CreateRoute(_outbound.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), 32));

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
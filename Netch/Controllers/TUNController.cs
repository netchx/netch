using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Netch.Interfaces;
using Netch.Interops;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.TunMode;
using Netch.Servers;
using Netch.Utils;
using static Netch.Interops.tun2socks;

namespace Netch.Controllers
{
    public class TUNController : IModeController
    {
        private readonly DNSController _aioDnsController = new();

        private TunMode _mode = null!;
        private IPAddress? _serverRemoteAddress;
        private TUNConfig _tunConfig = null!;

        private NetRoute _tun;
        private NetRoute _outbound;
        private bool _outboundReady;
        private bool _nativeInitialized;
        private bool _dnsInitialized;

        public string Name => "tun2socks";
        public string interfaceName => "netch";

        public ModeFeature Features => ModeFeature.SupportSocks5Auth;

        public async Task StartAsync(Socks5Server server, Mode mode)
        {
            if (mode is not TunMode tunMode)
                throw new InvalidOperationException();

            _mode = tunMode;
            _tunConfig = Global.Settings.TUNTAP;

            if (!IsIPv4(_tunConfig.Address) || !IsIPv4(_tunConfig.Netmask) || !IsIPv4(_tunConfig.Gateway) ||
                (_tunConfig.UseCustomDNS && !IsIPv4(_tunConfig.DNS)))
                throw new MessageException("TUN mode requires IPv4 address, netmask, gateway, and DNS settings.");

            var invalidRoute = _tunConfig.BypassIPs
                .Concat(_mode.Handle)
                .Concat(_mode.Bypass)
                .FirstOrDefault(rule => !RouteUtils.TryParseIPNetwork(rule, out _, out _));
            if (invalidRoute != null)
                throw new MessageException($"TUN mode only supports IPv4 CIDR routes: {invalidRoute}");

            if (server.RemoteHostname.ValueOrDefault() != null)
                _serverRemoteAddress = await DnsUtils.LookupAsync(server.RemoteHostname!, AddressFamily.InterNetwork);
            else
                _serverRemoteAddress = await DnsUtils.LookupAsync(server.Hostname, AddressFamily.InterNetwork);

            if (_serverRemoteAddress == null)
                throw new MessageException("TUN mode requires an IPv4 address for the proxy server bypass route.");

            if (_serverRemoteAddress != null && IPAddress.IsLoopback(_serverRemoteAddress))
                _serverRemoteAddress = null;

            _outbound = NetRoute.GetBestRouteTemplate();
            _outboundReady = true;
            CheckDriver();

            // Wait for adapter to be created
            for (var i = 0; i < 20; i++)
            {
                await Task.Delay(300);
                try
                {
                    _tun.InterfaceIndex = NetworkInterfaceUtils.Get(ni => ni.Name.StartsWith(interfaceName)).GetIndex();
                    break;
                }
                catch
                {
                    // ignored
                }
            }

            Dial(NameList.TYPE_ADAPMTU, "1500");
            Dial(NameList.TYPE_BYPBIND, _outbound.Gateway);
            Dial(NameList.TYPE_BYPLIST, "disabled");

            #region Server

            Dial(NameList.TYPE_TCPREST, "");
            Dial(NameList.TYPE_TCPTYPE, "Socks5");

            Dial(NameList.TYPE_UDPREST, "");
            Dial(NameList.TYPE_UDPTYPE, "Socks5");

            Dial(NameList.TYPE_TCPHOST, $"{await server.AutoResolveHostnameAsync(AddressFamily.InterNetwork)}:{server.Port}");

            Dial(NameList.TYPE_UDPHOST, $"{await server.AutoResolveHostnameAsync(AddressFamily.InterNetwork)}:{server.Port}");

            if (server.Auth())
            {
                Dial(NameList.TYPE_TCPUSER, server.Username!);
                Dial(NameList.TYPE_TCPPASS, server.Password!);

                Dial(NameList.TYPE_UDPUSER, server.Username!);
                Dial(NameList.TYPE_UDPPASS, server.Password!);
            }

            #endregion

            #region DNS

            if (_tunConfig.UseCustomDNS)
            {
                Dial(NameList.TYPE_DNSADDR, DnsUtils.AppendPort(_tunConfig.DNS));
            }
            else
            {
                _dnsInitialized = true;
                await _aioDnsController.StartAsync();
                Dial(NameList.TYPE_DNSADDR, $"127.0.0.1:{Global.Settings.AioDNS.ListenPort}");
            }

            #endregion

            _nativeInitialized = true;
            if (!Init())
                throw new MessageException("tun2socks start failed.");

            var tunIndex = (int)RouteHelper.ConvertLuidToIndex(tun_luid());
            if (tunIndex == 0)
                throw new MessageException("Failed to locate the TUN network interface.");

            _tun = NetRoute.TemplateBuilder(_tunConfig.Gateway, tunIndex);

            if (!RouteHelper.CreateUnicastIP(AddressFamily.InterNetwork,
                _tunConfig.Address,
                (byte)Utils.Utils.SubnetToCidr(_tunConfig.Netmask),
                (ulong)tunIndex))
                throw new MessageException("Failed to configure the TUN IPv4 address.");

            SetupRouteTable();
        }

        public async Task StopAsync()
        {
            // Remove network routes before releasing the adapter they depend on.
            // Native shutdown still runs if route/DNS cleanup fails.
            var dnsStop = _dnsInitialized ? _aioDnsController.StopAsync() : Task.CompletedTask;
            try
            {
                await Task.WhenAll(Task.Run(ClearRouteTable), dnsStop);
            }
            finally
            {
                if (dnsStop.IsCompletedSuccessfully)
                    _dnsInitialized = false;
                if (_nativeInitialized)
                {
                    if (!await FreeAsync())
                        throw new MessageException("tun2socks stop failed.");
                    _nativeInitialized = false;
                }
            }
        }

        private void CheckDriver()
        {
            string binDriver = Path.Combine(Global.NetchDir, Constants.WintunDllFile);
            string sysDriver = $@"{Environment.SystemDirectory}\wintun.dll";

            var binHash = Utils.Utils.Sha256CheckSumAsync(binDriver).Result;
            var sysHash = Utils.Utils.Sha256CheckSumAsync(sysDriver).Result;
            Log.Information("Built-in  wintun.dll Hash: {Hash}", binHash);
            Log.Information("Installed wintun.dll Hash: {Hash}", sysHash);
            if (binHash == sysHash)
                return;

            try
            {
                Log.Information("Copy wintun.dll to System Directory");
                File.Copy(binDriver, sysDriver, true);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy wintun.dll failed");
                throw new MessageException($"Failed to copy wintun.dll to system directory: {e.Message}");
            }
        }

        #region Route

        private void SetupRouteTable()
        {
            Global.MainForm.StatusText(i18N.Translate("Setup Route Table Rule"));

            var tunNetworkInterface = NetworkInterfaceUtils.Get(_tun.InterfaceIndex);
            // Server Address
            if (_serverRemoteAddress != null)
                RouteUtils.CreateRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));

            // Global Bypass IPs
            RouteUtils.CreateRouteFill(_outbound, _tunConfig.BypassIPs);

            // rule
            RouteUtils.CreateRouteFill(_tun, _mode.Handle);
            RouteUtils.CreateRouteFill(_outbound, _mode.Bypass);

            // dns

            if (_tunConfig.UseCustomDNS)
            {
                if (_tunConfig.ProxyDNS)
                {
                    // NOTICE: DNS metric is network interface metric
                    RouteUtils.CreateRoute(_tun.FillTemplate(_tunConfig.DNS, 32));
                }

                tunNetworkInterface.SetDns(_tunConfig.DNS);
            }
            else
            {
                RouteUtils.CreateRoute(_outbound.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), 32));
                RouteUtils.CreateRoute(_tun.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.OtherDNS), 32));
            }

            NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex, 0);
        }

        private void ClearRouteTable()
        {
            if (!_outboundReady)
                return;

            if (_serverRemoteAddress != null)
                RouteUtils.DeleteRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));

            if (_outbound.Gateway != null)
            {
                RouteUtils.DeleteRouteFill(_outbound, Global.Settings.TUNTAP.BypassIPs);
                RouteUtils.DeleteRoute(_outbound.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), 32));
                // Outbound interface metric was never changed on startup.
            }

            if (_mode != null)
                RouteUtils.DeleteRouteFill(_outbound, _mode.Bypass);
        }

        #endregion

        private static bool IsIPv4(string value)
        {
            return IPAddress.TryParse(value, out var address) && address.AddressFamily == AddressFamily.InterNetwork;
        }
    }
}

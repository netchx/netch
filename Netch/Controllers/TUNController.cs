using Netch.Interfaces;
using Netch.Interops;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.TunMode;
using Netch.Servers;
using Netch.Utils;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Netch.Controllers;

public class TUNController : IModeController
{
    private const uint ErrorObjectAlreadyExists = 5010;

    private readonly DNSController _aioDnsController = new();
    private TunMode _mode = new();
    private readonly List<IPAddress> _serverRemoteAddresses = new();
    private TUNConfig _tunConfig = new();

    private NetRoute _tun;
    private NetRoute _tun6;
    private NetRoute _outbound;
    private NetRoute? _outbound6;
    private bool _outboundReady;
    private bool _ipv6Configured;
    private bool _tun2SocksInitialized;
    private bool _dnsInitialized;
    private readonly List<NetRoute> _createdRoutes = new();

    public string Name => "tun2socks";
    public string InterfaceName => "Netch";

    public ModeFeature Features => ModeFeature.SupportIPv6 | ModeFeature.SupportSocks5Auth;

    public async Task StartAsync(SocksServer server, Mode mode)
    {
        if (mode is not TunMode tunMode)
            throw new InvalidOperationException();

        _mode = tunMode;
        _tunConfig = Global.Settings.TUNTAP;
        ValidateConfiguration();

        var remoteHostname = server.RemoteHostname.IsNullOrEmpty() ? server.Address : server.RemoteHostname;
        var remoteIPv4 = await DnsUtils.LookupAsync(remoteHostname, AddressFamily.InterNetwork);
        var remoteIPv6 = await DnsUtils.LookupAsync(remoteHostname, AddressFamily.InterNetworkV6);
        if (remoteIPv4 != null)
            _serverRemoteAddresses.Add(remoteIPv4);
        if (remoteIPv6 != null && !_serverRemoteAddresses.Contains(remoteIPv6))
            _serverRemoteAddresses.Add(remoteIPv6);

        if (_serverRemoteAddresses.Count == 0)
            throw new MessageException("TUN mode requires an IPv4 or IPv6 address for the proxy server bypass route.");

        _serverRemoteAddresses.RemoveAll(IPAddress.IsLoopback);
        Log.Information("Resolved proxy server route exclusions: {Addresses}", string.Join(", ", _serverRemoteAddresses));

        _outbound = NetRoute.GetBestRouteTemplate();
        if (_tunConfig.EnableIPv6 && NeedsIPv6Outbound())
            _outbound6 = NetRoute.GetBestRouteTemplate(AddressFamily.InterNetworkV6);

        await CheckDriverAsync();

        var proxyHost = ProxyUri.Host(await server.AutoResolveHostnameAsync());
        var username = server.Auth() ? server.Username : null;
        var password = server.Auth() ? server.Password : null;

        if (!TUN2Socks.Init(InterfaceName, proxyHost, server.Port, username, password))
            throw new MessageException("tun2socks start failed.");

        _tun2SocksInitialized = true;

        var tunIndex = await WaitForInterfaceIndexAsync();
        _tun = NetRoute.TemplateBuilder(_tunConfig.Gateway, tunIndex);

        if (!RouteHelper.CreateUnicastIP(
                AddressFamily.InterNetwork,
                _tunConfig.Address,
                (byte)Utils.Utils.SubnetToCidr(_tunConfig.Netmask),
                (ulong)tunIndex))
            throw new MessageException("Failed to configure the TUN IPv4 address.");

        if (_tunConfig.EnableIPv6)
        {
            _tun6 = NetRoute.TemplateBuilder(_tunConfig.IPv6Gateway, tunIndex);
            if (!RouteHelper.CreateUnicastIP(
                    AddressFamily.InterNetworkV6,
                    _tunConfig.IPv6Address,
                    _tunConfig.IPv6PrefixLength,
                    (ulong)tunIndex))
                throw new MessageException("Failed to configure the TUN IPv6 address.");

            _ipv6Configured = true;
        }

        await SetupRouteTableAsync();
    }

    public async Task StopAsync()
    {
        try
        {
            ClearRouteTable();
            if (_dnsInitialized)
            {
                await _aioDnsController.StopAsync();
                _dnsInitialized = false;
            }
        }
        finally
        {
            if (_tun2SocksInitialized)
            {
                if (!await TUN2Socks.FreeAsync())
                    throw new MessageException("tun2socks stop failed.");

                _tun2SocksInitialized = false;
            }

            _ipv6Configured = false;
            _serverRemoteAddresses.Clear();
            _outbound6 = null;
        }
    }

    private void ValidateConfiguration()
    {
        if (!IsIPv4(_tunConfig.Address) || !IsIPv4(_tunConfig.Netmask) || !IsIPv4(_tunConfig.Gateway) ||
            (_tunConfig.UseCustomDNS && !IsIPAddress(_tunConfig.DNS)))
            throw new MessageException("TUN mode requires valid IPv4 address, netmask, gateway, and DNS settings.");

        if (_tunConfig.EnableIPv6 &&
            (!IsIPv6(_tunConfig.IPv6Address) || !IsIPv6(_tunConfig.IPv6Gateway) || _tunConfig.IPv6PrefixLength is 0 or > 128))
            throw new MessageException("TUN mode requires valid IPv6 address, gateway, and prefix length settings.");

        var invalidRoute = _tunConfig.BypassIPs
            .Concat(_mode.Handle)
            .Concat(_mode.Bypass)
            .FirstOrDefault(rule => !RouteUtils.TryParseIPNetwork(rule, out _, out _));
        if (invalidRoute != null)
            throw new MessageException($"TUN mode only supports IPv4/IPv6 CIDR routes: {invalidRoute}");
    }

    private async Task<int> WaitForInterfaceIndexAsync()
    {
        for (var i = 0; i < 30; i++)
        {
            await Task.Delay(200);
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(x => x.Name.StartsWith(InterfaceName, StringComparison.OrdinalIgnoreCase));
            if (networkInterface != null)
                return networkInterface.GetIndex();
        }

        Log.Error("虚拟网卡不存在");
        throw new MessageException("tun2socks start failed.");
    }

    private async Task CheckDriverAsync()
    {
        string binDriver = Path.Combine(Global.NetchDir, Constants.WintunDllFile);
        string sysDriver = $@"{Environment.SystemDirectory}\wintun.dll";

        var binHashTask = Utils.Utils.Sha256CheckSumAsync(binDriver);
        var sysHashTask = Utils.Utils.Sha256CheckSumAsync(sysDriver);
        var binHash = await binHashTask;
        var sysHash = await sysHashTask;
        Log.Information("Built-in wintun.dll Hash: {Hash}", binHash);
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

    private async Task SetupRouteTableAsync()
    {
        Global.MainForm.StatusText(i18N.Translate("Setup Route Table Rule"));
        var tunNetworkInterface = NetworkInterfaceUtils.Get(_tun.InterfaceIndex);

        foreach (var serverRemoteAddress in _serverRemoteAddresses)
        {
            var template = GetTemplate(_outbound, _outbound6, serverRemoteAddress);
            EnsureTrackedRoute(template.FillTemplate(
                serverRemoteAddress.ToString(),
                serverRemoteAddress.AddressFamily == AddressFamily.InterNetwork ? (byte)32 : (byte)128));
        }

        EnsureTrackedRoutes(_outbound, _outbound6, _tunConfig.BypassIPs);
        EnsureTrackedRoutes(_tun, _ipv6Configured ? _tun6 : null, _mode.Handle);
        EnsureTrackedRoutes(_outbound, _outbound6, _mode.Bypass);

        if (_tunConfig.UseCustomDNS)
        {
            if (_tunConfig.ProxyDNS)
            {
                var dnsAddress = IPAddress.Parse(_tunConfig.DNS);
                var dnsTemplate = GetTemplate(_tun, _ipv6Configured ? _tun6 : null, dnsAddress);
                EnsureTrackedRoute(dnsTemplate.FillTemplate(
                    _tunConfig.DNS,
                    dnsAddress.AddressFamily == AddressFamily.InterNetwork ? (byte)32 : (byte)128));
            }

            tunNetworkInterface.SetDns(_tunConfig.DNS);
        }
        else
        {
            EnsureHostRoute(_outbound, _outbound6, Global.Settings.AioDNS.ChinaDNS);
            EnsureHostRoute(_tun, _ipv6Configured ? _tun6 : null, Global.Settings.AioDNS.OtherDNS);
            tunNetworkInterface.SetDns("127.0.0.1");
            _dnsInitialized = true;
            await _aioDnsController.StartAsync();
            Log.Information("TUN DNS is routed to local AioDNS at 127.0.0.1:{Port}", Global.Settings.AioDNS.ListenPort);
        }

        NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex, 0);
        if (_ipv6Configured)
            NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex, 0, AddressFamily.InterNetworkV6);

        _outboundReady = true;
    }

    private void ClearRouteTable()
    {
        if (!_outboundReady)
            return;

        for (var i = _createdRoutes.Count - 1; i >= 0; i--)
        {
            var route = _createdRoutes[i];
            if (!RouteUtils.DeleteRoute(route))
                Log.Warning("Failed to delete route {Network}/{Cidr} via {Gateway}", route.Network, route.Cidr, route.Gateway);
        }

        _createdRoutes.Clear();
        NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex);
        if (_ipv6Configured)
            NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex, null, AddressFamily.InterNetworkV6);

        _outboundReady = false;
    }

    private void EnsureTrackedRoutes(NetRoute ipv4Template, NetRoute? ipv6Template, IEnumerable<string> rules)
    {
        foreach (var rule in rules)
        {
            if (!RouteUtils.TryParseIPNetwork(rule, out var network, out var cidr))
                throw new MessageException($"Invalid TUN route: {rule}");

            var address = IPAddress.Parse(network);
            var template = GetTemplate(ipv4Template, ipv6Template, address);
            EnsureTrackedRoute(template.FillTemplate(network, (byte)cidr));
        }
    }

    private void EnsureHostRoute(NetRoute ipv4Template, NetRoute? ipv6Template, string endpoint)
    {
        var host = Utils.Utils.GetHostFromUri(endpoint);
        if (!IPAddress.TryParse(host, out var address))
            throw new MessageException($"DNS endpoint must be an IP address for TUN routing: {endpoint}");

        var template = GetTemplate(ipv4Template, ipv6Template, address);
        EnsureTrackedRoute(template.FillTemplate(
            address.ToString(),
            address.AddressFamily == AddressFamily.InterNetwork ? (byte)32 : (byte)128));
    }

    private static NetRoute GetTemplate(NetRoute ipv4Template, NetRoute? ipv6Template, IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return ipv4Template;

        if (address.AddressFamily == AddressFamily.InterNetworkV6 && ipv6Template.HasValue)
            return ipv6Template.Value;

        throw new MessageException($"No IPv6 route template is available for {address}.");
    }

    private void EnsureTrackedRoute(NetRoute route)
    {
        if (!IPAddress.TryParse(route.Network, out var address))
            throw new MessageException($"Invalid TUN route address: {route.Network}");

        var status = RouteHelper.CreateRouteStatus(
            address.AddressFamily,
            route.Network,
            route.Cidr,
            route.Gateway,
            (ulong)route.InterfaceIndex,
            route.Metric);

        if (status == 0)
        {
            _createdRoutes.Add(route);
            return;
        }

        if (status == ErrorObjectAlreadyExists)
        {
            Log.Information("CreateRoute already exists, reusing: {Address}/{Cidr} via {Gateway} interface {Interface} metric {Metric}",
                route.Network, route.Cidr, route.Gateway, route.InterfaceIndex, route.Metric);
            return;
        }

        var message = new Win32Exception((int)status).Message;
        Log.Warning("CreateRoute failed: {Address}/{Cidr} via {Gateway} interface {Interface} metric {Metric}, Windows status {Status} ({StatusName})",
            route.Network, route.Cidr, route.Gateway, route.InterfaceIndex, route.Metric, status, message);
        throw new MessageException($"Failed to create TUN route {route.Network}/{route.Cidr} via {route.Gateway}. Windows error {status}: {message}");
    }

    private bool NeedsIPv6Outbound()
    {
        if (_serverRemoteAddresses.Any(x => x.AddressFamily == AddressFamily.InterNetworkV6))
            return true;

        if (_tunConfig.BypassIPs.Concat(_mode.Bypass).Any(IsIPv6Route))
            return true;

        return IPAddress.TryParse(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), out var dns) &&
               dns.AddressFamily == AddressFamily.InterNetworkV6;
    }

    private static bool IsIPv4(string value)
    {
        return IPAddress.TryParse(value, out var address) && address.AddressFamily == AddressFamily.InterNetwork;
    }

    private static bool IsIPv6(string value)
    {
        return IPAddress.TryParse(value, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    private static bool IsIPAddress(string value)
    {
        return IPAddress.TryParse(value, out _);
    }

    private static bool IsIPv6Route(string value)
    {
        return RouteUtils.TryParseIPNetwork(value, out var network, out _) &&
               IPAddress.Parse(network).AddressFamily == AddressFamily.InterNetworkV6;
    }

    #endregion
}

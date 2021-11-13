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

namespace Netch.Controllers;

public class TUNController : Guard, IModeController
{
    private readonly DNSController _aioDnsController = new();

    private TunMode _mode = null!;
    private IPAddress? _serverRemoteAddress;
    private TUNConfig _tunConfig = null!;
    private bool _routeSetuped = false;

    private NetRoute _tun;
    private NetworkInterface _tunNetworkInterface = null!;
    private NetRoute _outbound;

    public override string Name => "tun2socks";

    public ModeFeature Features => ModeFeature.SupportSocks5Auth;

    protected override IEnumerable<string> StartedKeywords { get; } = new[] { "Creating adapter" };

    protected override IEnumerable<string> FailedKeywords { get; } = new[] { "panic" };

    public TUNController() : base("tun2socks.exe")
    {
    }

    public async Task StartAsync(Socks5Server server, Mode mode)
    {
        if (mode is not TunMode tunMode)
            throw new InvalidOperationException();

        _mode = tunMode;
        _tunConfig = Global.Settings.TUNTAP;

        if (server.RemoteHostname.ValueOrDefault() != null)
            _serverRemoteAddress = await DnsUtils.LookupAsync(server.RemoteHostname!);
        else
            _serverRemoteAddress = await DnsUtils.LookupAsync(server.Hostname);

        if (_serverRemoteAddress != null && IPAddress.IsLoopback(_serverRemoteAddress))
            _serverRemoteAddress = null;

        _outbound = NetRoute.GetBestRouteTemplate();
        CheckDriver();

        var proxy = server.Auth()
            ? $"socks5://{server.Username}:{server.Password}@{await server.AutoResolveHostnameAsync()}:{server.Port}"
            : $"socks5://{await server.AutoResolveHostnameAsync()}:{server.Port}";

        const string interfaceName = "netch";
        var arguments = new object?[]
        {
            // -device tun://aioCloud -proxy socks5://127.0.0.1:7890
            "-device", $"tun://{interfaceName}",
            "-proxy", proxy,
            "-mtu", "1500"
        };

        await StartGuardAsync(Arguments.Format(arguments));

        // Wait for adapter to be created
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(300);
            try
            {
                _tunNetworkInterface = NetworkInterfaceUtils.Get(ni => ni.Name.StartsWith(interfaceName));
                break;
            }
            catch
            {
                // ignored
            }
        }

        if (_tunNetworkInterface == null)
            throw new MessageException("Create wintun adapter failed");

        var tunIndex = _tunNetworkInterface.GetIndex();
        _tun = NetRoute.TemplateBuilder(_tunConfig.Gateway, tunIndex);

        Global.MainForm.StatusText(i18N.Translate("Assigning unicast IP"));
        if (!await Task.Run(() => RouteHelper.CreateUnicastIP(AddressFamily.InterNetwork,
                _tunConfig.Address,
                (byte)Utils.Utils.SubnetToCidr(_tunConfig.Netmask),
                (ulong)tunIndex)))
        {
            Log.Error("Create unicast IP failed");
            throw new MessageException("Create unicast IP failed");
        }

        await SetupRouteTableAsync();
    }

    public override async Task StopAsync()
    {
        var tasks = new[]
        {
            StopGuardAsync(),
            Task.Run(ClearRouteTable),
            _aioDnsController.StopAsync()
        };

        await Task.WhenAll(tasks);
    }

    private void CheckDriver()
    {
        var f = $@"{Environment.SystemDirectory}\wintun.dll";
        try
        {
            if (File.Exists(f))
            {
                Log.Information($"Remove unused \"{f}\"");
                File.Delete(f);
            }
        }
        catch
        {
            // ignored
        }
    }

    #region Route

    private async Task SetupRouteTableAsync()
    {
        // _outbound: not go through proxy
        // _tun: tun -> socks5
        // aiodns: a simple dns server with dns routing


        _routeSetuped = true;
        Global.MainForm.StatusText(i18N.Translate("Setup Route Table Rule"));

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
                RouteUtils.CreateRoute(_tun.FillTemplate(_tunConfig.DNS, 32));

            _tunNetworkInterface.SetDns(_tunConfig.DNS);
        }
        else
        {
            // aiodns
            RouteUtils.CreateRoute(_outbound.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), 32));
            RouteUtils.CreateRoute(_tun.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.OtherDNS), 32));
            // aiodns listen on tun interface
            await _aioDnsController.StartAsync(Global.Settings.TUNTAP.Address);

            _tunNetworkInterface.SetDns(_tunConfig.Address);
        }

        // set tun interface's metric to the highest to let Windows use the interface's DNS
        NetworkInterfaceUtils.SetInterfaceMetric(_tun.InterfaceIndex, 0);
    }

    private void ClearRouteTable()
    {
        if (!_routeSetuped)
            return;

        if (_serverRemoteAddress != null)
            RouteUtils.DeleteRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));

        RouteUtils.DeleteRouteFill(_outbound, Global.Settings.TUNTAP.BypassIPs);

        RouteUtils.DeleteRouteFill(_outbound, _mode.Bypass);

        RouteUtils.DeleteRoute(_outbound.FillTemplate(Utils.Utils.GetHostFromUri(Global.Settings.AioDNS.ChinaDNS), 32));

        NetworkInterfaceUtils.SetInterfaceMetric(_outbound.InterfaceIndex);
    }

    #endregion
}
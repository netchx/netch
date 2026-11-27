using System.Net;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Netch.Controllers;

public class DNSController : IController
{
    private AioDnsServer? _server;

    public string Name => "DNS Service";

    public Task StartAsync() => StartAsync(null, null, null);

    public async Task StartAsync(
        IReadOnlyList<IPAddress>? chinaBootstrapIps,
        IReadOnlyList<IPAddress>? otherBootstrapIps,
        SocksServer? otherSocks5Proxy)
    {
        await StopAsync();

        var aioDnsConfig = Global.Settings.AioDNS;
        if (!DnsEndpoint.TryParse(aioDnsConfig.ChinaDNS, out var chinaEndpoint))
            throw new MessageException($"Invalid AioDNS domestic upstream: {aioDnsConfig.ChinaDNS}");

        if (!DnsEndpoint.TryParse(aioDnsConfig.OtherDNS, out var otherEndpoint))
            throw new MessageException($"Invalid AioDNS international upstream: {aioDnsConfig.OtherDNS}");

        var rulesPath = Path.Combine(Global.NetchDir, Constants.AioDnsRuleFile);
        var logPath = Path.Combine(Global.NetchDir, "logging", "AioDNS.log");

        var server = new AioDnsServer(new AioDnsOptions
        {
            ListenAddress = IPAddress.Loopback,
            ListenPort = aioDnsConfig.ListenPort,
            RuleFilePath = rulesPath,
            LogFilePath = logPath,
            ChinaEndpoint = chinaEndpoint,
            OtherEndpoint = otherEndpoint,
            ChinaBootstrapIps = chinaBootstrapIps,
            OtherBootstrapIps = otherBootstrapIps,
            OtherSocks5Proxy = otherSocks5Proxy
        });

        Log.Information("Starting AioDNS: ChinaDNS={ChinaDNS}, OtherDNS={OtherDNS}, Rules={Rules}, LogPath={LogPath}",
            chinaEndpoint.Normalized, otherEndpoint.Normalized, rulesPath, logPath);

        try
        {
            await server.StartAsync();
            _server = server;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AioDNS start failed");
            await server.DisposeAsync();
            throw new MessageException($"AioDNS start failed: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        var server = Interlocked.Exchange(ref _server, null);
        if (server == null)
            return;

        Log.Information("Stopping AioDNS");
        await server.StopAsync();
    }
}

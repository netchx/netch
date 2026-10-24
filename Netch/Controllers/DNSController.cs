using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;
using static Netch.Interops.AioDNS;

namespace Netch.Controllers;

public class DNSController : IController
{
    public string Name => "DNS Service";

    public async Task StartAsync()
    {
        var aioDnsConfig = Global.Settings.AioDNS;

        Dial(NameList.TYPE_REST, "");
        var logPath = Path.Combine(Global.NetchDir, "logging", "AioDNS.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        if (!Dial(NameList.TYPE_LOG, logPath))
            Log.Warning("Failed to configure AioDNS log file {LogPath}", logPath);

        Dial(NameList.TYPE_LIST, Path.GetFullPath(Constants.AioDnsRuleFile));
        // TODO remove ListenPort setting
        Dial(NameList.TYPE_LISN, $"127.0.0.1:{aioDnsConfig.ListenPort}");
        Dial(NameList.TYPE_CDNS, DnsUtils.AppendPort(aioDnsConfig.ChinaDNS));
        Dial(NameList.TYPE_ODNS, DnsUtils.AppendPort(aioDnsConfig.OtherDNS));

        Log.Information("Starting AioDNS: ChinaDNS={ChinaDNS}, OtherDNS={OtherDNS}, Rules={Rules}, LogPath={LogPath}",
            aioDnsConfig.ChinaDNS, aioDnsConfig.OtherDNS, Path.GetFullPath(Constants.AioDnsRuleFile), logPath);

        if (!await InitAsync())
            throw new MessageException("AioDNS start failed.");
    }

    public Task StopAsync()
    {
        Log.Information("Stopping AioDNS");
        return FreeAsync();
    }
}

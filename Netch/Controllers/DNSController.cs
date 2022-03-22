using Netch.Interfaces;
using Netch.Models;
using static Netch.Interops.AioDNS;

namespace Netch.Controllers;

public class DNSController : IController
{
    public string Name => "DNS Service";

    public async Task StartAsync()
    {
        var aioDnsConfig = Global.Settings.AioDNS;

        Dial(NameList.TYPE_REST, "");
        Dial(NameList.TYPE_LIST, Path.GetFullPath(Constants.AioDnsRuleFile));
        // TODO remove ListenPort setting
        Dial(NameList.TYPE_LISN, $"127.0.0.1:{aioDnsConfig.ListenPort}");
        Dial(NameList.TYPE_CDNS, $"{aioDnsConfig.ChinaDNS}");
        Dial(NameList.TYPE_ODNS, $"{aioDnsConfig.OtherDNS}");

        if (!await InitAsync())
            throw new MessageException("AioDNS start failed.");
    }

    public Task StopAsync()
    {
        return FreeAsync();
    }
}
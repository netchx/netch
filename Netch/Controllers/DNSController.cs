using System;
using System.IO;
using System.Threading.Tasks;
using Netch.Interfaces;
using static Netch.Interops.AioDNS;

namespace Netch.Controllers
{
    public class DNSController : IController
    {
        public string Name => "DNS Service";

        public async Task StopAsync()
        {
            await FreeAsync();
        }

        public async Task StartAsync()
        {
            MainController.PortCheck(Global.Settings.AioDNS.ListenPort, "DNS");

            var aioDnsConfig = Global.Settings.AioDNS;
            var listenAddress = Global.Settings.LocalAddress;

            Dial(NameList.TYPE_REST, "");
            Dial(NameList.TYPE_ADDR, $"{listenAddress}:{aioDnsConfig.ListenPort}");
            Dial(NameList.TYPE_LIST, Path.GetFullPath(Constants.AioDnsRuleFile));
            Dial(NameList.TYPE_CDNS, $"{aioDnsConfig.ChinaDNS}");
            Dial(NameList.TYPE_ODNS, $"{aioDnsConfig.OtherDNS}");

            if (!await InitAsync())
                throw new Exception("AioDNS start failed.");
        }
    }
}
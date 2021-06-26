using System;
using System.IO;
using Netch.Interfaces;
using static Netch.Interops.AioDNS;

namespace Netch.Controllers
{
    public class DNSController : IController
    {
        public string Name => "DNS Service";

        public void Stop()
        {
            Free();
        }

        public void Start()
        {
            MainController.PortCheck(Global.Settings.AioDNS.ListenPort, "DNS");

            var aioDnsConfig = Global.Settings.AioDNS;
            var listenAddress = Global.Settings.LocalAddress;

            Dial(NameList.TYPE_REST, "");
            Dial(NameList.TYPE_ADDR, $"{listenAddress}:{aioDnsConfig.ListenPort}");
            Dial(NameList.TYPE_LIST, Path.GetFullPath(Constants.AioDnsRuleFile));
            Dial(NameList.TYPE_CDNS, $"{aioDnsConfig.ChinaDNS}");
            Dial(NameList.TYPE_ODNS, $"{aioDnsConfig.OtherDNS}");

            if (!Init())
                throw new Exception("AioDNS start failed.");
        }
    }
}
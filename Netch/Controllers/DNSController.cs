using System;
using System.IO;
using Netch.Interfaces;
using static Netch.Interops.AioDNS;

namespace Netch.Controllers
{
    public class DNSController : IController
    {
        public string Name { get; } = "DNS Service";

        public void Stop()
        {
            Free();
        }

        /// <summary>
        ///     启动DNS服务
        /// </summary>
        /// <returns></returns>
        public void Start()
        {
            Dial(NameList.TYPE_REST, "");
            Dial(NameList.TYPE_ADDR, $"{Global.Settings.LocalAddress}:{Global.Settings.AioDNS.ListenPort}");
            Dial(NameList.TYPE_LIST, Path.GetFullPath(Global.Settings.AioDNS.RulePath));
            Dial(NameList.TYPE_CDNS, $"{Global.Settings.AioDNS.ChinaDNS}");
            Dial(NameList.TYPE_ODNS, $"{Global.Settings.AioDNS.OtherDNS}");

            if (!Init())
                throw new Exception("AioDNS start failed");
        }
    }
}
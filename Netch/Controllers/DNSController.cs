using System;
using Netch.Utils;

namespace Netch.Controllers
{
    public class DNSController : Controller
    {
        public DNSController()
        {
            Name = "DNS Service";
            MainFile = "unbound.exe";
            RedirectStd = false;
        }

        /// <summary>
        ///     启动DNS服务
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            return StartInstanceAuto("-c unbound-service.conf -v");
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
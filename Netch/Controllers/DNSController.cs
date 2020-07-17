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
        }

        /// <summary>
        ///     启动DNS服务
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            Instance = GetProcess();
            Instance.StartInfo.Arguments = "-c unbound-service.conf -v";

            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            try
            {
                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                return true;
            }
            catch (Exception e)
            {
                Logging.Error("dns-unbound 进程出错：\n" + e);
                return false;
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
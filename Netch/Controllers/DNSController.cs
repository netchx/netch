using System;
using System.Diagnostics;
using Netch.Utils;

namespace Netch.Controllers
{
    public class DNSController : Controller
    {
        public DNSController()
        {
            Name = "dns Service";
            MainFile = "unbound";
            ExtFiles = new[] {"unbound-service.conf", "forward-zone.conf"};
            InitCheck();
        }

        /// <summary>
        ///     启动DNS服务
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (!Ready) return false;

            Instance = GetProcess("bin\\unbound.exe");
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

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Write(e.Data);
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
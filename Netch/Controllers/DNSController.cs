using System;
using System.Diagnostics;
using Netch.Forms;
using Netch.Utils;

namespace Netch.Controllers
{
    public class DNSController : Controller
    {
        public DNSController()
        {
            MainName = "unbound";
            ExtFiles = new[] {"unbound-service.conf", "forward-zone.conf"};
            ready = BeforeStartProgress();
        }

        /// <summary>
        ///     启动DNS服务
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            MainForm.Instance.StatusText(i18N.Translate("Starting dns Service"));
            try
            {
                Instance = MainController.GetProcess("bin\\unbound.exe");

                Instance.StartInfo.Arguments = "-c unbound-service.conf -v";

                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;

                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                Logging.Info("dns-unbound 启动完毕");
                return true;
            }
            catch (Exception)
            {
                Logging.Info("dns-unbound 进程出错");
                Stop();
                return false;
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            WriteLog(e);
        }
    }
}
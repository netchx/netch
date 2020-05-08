using Netch.Forms;
using Netch.Utils;
using System;
using System.Diagnostics;
using System.IO;

namespace Netch.Controllers
{
    public class DNSController
    {
        /// <summary>
        ///		进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        /// 启动DNS服务
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Starting dns Service")}");
            try
            {
                if (!File.Exists("bin\\unbound.exe") && !File.Exists("bin\\unbound-service.conf") && !File.Exists("bin\\forward-zone.conf"))
                {
                    return false;
                }

                Instance = MainController.GetProcess();
                Instance.StartInfo.FileName = "bin\\unbound.exe";

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
                Utils.Logging.Info("dns-unbound 进程出错");
                Stop();
                return false;
            }
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            try
            {
                if (Instance != null && !Instance.HasExited)
                {
                    Instance.Kill();
                    Instance.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                if (File.Exists("logging\\dns-unbound.log"))
                {
                    File.Delete("logging\\dns-unbound.log");
                }
                File.AppendAllText("logging\\dns-unbound.log", $"{e.Data}\r\n");
            }
        }
    }
}

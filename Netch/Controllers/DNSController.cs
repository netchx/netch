using Netch.Forms;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Netch.Controllers
{
    public class DNSController
    {
        /// <summary>
        ///		进程实例
        /// </summary>
        public Process Instance;

        /// <summary>
        /// 启动NatTypeTester
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Starting dns2tcp Service")}");
            try
            {
                if (!File.Exists("bin\\dns2tcp.exe"))
                {
                    return false;
                }

                Instance = MainController.GetProcess();
                Instance.StartInfo.FileName = "bin\\dns2tcp.exe";

                Instance.StartInfo.Arguments = " -L 127.0.0.1:53 -R 1.1.1.1:53";

                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;

                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                return true;
            }
            catch (Exception)
            {
                Utils.Logging.Info("dns2tcp 进程出错");
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
                if (File.Exists("logging\\dns2tcp.log"))
                {
                    File.Delete("logging\\dns2tcp.log");
                }
                File.AppendAllText("logging\\dns2tcp.log", $"{e.Data}\r\n");
            }
        }

       /* public static DNS.Server.DnsServer Server = new DNS.Server.DnsServer(new Resolver());

        public bool Start()
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting LocalDns service")}");
            try
            {
                _ = Server.Listen(new IPEndPoint(IPAddress.IPv6Any, 53));
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
                return false;
            }

            return true;
        }

        public void Stop()
        {
            try
            {
                Server.Dispose();
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }*/
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class NTTController : Controller
    {
        public NTTController()
        {
            MainName = "NTT";
            ready = BeforeStartProgress();
        }

        /// <summary>
        ///     启动NatTypeTester
        /// </summary>
        /// <returns></returns>
        public (bool, string, string, string) Start()
        {
            Thread.Sleep(1000);
            MainForm.Instance.NatTypeStatusText(i18N.Translate("Starting NatTester"));
            try
            {
                Instance = MainController.GetProcess("bin\\NTT.exe");

                Instance.StartInfo.Arguments = $" {Global.Settings.STUN_Server} {Global.Settings.STUN_Server_Port}";

                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;

                State = State.Starting;
                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                Instance.WaitForExit();

                var result = File.ReadAllText($"logging\\{MainName}.log").Split('#');
                var natType = result[0];
                var localEnd = result[1];
                var publicEnd = result[2];
                MainForm.Instance.NatTypeStatusText(natType);

                return (true, natType, localEnd, publicEnd);
            }
            catch (Exception)
            {
                Logging.Info("NTT 进程出错");
                Stop();
                return (false, null, null, null);
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            WriteLog(e);
        }
    }
}
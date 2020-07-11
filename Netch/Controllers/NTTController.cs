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
            MainFile = "NTT";
            InitCheck();
        }

        /// <summary>
        ///     启动 NatTypeTester
        /// </summary>
        /// <returns></returns>
        public (bool, string, string, string) Start()
        {
            if (!Ready) return (false, null, null, null);
            Thread.Sleep(1000);
            MainForm.Instance.NatTypeStatusText(i18N.Translate("Starting NatTester"));
            try
            {
                Instance = GetProcess("bin\\NTT.exe");

                Instance.StartInfo.Arguments = $" {Global.Settings.STUN_Server} {Global.Settings.STUN_Server_Port}";

                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;

                State = State.Starting;
                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                Instance.WaitForExit();

                var result = File.ReadAllText($"logging\\{MainFile}.log").Split('#');
                var natType = result[0];
                var localEnd = result[1];
                var publicEnd = result[2];
                MainForm.Instance.NatTypeStatusText(natType);

                return (true, natType, localEnd, publicEnd);
            }
            catch (Exception)
            {
                Logging.Error("NTT 进程出错");
                Stop();
                return (false, null, null, null);
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            WriteLog(e);
        }

        /// <summary>
        ///     无用
        /// </summary>
        public override void Stop()
        {
        }
    }
}
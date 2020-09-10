using System;
using System.Diagnostics;
using System.Linq;
using Netch.Utils;

namespace Netch.Controllers
{
    public class NTTController : Controller
    {
        private string _localEnd;
        private string _publicEnd;
        private string _result;
        private string _bindingTest;

        public NTTController()
        {
            Name = "NTT";
            MainFile = "NTT.exe";
        }

        /// <summary>
        ///     启动 NatTypeTester
        /// </summary>
        /// <returns></returns>
        public (string, string, string) Start()
        {
            _result = _localEnd = _publicEnd = null;

            try
            {
                InitInstance($" {Global.Settings.STUN_Server} {Global.Settings.STUN_Server_Port}");
                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;
                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                Instance.WaitForExit();
                if (_bindingTest == "Fail")
                    _result = "UdpBlocked";
                return (_result, _localEnd, _publicEnd);
            }
            catch (Exception e)
            {
                Logging.Error($"{Name} 控制器出错:\n" + e);
                try
                {
                    Stop();
                }
                catch
                {
                    // ignored
                }

                return (null, null, null);
            }
        }

        private new void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            Logging.Info($"[NTT] {e.Data}");

            var str = e.Data.Split(':').Select(s => s.Trim()).ToArray();
            if (str.Length < 2)
                return;
            var key = str[0];
            var value = str[1];
            switch (key)
            {
                case "Other address is":
                case "Nat mapping behavior":
                case "Nat filtering behavior":
                    break;
                case "Binding test":
                    _bindingTest = value;
                    break;
                case "Local address":
                    _localEnd = value;
                    break;
                case "Mapped address":
                    _publicEnd = value;
                    break;
                case "result":
                    _result = value;
                    break;
                default:
                    _result = str.Last();
                    break;
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
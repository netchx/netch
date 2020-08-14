using System;
using System.ComponentModel;
using System.Diagnostics;
using Netch.Utils;

namespace Netch.Controllers
{
    public class NTTController : Controller
    {
        private string _Other_address;
        private string _Binding_test;
        private string _Local_address;
        private string _Mapped_address;
        private string _Nat_mapping_behavior;
        private string _Nat_filtering_behavior;
        private string _lastResult;

        public NTTController()
        {
            Name = "NTT";
            MainFile = "NTT.exe";
        }

        /// <summary>
        ///     启动 NatTypeTester
        /// </summary>
        /// <returns></returns>
        public (bool, string, string, string) Start()
        {
            try
            {
                InitInstance($" {Global.Settings.STUN_Server} {Global.Settings.STUN_Server_Port}");
                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;
                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();
                Instance.WaitForExit();

                /* var result = _lastResult.Split('\n');
                 var natType = result[0];
                 var localEnd = result[1];
                 var publicEnd = result[2];*/

                var natType = _lastResult;
                var localEnd = _Local_address;
                var publicEnd = _Mapped_address;

                return (true, natType, localEnd, publicEnd);
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

                return (false, null, null, null);
            }
        }

        private new void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            var str = e.Data.Split(':');
            if (str.Length < 2)
                return;
            var key = str[0];
            var value = str[1].Trim();
            switch (key)
            {
                case "Other address is":
                    _Other_address = value;
                    Logging.Info($"[NTT] Other address is {value}");
                    break;
                case "Binding test":
                    _Binding_test = value;
                    Logging.Info($"[NTT] Binding test {value}");
                    break;
                case "Local address":
                    _Local_address = value;
                    Logging.Info($"[NTT] Local address {value}");
                    break;
                case "Mapped address":
                    _Mapped_address = value;
                    Logging.Info($"[NTT] Mapped address {value}");
                    break;
                case "Nat mapping behavior":
                    _Nat_mapping_behavior = value;
                    Logging.Info($"[NTT] Nat mapping behavior {value}");
                    break;
                case "Nat filtering behavior":
                    _Nat_filtering_behavior = value;
                    Logging.Info($"[NTT] Nat filtering behavior {value}");
                    break;
                case "result":
                    _lastResult = value;
                    Logging.Info($"[NTT] result {value}");
                    break;
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
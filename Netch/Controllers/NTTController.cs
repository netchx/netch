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
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("Other address is: ")) 
                    _Other_address = e.Data.Split(':')[1].Trim();
                if (e.Data.StartsWith("Binding test: "))
                    _Binding_test = e.Data.Split(':')[1].Trim();
                if (e.Data.StartsWith("Local address: "))
                    _Local_address = e.Data.Split(':')[1].Trim();
                if (e.Data.StartsWith("Mapped address: "))
                    _Mapped_address = e.Data.Split(':')[1].Trim();
                if (e.Data.StartsWith("Nat mapping behavior: "))
                    _Nat_mapping_behavior = e.Data.Split(':')[1].Trim();
                if (e.Data.StartsWith("Nat filtering behavior: "))
                    _Nat_filtering_behavior = e.Data.Split(':')[1].Trim();
                if (e.Data.StartsWith("result: "))
                    _lastResult = e.Data.Split(':')[1].Trim();
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}
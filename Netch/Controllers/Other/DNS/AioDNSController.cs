using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Netch.Controllers.Other.DNS
{
    public class AioDNSController : Interface.IController
    {
        private Tools.Guard Guard = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\aiodns.exe"),
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"),
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            },
            JudgmentStarted = new List<string>()
            {
                "Started"
            },
            JudgmentStopped = new List<string>()
            {
                "[aiodns][main]"
            },
            AutoRestart = true
        };

        public bool Create(Models.Server.Server s, Models.Mode.Mode m)
        {
            Global.Logger.Info(String.Format("{0:x} aiodns.exe", Utils.FileHelper.Checksum("bin\\aiodns.exe")));

            this.Guard.StartInfo.Arguments = String.Format("-c {} -l {} -cdns {} -odns {}", ".\\aiodns.conf", ":53", Global.Config.AioDNS.ChinaDNS, Global.Config.AioDNS.OtherDNS);
            return this.Guard.Create();
        }

        public bool Delete()
        {
            this.Guard.Delete();

            return true;
        }
    }
}

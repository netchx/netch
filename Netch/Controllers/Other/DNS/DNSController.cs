using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netch.Controllers.Other.DNS
{
    public class DNSController : Interface.IController
    {
        private Tools.Guard Guard = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin\\dnsproxy.exe"),
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin"),
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            },
            JudgmentStarted = new List<string>()
            {
            },
            JudgmentStopped = new List<string>()
            {
            },
            AutoRestart = true
        };

        public bool Create(Models.Server.Server s, Models.Mode.Mode m)
        {
            throw new NotImplementedException();
        }

        public bool Delete()
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Netch.Controllers.Server
{
    public class ShadowsocksRController : Interface.IController
    {
        private Tools.Guard Guard = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin\\ShadowsocksR.exe"),
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin"),
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            },
            JudgmentStarted = new List<string>()
            {
                "listening at"
            },
            JudgmentStopped = new List<string>()
            {
                "usage",
                "invalid"
            },
            AutoRestart = true
        };

        public bool Create(Models.Server.Server s, Models.Mode.Mode m)
        {
            var node = s as Models.Server.ShadowsocksR.ShadowsocksR;

            var sb = new StringBuilder();
            sb.Append($"-l {Global.Config.Ports.Socks} -s {node.Resolve()} -p {node.Port} -k '{node.Passwd}' -O {node.Prot} -o {node.OBFS} -t 30 -u --fast-open --no-delay");
            if (!String.IsNullOrEmpty(node.ProtParam)) sb.Append($" -G '{node.ProtParam}'");
            if (!String.IsNullOrEmpty(node.OBFSParam)) sb.Append($" -g '{node.OBFSParam}'");

            this.Guard.StartInfo.Arguments = sb.ToString();
            return this.Guard.Create();
        }

        public bool Delete()
        {
            this.Guard.Delete();

            return true;
        }
    }
}

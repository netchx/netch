using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.Shadowsocks;

namespace Netch.Utils
{
    public static class Bandwidth
    {
        public static ulong received;
        public static TraceEventSession? tSession;

        private static readonly string[] Suffix = {"B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"};

        /// <summary>
        ///     计算流量
        /// </summary>
        /// <param name="d"></param>
        /// <returns>带单位的流量字符串</returns>
        public static string Compute(ulong d)
        {
            const double step = 1024.00;

            byte level = 0;
            double? size = null;
            while ((size ?? d) > step)
            {
                if (level >= 6) // Suffix.Length - 1
                    break;

                level++;
                size = (size ?? d) / step;
            }

            return $@"{size ?? 0:0.##} {Suffix[level]}";
        }

        /// <summary>
        ///     根据程序名统计流量
        /// </summary>
        public static void NetTraffic()
        {
            if (!Global.Flags.IsWindows10Upper)
                return;

            var counterLock = new object();
            //int sent = 0;

            //var processList = Process.GetProcessesByName(ProcessName).Select(p => p.Id).ToHashSet();
            var instances = new List<Process>();
            switch (MainController.ServerController)
            {
                case null:
                    break;
                case SSController {DllFlag: true}:
                    instances.Add(Process.GetCurrentProcess());
                    break;
                case Guard instanceController:
                    if (instanceController.Instance != null)
                        instances.Add(instanceController.Instance);

                    break;
            }

            if (!instances.Any())
                switch (MainController.ModeController)
                {
                    case null:
                        break;
                    case HTTPController httpController:
                        instances.Add(httpController.PrivoxyController.Instance!);
                        break;
                    case NFController _:
                        instances.Add(Process.GetCurrentProcess());
                        break;
                    case Guard instanceController:
                        instances.Add(instanceController.Instance!);
                        break;
                }

            var processList = instances.Select(instance => instance.Id).ToList();

            Logging.Info("流量统计进程:" + string.Join(",", instances.Select(instance => $"({instance.Id})" + instance.ProcessName).ToArray()));

            received = 0;

            if (!instances.Any())
                return;

            Global.MainForm.BandwidthState(true);

            Task.Run(() =>
            {
                tSession = new TraceEventSession("KernelAndClrEventsSession");
                tSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                //这玩意儿上传和下载得到的data是一样的:)
                //所以暂时没办法区分上传下载流量
                tSession.Source.Kernel.TcpIpRecv += data =>
                {
                    if (processList.Contains(data.ProcessID))
                        lock (counterLock)
                            received += (ulong) data.size;

                    // Debug.WriteLine($"TcpIpRecv: {ToByteSize(data.size)}");
                };

                tSession.Source.Kernel.UdpIpRecv += data =>
                {
                    if (processList.Contains(data.ProcessID))
                        lock (counterLock)
                            received += (ulong) data.size;

                    // Debug.WriteLine($"UdpIpRecv: {ToByteSize(data.size)}");
                };

                tSession.Source.Process();
            });

            while (Global.MainForm.State != State.Stopped)
            {
                Task.Delay(1000).Wait();
                lock (counterLock)
                    Global.MainForm.OnBandwidthUpdated(received);
            }
        }

        public static void Stop()
        {
            tSession?.Dispose();
            received = 0;
        }
    }
}
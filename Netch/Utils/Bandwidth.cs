using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Netch.Controllers;
using Netch.Models;

namespace Netch.Utils
{
    public static class Bandwidth
    {
        public static ulong received;
        public static TraceEventSession tSession;

        /// <summary>
        ///     计算流量
        /// </summary>
        /// <param name="bandwidth">流量</param>
        /// <returns>带单位的流量字符串</returns>
        public static string Compute(ulong size)
        {
            var mStrSize = @"0";
            const double step = 1024.00;
            var factSize = size;
            if (factSize < step)
            {
                mStrSize = $@"{factSize:0.##} B";
            }
            else if (factSize >= step && factSize < 1048576)
            {
                mStrSize = $@"{factSize / step:0.##} KB";
            }
            else if (factSize >= 1048576 && factSize < 1073741824)
            {
                mStrSize = $@"{factSize / step / step:0.##} MB";
            }
            else if (factSize >= 1073741824 && factSize < 1099511627776)
            {
                mStrSize = $@"{factSize / step / step / step:0.##} GB";
            }
            else if (factSize >= 1099511627776)
            {
                mStrSize = $@"{factSize / step / step / step / step:0.##} TB";
            }

            return mStrSize;
        }

        public static bool NetTrafficAvailable => /*Global.Settings.EnableNetTraffic && */Environment.OSVersion.Version.Major >= 10;

        /// <summary>
        /// 根据程序名统计流量
        /// </summary>
        public static void NetTraffic(Server server, Mode mode)
        {
            if (!NetTrafficAvailable)
                return;

            var counterLock = new object();
            //int sent = 0;

            //var processList = Process.GetProcessesByName(ProcessName).Select(p => p.Id).ToHashSet();
            var instances = new List<Process>();
            if (server.Type.Equals("Socks5") && MainController.ModeController.Name == "HTTP")
            {
                instances.Add(((HTTPController) MainController.ModeController).pPrivoxyController.Instance);
            }
            else if (server.Type.Equals("SS") && Global.Settings.BootShadowsocksFromDLL &&
                     (mode.Type == 0 || mode.Type == 1 || mode.Type == 2))
            {
                instances.Add(Process.GetCurrentProcess());
            }
            else if (MainController.EncryptedProxyController != null)
            {
                instances.Add(MainController.EncryptedProxyController.Instance);
            }
            else if (MainController.ModeController != null)
            {
                instances.Add(MainController.ModeController.Instance);
            }

            var processList = instances.Select(instance => instance.Id).ToList();

            Logging.Info("流量统计进程:" + string.Join(",",
                instances.Select(instance => $"({instance.Id})" + instance.ProcessName).ToArray()));

            Task.Run(() =>
            {
                tSession = new TraceEventSession("KernelAndClrEventsSession");
                tSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                //这玩意儿上传和下载得到的data是一样的:)
                //所以暂时没办法区分上传下载流量
                tSession.Source.Kernel.TcpIpRecv += data =>
                {
                    if (processList.Contains(data.ProcessID))
                    {
                        lock (counterLock)
                            received += (ulong) data.size;

                        // Debug.WriteLine($"TcpIpRecv: {ToByteSize(data.size)}");
                    }
                };
                tSession.Source.Kernel.UdpIpRecv += data =>
                {
                    if (processList.Contains(data.ProcessID))
                    {
                        lock (counterLock)
                            received += (ulong) data.size;

                        // Debug.WriteLine($"UdpIpRecv: {ToByteSize(data.size)}");
                    }
                };
                tSession.Source.Process();
            });

            while (Global.MainForm.State != State.Stopped)
            {
                Task.Delay(1000).Wait();
                lock (counterLock)
                {
                    Global.MainForm.OnBandwidthUpdated(received);
                }
            }
        }

        public static void Stop()
        {
            tSession?.Dispose();
            received = 0;
        }
    }
}
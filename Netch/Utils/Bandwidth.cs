using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Netch.Controllers;
using Netch.Forms;
using Netch.Models;

namespace Netch.Utils
{
    public static class Bandwidth
    {
        public static int received;

        /// <summary>
        ///     计算流量
        /// </summary>
        /// <param name="bandwidth">流量</param>
        /// <returns>带单位的流量字符串</returns>
        public static string Compute(long bandwidth)
        {
            string[] units = {"KB", "MB", "GB", "TB", "PB"};
            double result = bandwidth;
            var i = -1;

            do
            {
                i++;
            } while ((result /= 1024) > 1024);

            if (result < 0)
            {
                result = 0;
            }

            return string.Format("{0} {1}", Math.Round(result, 2), units[i]);
        }

        /// <summary>
        /// 根据程序名统计流量
        /// </summary>
        /// <param name="ProcessName"></param>
        public static void NetTraffic(Server server, Mode mode, MainController mainController)
        {
            var counterLock = new object();
            //int sent = 0;

            //var processList = Process.GetProcessesByName(ProcessName).Select(p => p.Id).ToHashSet();
            var instances = new List<Process>();
            if (server.Type.Equals("Socks5") && mainController.pModeController.Name == "HTTP")
            {
                instances.Add(((HTTPController) mainController.pModeController).pPrivoxyController.Instance);
            }
            else if (server.Type.Equals("SS") && Global.Settings.BootShadowsocksFromDLL)
            {
                instances.Add(Process.GetCurrentProcess());
            }
            else if (mainController.pEncryptedProxyController != null)
            {
                instances.Add(mainController.pEncryptedProxyController.Instance);
            }
            else if (mainController.pModeController != null)
            {
                instances.Add(mainController.pModeController.Instance);
            }

            var processList = instances.Select(instance => instance.Id).ToList();

            Logging.Info("流量统计进程:" + string.Join(",", instances.Select(instance => $"({instance.Id})"+instance.ProcessName).ToArray()));

            Task.Run(() =>
            {
                using (var session = new TraceEventSession("KernelAndClrEventsSession"))
                {
                    session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                    //这玩意儿上传和下载得到的data是一样的:)
                    //所以暂时没办法区分上传下载流量
                    session.Source.Kernel.TcpIpRecv += data =>
                    {
                        if (processList.Contains(data.ProcessID))
                        {
                            lock (counterLock)
                                received += data.size;
                            //Logging.Info($"TcpIpRecv: {Compute(data.size)}");
                        }
                    };
                    session.Source.Kernel.UdpIpRecv += data =>
                    {
                        if (processList.Contains(data.ProcessID))
                        {
                            lock (counterLock)
                                received += data.size;
                            //Logging.Info($"UdpIpRecv: {Compute(data.size)}");
                        }
                    };

                    session.Source.Process();
                }
            });

            if ((Convert.ToInt32(Global.MainForm.LastDownloadBandwidth) - Convert.ToInt32(received)) == 0)
            {
                Global.MainForm.OnBandwidthUpdated(0);
                received = 0;
            }

            while (Global.MainForm.State != State.Stopped)
            {
                Task.Delay(1000).Wait();
                lock (counterLock)
                {
                    Global.MainForm.OnBandwidthUpdated(Convert.ToInt64(received));
                }
            }
        }
    }
}
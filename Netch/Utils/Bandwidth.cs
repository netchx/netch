using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Netch.Controllers;
using Netch.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Netch.Utils
{
    public static class Bandwidth
    {
        public static int received = 0;

        /// <summary>
		///     计算流量
		/// </summary>
		/// <param name="bandwidth">流量</param>
		/// <returns>带单位的流量字符串</returns>
		public static string Compute(long bandwidth)
        {
            string[] units = { "KB", "MB", "GB", "TB", "PB" };
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

            return string.Format("{0} {1}", System.Math.Round(result, 2), units[i]);
        }

        /// <summary>
        /// 根据程序名统计流量
        /// </summary>
        /// <param name="ProcessName"></param>
        public static void NetTraffic(Models.Server server, Models.Mode mode, MainController mainController)
        {
            var counterLock = new object();
            //int sent = 0;

            //var processList = Process.GetProcessesByName(ProcessName).Select(p => p.Id).ToHashSet();
            List<int> processList = new List<int>();

            if (server.Type.Equals("Socks5") && mainController.pHTTPController != null)
            {
                processList.Add(mainController.pHTTPController.pPrivoxyController.Instance.Id);
            }
            else if (server.Type.Equals("SS") && Global.Settings.BootShadowsocksFromDLL)
            {
                processList.Add(Process.GetCurrentProcess().Id);
            }
            else if (server.Type.Equals("SS") && mainController.pSSController != null)
            {
                processList.Add(mainController.pSSController.Instance.Id);
            }
            else if (server.Type.Equals("SSR") && mainController.pSSRController != null)
            {
                processList.Add(mainController.pSSRController.Instance.Id);
            }
            else if (server.Type.Equals("VMess") && mainController.pVMessController != null)
            {
                processList.Add(mainController.pVMessController.Instance.Id);
            }
            else if (server.Type.Equals("TR") && mainController.pTrojanController != null)
            {
                processList.Add(mainController.pTrojanController.Instance.Id);
            }
            else if (mainController.pTUNTAPController != null)
            {
                processList.Add(mainController.pTUNTAPController.Instance.Id);
            }
            else if (mainController.pNFController != null)
            {
                processList.Add(mainController.pNFController.Instance.Id);
            }
            Logging.Info("启动流量统计 PID：" + string.Join(",", processList.ToArray()));

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

            if ((Convert.ToInt32(MainForm.Instance.LastDownloadBandwidth) - Convert.ToInt32(received)) == 0)
            {
                MainForm.Instance.OnBandwidthUpdated(0);
                received = 0;
            }
            while (MainForm.Instance.State != Models.State.Stopped)
            {
                Task.Delay(1000).Wait();
                lock (counterLock)
                {
                    MainForm.Instance.OnBandwidthUpdated(Convert.ToInt64(received));
                }
            }

        }
    }
}

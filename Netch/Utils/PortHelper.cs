using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using Netch.Controllers;

namespace Netch.Utils
{
    public static class PortHelper
    {
        private static readonly List<int[]> TCPExcludedRanges = new List<int[]>();
        private static readonly List<int[]> UDPExcludedRanges = new List<int[]>();

        static PortHelper()
        {
            try
            {
                GetExcludedPortRange(PortType.TCP, ref TCPExcludedRanges);
                GetExcludedPortRange(PortType.UDP, ref UDPExcludedRanges);
            }
            catch (Exception e)
            {
                Logging.Error("获取保留端口失败: " + e);
            }
        }

        private static void GetExcludedPortRange(PortType portType, ref List<int[]> targetList)
        {
            var lines = new List<string>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $" int ipv4 show excludedportrange {portType}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) lines.Add(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            var splitLine = false;
            foreach (var line in lines)
            {
                if (!splitLine)
                {
                    if (line.StartsWith("-"))
                    {
                        splitLine = true;
                    }
                }
                else
                {
                    if (line == string.Empty)
                        break;

                    var value = line.Trim().Split(' ').Where(s => s != string.Empty);

                    var port = 0;
                    var _ = (from s1 in value
                        where int.TryParse(s1, out port)
                        select port).ToArray();

                    targetList.Add(_);
                }
            }
        }

        /// <summary>
        ///     检查端口是否是保留端口
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="type">端口类型</param>
        /// <returns>是否是保留端口</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static bool IsPortExcluded(int port, PortType type)
        {
            return type switch
            {
                PortType.TCP => TCPExcludedRanges.Any(range => range[0] <= port && port <= range[1]),
                PortType.UDP => UDPExcludedRanges.Any(range => range[0] <= port && port <= range[1]),
                PortType.Both => IsPortExcluded(port, PortType.TCP) || IsPortExcluded(port, PortType.UDP),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary>
        ///     指定类型的端口是否已经被使用了
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="type">检查端口类型</param>
        /// <returns>是否被占用</returns>
        public static bool PortInUse(int port, PortType type = PortType.Both)
        {
            var netInfo = IPGlobalProperties.GetIPGlobalProperties();
            var isTcpUsed = type != PortType.UDP &&
                            (IsPortExcluded(port, PortType.TCP) ||
                             netInfo.GetActiveTcpListeners().Any(ipEndPoint => ipEndPoint.Port == port));
            var isUdpUsed = type != PortType.TCP &&
                            (IsPortExcluded(port, PortType.UDP) ||
                             netInfo.GetActiveUdpListeners().Any(ipEndPoint => ipEndPoint.Port == port));
            var isPortExcluded = !MainController.UsingPorts.Contains(port);

            return isPortExcluded && (isTcpUsed || isUdpUsed);
        }

        public static int GetAvailablePort()
        {
            for (var i = 0; i < 55535; i++)
            {
                var p = new Random().Next(10000, 65535);
                if (!PortInUse(p))
                {
                    return p;
                }
            }

            throw new Exception("Cant Generate Available Port");
        }
    }

    /// <summary>
    ///     检查端口类型
    /// </summary>
    public enum PortType
    {
        TCP,
        UDP,
        Both
    }
}
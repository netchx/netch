using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using Netch.Models;

namespace Netch.Utils
{
    public static class PortHelper
    {
        private static readonly List<Range> TCPReservedRanges = new();
        private static readonly List<Range> UDPReservedRanges = new();
        private static readonly IPGlobalProperties NetInfo = IPGlobalProperties.GetIPGlobalProperties();

        static PortHelper()
        {
            try
            {
                GetReservedPortRange(PortType.TCP, ref TCPReservedRanges);
                GetReservedPortRange(PortType.UDP, ref UDPReservedRanges);
            }
            catch (Exception e)
            {
                Logging.Error("获取保留端口失败: " + e);
            }
        }

        private static void GetReservedPortRange(PortType portType, ref List<Range> targetList)
        {
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

            process.Start();
            var output = process.StandardOutput.ReadToEnd();

            foreach (var line in output.SplitRemoveEmptyEntriesAndTrimEntries('\n'))
            {
                var value = line.Trim().SplitRemoveEmptyEntries(' ');
                if (value.Length != 2)
                    continue;

                if (!ushort.TryParse(value[0], out var start) || !ushort.TryParse(value[1], out var end))
                    continue;

                targetList.Add(new Range(start, end));
            }
        }

        /// <summary>
        ///     指定类型的端口是否已经被使用了
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="type">检查端口类型</param>
        /// <returns>是否被占用</returns>
        public static void CheckPort(ushort port, PortType type = PortType.Both)
        {
            switch (type)
            {
                case PortType.Both:
                    CheckPort(port, PortType.TCP);
                    CheckPort(port, PortType.UDP);
                    break;
                default:
                    CheckPortInUse(port, type);
                    CheckPortReserved(port, type);
                    break;
            }
        }

        private static void CheckPortInUse(ushort port, PortType type)
        {
            switch (type)
            {
                case PortType.Both:
                    CheckPortInUse(port, PortType.TCP);
                    CheckPortInUse(port, PortType.UDP);
                    break;
                case PortType.TCP:
                    if (NetInfo.GetActiveTcpListeners().Any(ipEndPoint => ipEndPoint.Port == port))
                        throw new PortInUseException();

                    break;
                case PortType.UDP:
                    if (NetInfo.GetActiveUdpListeners().Any(ipEndPoint => ipEndPoint.Port == port))
                        throw new PortInUseException();

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        ///     检查端口是否是保留端口
        /// </summary>
        private static void CheckPortReserved(ushort port, PortType type)
        {
            switch (type)
            {
                case PortType.Both:
                    CheckPortReserved(port, PortType.TCP);
                    CheckPortReserved(port, PortType.UDP);
                    return;
                case PortType.TCP:
                    if (TCPReservedRanges.Any(range => range.InRange(port)))
                        throw new PortReservedException();

                    break;
                case PortType.UDP:
                    if (UDPReservedRanges.Any(range => range.InRange(port)))
                        throw new PortReservedException();

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static ushort GetAvailablePort(PortType portType = PortType.Both)
        {
            var random = new Random();
            for (ushort i = 0; i < 55535; i++)
            {
                var p = (ushort) random.Next(10000, 65535);
                try
                {
                    CheckPort(p, portType);
                    return p;
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            throw new Exception();
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

    public class PortInUseException : Exception
    {
        public PortInUseException(string message) : base(message)
        {
        }

        public PortInUseException()
        {
        }
    }

    public class PortReservedException : Exception
    {
        public PortReservedException(string message) : base(message)
        {
        }

        public PortReservedException()
        {
        }
    }
}
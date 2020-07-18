using System.Linq;
using System.Net.NetworkInformation;
using Netch.Controllers;

namespace Netch.Utils
{
    public static class PortHelper
    {
        /// <summary>
        ///     指定类型的端口是否已经被使用了
        /// </summary>
        /// <param name="port">端口</param>
        /// <param name="type">检查端口类型</param>
        /// <returns>是否被占用</returns>
        public static bool PortInUse(int port, PortType type = PortType.Both)
        {
            var netInfo = IPGlobalProperties.GetIPGlobalProperties();
            var tcpResult = type != PortType.UDP && netInfo.GetActiveTcpListeners().Any(ipEndPoint => !MainController.UsingPorts.Contains(ipEndPoint.Port) && ipEndPoint.Port == port);
            var udpResult = type != PortType.TCP && netInfo.GetActiveUdpListeners().Any(ipEndPoint => !MainController.UsingPorts.Contains(ipEndPoint.Port) && ipEndPoint.Port == port);

            return tcpResult || udpResult;
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
using System;
using System.Collections;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;

namespace Netch.Utils
{
    public static class DNS
    {
        /// <summary>
        ///     缓存
        /// </summary>
        public static Hashtable Cache = new Hashtable();

        /// <summary>
        ///     查询
        /// </summary>
        /// <param name="hostname">主机名</param>
        /// <returns></returns>
        public static IPAddress Lookup(string hostname)
        {
            try
            {
                if (Cache.Contains(hostname))
                {
                    return Cache[hostname] as IPAddress;
                }

                var task = Dns.GetHostAddressesAsync(hostname);
                if (!task.Wait(1000))
                {
                    return null;
                }

                if (task.Result.Length == 0)
                {
                    return null;
                }

                Cache.Add(hostname, task.Result[0]);

                return task.Result[0];
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// 设置DNS
        /// </summary>
        /// <param name="dns"></param>
        public static void SetDNS(string[] dns)
        {
            ManagementClass wmi = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = wmi.GetInstances();
            ManagementBaseObject inPar = null;
            ManagementBaseObject outPar = null;
            foreach (ManagementObject mo in moc)
            {
                //如果没有启用IP设置的网络设备则跳过，如果是虚拟机网卡也跳过
                if (!(bool)mo["IPEnabled"] ||
                    mo["Description"].ToString().Contains("Virtual") ||
                    mo["Description"].ToString().Contains("VMware") ||
                    mo["Description"].ToString().Contains("TAP"))
                    continue;

                //设置DNS地址
                if (dns != null)
                {
                    inPar = mo.GetMethodParameters("SetDNSServerSearchOrder");
                    inPar["DNSServerSearchOrder"] = dns;
                    outPar = mo.InvokeMethod("SetDNSServerSearchOrder", inPar, null);
                }
            }
        }
        /// <summary>
        /// 从网卡获取ip设置信息
        /// </summary>
        public static string[] getSystemDns()
        {
            var systemDns = new[] { "223.5.5.5", "1.1.1.1" };
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
                if (!network.Description.Contains("Virtual") &&
                    !network.Description.Contains("VMware") &&
                    !network.Description.Contains("TAP") &&
                    network.OperationalStatus == OperationalStatus.Up &&
                    network.GetIPProperties()?.GatewayAddresses.Count != 0)
                {
                    systemDns = network.GetIPProperties().DnsAddresses.Select(dns => dns.ToString()).ToArray();
                }

            return systemDns;
        }
    }
}
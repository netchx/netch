using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Win32;

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

        private static RegistryKey AdapterRegistry(bool write = false)
        {
            if (Global.Outbound.Adapter == null)
                Utils.SearchOutboundAdapter();
            return Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{Global.Outbound.Adapter.Id}", write);
        }

        /// <summary>
        /// 出口网卡 DNS
        /// <para></para>
        /// 依赖 <see cref="Global.Outbound.Adapter"/>
        /// </summary>
        public static string OutboundDNS
        {
            get
            {
                try
                {
                    return (string) AdapterRegistry().GetValue("NameServer");
                }
                catch
                {
                    return string.Empty;
                }
            }
            set => AdapterRegistry(true).SetValue("NameServer", value, RegistryValueKind.String);
        }

        public static IEnumerable<string> Split(string dns)
        {
            return dns.Split(',').Where(ip => !string.IsNullOrWhiteSpace(ip)).Select(ip => ip.Trim());
        }

        public static bool TrySplit(string value, out IEnumerable<string> result, ushort maxCount = 0)
        {
            result = Split(value).ToArray();

            return maxCount == 0 || result.Count() <= maxCount
                &&
                result.All(ip => IPAddress.TryParse(ip, out _));
        }

        public static string Join(IEnumerable<string> dns)
        {
            return string.Join(",", dns);
        }
    }
}
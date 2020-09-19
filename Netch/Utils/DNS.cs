using System;
using System.Collections;
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
    }
}
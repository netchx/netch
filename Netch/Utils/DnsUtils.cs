using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Netch.Utils
{
    public static class DnsUtils
    {
        /// <summary>
        ///     缓存
        /// </summary>
        private static readonly Hashtable Cache = new();

        /// <summary>
        ///     查询
        /// </summary>
        /// <param name="hostname">主机名</param>
        /// <returns></returns>
        public static IPAddress? Lookup(string hostname)
        {
            try
            {
                if (Cache.Contains(hostname))
                    return Cache[hostname] as IPAddress;

                var task = Dns.GetHostAddressesAsync(hostname);
                if (!task.Wait(1000))
                    return null;

                if (task.Result.Length == 0)
                    return null;

                Cache.Add(hostname, task.Result[0]);

                return task.Result[0];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        ///     查询
        /// </summary>
        /// <param name="hostname">主机名</param>
        /// <returns></returns>
        public static void ClearCache()
        {
            Cache.Clear();
        }

        public static IEnumerable<string> Split(string dns)
        {
            return dns.SplitRemoveEmptyEntriesAndTrimEntries(',');
        }

        public static bool TrySplit(string value, out IEnumerable<string> result, ushort maxCount = 0)
        {
            result = Split(value).ToArray();

            return maxCount == 0 || result.Count() <= maxCount && result.All(ip => IPAddress.TryParse(ip, out _));
        }

        public static string Join(IEnumerable<string> dns)
        {
            return string.Join(",", dns);
        }
    }
}
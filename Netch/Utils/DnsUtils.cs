using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Serilog;

namespace Netch.Utils
{
    public static class DnsUtils
    {
        /// <summary>
        ///     缓存
        /// </summary>
        private static readonly Hashtable Cache = new();

        public static async Task<IPAddress?> LookupAsync(string hostname, int timeout = 3000)
        {
            try
            {
                if (Cache.Contains(hostname))
                    return Cache[hostname] as IPAddress;

                var task = Dns.GetHostAddressesAsync(hostname);

                var resTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);

                if (resTask == task)
                {
                    var result = await task;

                    if (result.Length == 0)
                        return null;

                    Cache.Add(hostname, result[0]);

                    return result[0];
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Verbose(e, "Lookup hostname {Hostname} failed", hostname);
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
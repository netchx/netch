using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private static readonly Hashtable Cache6 = new();

        public static async Task<IPAddress?> LookupAsync(string hostname, AddressFamily inet = AddressFamily.InterNetwork, int timeout = 3000)
        {
            try
            {
                if (inet == AddressFamily.InterNetwork)
                {
                    if (Cache.Contains(hostname))
                        return Cache[hostname] as IPAddress;
                }
                else
                {
                    Trace.Assert(inet == AddressFamily.InterNetworkV6);
                    if (Cache6.Contains(hostname))
                        return Cache6[hostname] as IPAddress;
                }

                var task = Dns.GetHostAddressesAsync(hostname);

                var resTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);

                if (resTask == task)
                {
                    var addresses = await task;

                    var result = addresses.FirstOrDefault(i => i.AddressFamily == inet);
                    if (result == null)
                        return null;

                    if (inet == AddressFamily.InterNetwork)
                        Cache.Add(hostname, result);
                    else
                        Cache6.Add(hostname, result);

                    return result;
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
            Cache6.Clear();
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
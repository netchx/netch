using System;
using System.Collections;
using System.Net;

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
    }
}

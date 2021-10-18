using System;
using System.Collections;
using System.Net;

namespace Netch.Utils
{
    public static class DNS
    {
        /// <summary>
        ///     缓存内容
        /// </summary>
        private class CacheEntry
        {
            /// <summary>
            ///     缓存时间
            /// </summary>
            public long Unix;

            /// <summary>
            ///     地址
            /// </summary>
            public IPAddress IP;
        }

        /// <summary>
        ///     缓存表
        /// </summary>
        private static Hashtable Cache = new Hashtable();

        /// <summary>
        ///     获取 IP 地址
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IPAddress Fetch(string name)
        {
            try
            {
                if (Cache.Contains(name))
                {
                    var data = Cache[name] as CacheEntry;

                    if (DateTimeOffset.Now.ToUnixTimeSeconds() - data.Unix < 120)
                        return data.IP;

                    Cache.Remove(name);
                }

                var task = Dns.GetHostAddressesAsync(name);
                if (!task.Wait(1000))
                    return IPAddress.Any;

                if (task.Result.Length == 0)
                    return IPAddress.Any;

                Cache.Add(name, new CacheEntry() { Unix = DateTimeOffset.Now.ToUnixTimeSeconds(), IP = task.Result[0] });
                return task.Result[0];
            }
            catch (Exception e)
            {
                Global.Logger.Warning(e.ToString());

                return IPAddress.Any;
            }
        }
    }
}

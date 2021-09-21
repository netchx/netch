using System;
using System.Collections;
using System.Net;

namespace Netch.Utils
{
    public static class DNS
    {
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
                    return Cache[name] as IPAddress;
                }

                var task = Dns.GetHostAddressesAsync(name);
                if (!task.Wait(1000))
                {
                    return IPAddress.Any;
                }

                if (task.Result.Length == 0)
                {
                    return IPAddress.Any;
                }

                Cache.Add(name, task.Result[0]);
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

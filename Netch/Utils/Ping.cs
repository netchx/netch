using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Netch.Utils
{
    public static class Ping
    {
        /// <summary>
        ///     缓存内容
        /// </summary>
        private class CacheEntry
        {
            /// <summary>
            ///      缓存时间
            /// </summary>
            public long Unix;

            /// <summary>
            ///     延迟
            /// </summary>
            public int Time;
        }

        /// <summary>
        ///     缓存表
        /// </summary>
        private static Hashtable Cache = new Hashtable();

        /// <summary>
        ///     测试 ICMP 延迟
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        private static int ICMPing(IPAddress addr)
        {
            using (var client = new System.Net.NetworkInformation.Ping())
            {
                var tk = client.SendPingAsync(addr);
                if (!tk.Wait(1000))
                {
                    return 999;
                }

                return Convert.ToInt32(tk.Result.RoundtripTime);
            }
        }

        /// <summary>
        ///     测试 TCP 延迟
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private static int TCPPing(IPAddress addr, ushort port)
        {
            using (var client = new TcpClient())
            {
                var sw = Stopwatch.StartNew();
                var tk = client.ConnectAsync(addr, port);

                if (!tk.Wait(1000))
                {
                    sw.Stop();
                    return 999;
                }

                sw.Stop();
                return Convert.ToInt32(sw.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        ///     获取延迟
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int Fetch(Models.Server.Server s)
        {
            /*
             * -1  : Not Test
             * -2  : DNS Exception
             * -3  : Exception
             * 999 : Timeout
             */

            try
            {
                var addr = DNS.Fetch(s.Host);
                if (addr == IPAddress.Any)
                {
                    return -2;
                }

                if (Cache.Contains(addr))
                {
                    var rule = Cache[addr] as CacheEntry;
                    if (DateTimeOffset.Now.ToUnixTimeSeconds() - rule.Unix < 30)
                    {
                        return rule.Time;
                    }
                    else
                    {
                        Cache.Remove(addr);
                    }
                }

                var time = 0;
                if (Global.Config.Generic.ICMPing)
                {
                    time = ICMPing(addr);
                }
                else
                {
                    return TCPPing(addr, s.Port);
                }

                Cache.Add(addr, new CacheEntry() { Unix = DateTimeOffset.Now.ToUnixTimeSeconds(), Time = time });
                return time;
            }
            catch (Exception e)
            {
                Global.Logger.Warning(e.ToString());
                return -3;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Netch.Utils
{
    public static class Utils
    {
        public static bool OpenUrl(string path)
        {
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    }
                }.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool OpenDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    return OpenUrl(dir);
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        public static async Task<int> TCPingAsync(IPAddress ip, int port, int timeout = 1000, CancellationToken ct = default)
        {
            using var client = new TcpClient(ip.AddressFamily);
            var task = client.ConnectAsync(ip, port);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var resTask = await Task.WhenAny(Task.Delay(timeout, ct), task);

            stopwatch.Stop();
            if (resTask == task && client.Connected)
            {
                var t = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
                return t;
            }
            return timeout;
        }
    }
}

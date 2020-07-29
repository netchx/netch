using MaxMind.GeoIP2;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Netch.Utils
{
    public static class Utils
    {
        public static bool Open(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
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

        public static string GetCityCode(string Hostname)
        {
            if (Hostname.Contains(":"))
            {
                Hostname = Hostname.Split(':')[0];
            }

            string Country;
            try
            {
                var databaseReader = new DatabaseReader("bin\\GeoLite2-Country.mmdb");

                if (IPAddress.TryParse(Hostname, out _) == true)
                {
                    Country = databaseReader.Country(Hostname).Country.IsoCode;
                }
                else
                {
                    var DnsResult = DNS.Lookup(Hostname);

                    if (DnsResult != null)
                    {
                        Country = databaseReader.Country(DnsResult).Country.IsoCode;
                    }
                    else
                    {
                        Country = "Unknown";
                    }
                }
            }
            catch (Exception)
            {
                Country = "Unknown";
            }

            return Country == null ? "Unknown" : Country;
        }

        public static string SHA256CheckSum(string filePath)
        {
            try
            {
                var SHA256 = SHA256Managed.Create();
                var fileStream = File.OpenRead(filePath);
                return SHA256.ComputeHash(fileStream).Aggregate(string.Empty, (current, b) => current + b.ToString("x2"));
            }
            catch
            {
                return "";
            }
        }
    }
}
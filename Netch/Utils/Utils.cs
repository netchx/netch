using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MaxMind.GeoIP2;

namespace Netch.Utils
{
    public static class Utils
    {
        public static bool Open(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
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

                if (IPAddress.TryParse(Hostname, out _))
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
                var SHA256 = System.Security.Cryptography.SHA256.Create();
                var fileStream = File.OpenRead(filePath);
                return SHA256.ComputeHash(fileStream).Aggregate(string.Empty, (current, b) => current + b.ToString("x2"));
            }
            catch
            {
                return "";
            }
        }

        public static void KillProcessByName(string name)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                    if (p.MainModule != null && p.MainModule.FileName.StartsWith(Global.NetchDir))
                        p.Kill();
            }
            catch (Win32Exception e)
            {
                Logging.Error($"结束进程 {name} 错误：" + e.Message);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static string FileVersion(string file) => File.Exists(file) ? FileVersionInfo.GetVersionInfo(file).FileVersion : string.Empty;

        public static bool SearchOutboundAdapter(bool log = true)
        {
            IPAddress localEnd;
            try
            {
                localEnd = WebUtil.BestLocalEndPoint(new IPEndPoint(0x72727272, 53)).Address;
            }
            catch
            {
                return false;
            }

            try
            {
                // 根据 IP 寻找 出口适配器
                Global.Outbound.Adapter = NetworkInterface.GetAllNetworkInterfaces().First(_ =>
                {
                    try
                    {
                        return _.GetIPProperties().UnicastAddresses.Any(ip =>
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.Address.ToString().Equals(localEnd.ToString()))
                            {
                                Global.Outbound.Index = _.GetIPProperties().GetIPv4Properties().Index;
                                return true;
                            }

                            return false;
                        });
                    }
                    catch
                    {
                        return false;
                    }
                });
                Global.Outbound.Gateway = Global.Outbound.Adapter.GetIPProperties().GatewayAddresses[0].Address;
                if (log)
                {
                    Logging.Info($"出口 IPv4 地址：{Global.Outbound.Address}");
                    Logging.Info($"出口 网关 地址：{Global.Outbound.Gateway}");
                    Logging.Info($"出口适配器：{Global.Outbound.Adapter.Name} {Global.Outbound.Adapter.Id} {Global.Outbound.Adapter.Description}, index: {Global.Outbound.Index}");
                }

                return true;
            }
            catch (Exception e)
            {
                Logging.Error($"找不到出口IP所在网卡{e}");
                return false;
            }
        }

        public static void LoggingAdapters(string id)
        {
            var adapter = NetworkInterface.GetAllNetworkInterfaces().First(adapter => adapter.Id == id);
            Logging.Warning($"检索此网卡信息出错: {adapter.Name} {adapter.Id} {adapter.Description}");
        }
    }
}
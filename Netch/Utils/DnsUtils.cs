using DnsClient;
using Microsoft.VisualStudio.Threading;
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace Netch.Utils;

public static class DnsUtils
{
    private static readonly AsyncSemaphore Lock = new(1);

    /// <summary>
    ///     缓存
    /// </summary>
    private static readonly Hashtable Cache = new();
    private static readonly Hashtable Cache6 = new();

    public static async Task<IPAddress?> LookupAsync(string hostname, AddressFamily inet = AddressFamily.Unspecified, int timeout = 3000, string? dns = null)
    {
        using var _ = await Lock.EnterAsync();
        if (IPAddress.TryParse(hostname, out var ip))
        {
            // AddressFamily 过滤
            if (inet == AddressFamily.Unspecified || ip.AddressFamily == inet)
                return ip;
        }

        try
        {
            var cacheResult = inet switch
            {
                AddressFamily.Unspecified => (IPAddress?)(Cache[hostname] ?? Cache6[hostname]),
                AddressFamily.InterNetwork => (IPAddress?)Cache[hostname],
                AddressFamily.InterNetworkV6 => (IPAddress?)Cache6[hostname],
                _ => throw new ArgumentOutOfRangeException()
            };

            if (cacheResult != null)
                return cacheResult;

            return await LookupNoCacheAsync(hostname, inet, timeout, dns);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Lookup hostname {Hostname} failed", hostname);
            return null;
        }
    }

    private static async Task<IPAddress?> LookupNoCacheAsync(string hostname, AddressFamily inet = AddressFamily.Unspecified, int timeout = 3000, string? dns = null)
    {
        IPAddress[] addresses;
        if (string.IsNullOrWhiteSpace(dns))
        {
            addresses = await Dns.GetHostAddressesAsync(hostname);
        }
        else
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(dns), 53);
            var options = new LookupClientOptions(endpoint)
            {
                Timeout = TimeSpan.FromMilliseconds(timeout),
                UseCache = false
            };
            var lookup = new LookupClient(options);

            var list = new List<IPAddress>();
            if (inet is AddressFamily.Unspecified or AddressFamily.InterNetwork)
            {
                var r = await lookup.QueryAsync(hostname, QueryType.A);
                list.AddRange(r.Answers.ARecords().Select(a => a.Address));
            }

            if (inet is AddressFamily.Unspecified or AddressFamily.InterNetworkV6)
            {
                var r = await lookup.QueryAsync(hostname, QueryType.AAAA);
                list.AddRange(r.Answers.AaaaRecords().Select(a => a.Address));
            }

            addresses = list.ToArray();
        }
        var result = addresses.FirstOrDefault(i => inet == AddressFamily.Unspecified || i.AddressFamily == inet);

        if (result == null) return null;

        switch (result.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                Cache.Add(hostname, result);
                break;
            case AddressFamily.InterNetworkV6:
                Cache6.Add(hostname, result);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    public static void ClearCache()
    {
        Cache.Clear();
        Cache6.Clear();
    }

    public static string AppendPort(string host, ushort port = 53)
    {
        if (!host.Contains(':'))
            return host + $":{port}";

        return host;
    }
}
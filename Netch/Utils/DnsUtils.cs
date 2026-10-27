using DnsClient;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Netch.Utils;

public static class DnsUtils
{
    /// <summary>
    ///     The system resolver has no timeout of its own. Keep an upper bound so a
    ///     broken network cannot leave callers waiting indefinitely, but stay
    ///     lenient enough for slow first lookups.
    /// </summary>
    private const int SystemResolverTimeout = 10000;

    /// <summary>
    ///     缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, IPAddress> Cache = new();
    private static readonly ConcurrentDictionary<string, IPAddress> Cache6 = new();

    /// <summary>
    ///     Lookups used to be serialized behind a single global lock, so one slow
    ///     hostname delayed every delay test, NAT test and start-up lookup behind
    ///     it. Lookups now run concurrently and identical requests share one query.
    /// </summary>
    private static readonly ConcurrentDictionary<(string Hostname, AddressFamily Inet, string? Dns), Lazy<Task<IPAddress?>>> InFlight = new();

    public static async Task<IPAddress?> LookupAsync(string hostname, AddressFamily inet = AddressFamily.Unspecified, int timeout = 3000, string? dns = null)
    {
        if (IPAddress.TryParse(hostname, out var ip))
        {
            // AddressFamily 过滤
            if (inet == AddressFamily.Unspecified || ip.AddressFamily == inet)
                return ip;
        }

        try
        {
            var cacheResult = GetCached(hostname, inet);
            if (cacheResult != null)
                return cacheResult;

            var key = (hostname, inet, string.IsNullOrWhiteSpace(dns) ? null : dns);
            var lookup = InFlight.GetOrAdd(key,
                k => new Lazy<Task<IPAddress?>>(() => LookupNoCacheAsync(k.Hostname, k.Inet, timeout, k.Dns)));

            try
            {
                return await lookup.Value;
            }
            finally
            {
                InFlight.TryRemove(new KeyValuePair<(string, AddressFamily, string?), Lazy<Task<IPAddress?>>>(key, lookup));
            }
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Lookup hostname {Hostname} failed", hostname);
            return null;
        }
    }

    private static IPAddress? GetCached(string hostname, AddressFamily inet)
    {
        IPAddress? address;
        switch (inet)
        {
            case AddressFamily.Unspecified:
                if (Cache.TryGetValue(hostname, out address) || Cache6.TryGetValue(hostname, out address))
                    return address;

                return null;
            case AddressFamily.InterNetwork:
                return Cache.TryGetValue(hostname, out address) ? address : null;
            case AddressFamily.InterNetworkV6:
                return Cache6.TryGetValue(hostname, out address) ? address : null;
            default:
                throw new ArgumentOutOfRangeException(nameof(inet));
        }
    }

    private static async Task<IPAddress?> LookupNoCacheAsync(string hostname, AddressFamily inet, int timeout, string? dns)
    {
        IPAddress[] addresses;
        if (dns == null)
        {
            using var cts = new CancellationTokenSource(Math.Max(timeout, SystemResolverTimeout));
            addresses = inet == AddressFamily.Unspecified
                ? await Dns.GetHostAddressesAsync(hostname, cts.Token)
                : await Dns.GetHostAddressesAsync(hostname, inet, cts.Token);
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

            // Query A and AAAA in parallel instead of paying two round trips.
            var ipv4Task = inet is AddressFamily.Unspecified or AddressFamily.InterNetwork
                ? lookup.QueryAsync(hostname, QueryType.A)
                : null;
            var ipv6Task = inet is AddressFamily.Unspecified or AddressFamily.InterNetworkV6
                ? lookup.QueryAsync(hostname, QueryType.AAAA)
                : null;

            var list = new List<IPAddress>();
            if (ipv4Task != null)
                list.AddRange((await ipv4Task).Answers.ARecords().Select(a => a.Address));

            if (ipv6Task != null)
                list.AddRange((await ipv6Task).Answers.AaaaRecords().Select(a => a.Address));

            addresses = list.ToArray();
        }

        var result = addresses.FirstOrDefault(i => inet == AddressFamily.Unspecified || i.AddressFamily == inet);

        if (result == null)
            return null;

        switch (result.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                Cache[hostname] = result;
                break;
            case AddressFamily.InterNetworkV6:
                Cache6[hostname] = result;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result));
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

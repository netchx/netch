using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DnsClient;
using Microsoft.VisualStudio.Threading;

namespace Netch.Utils;

public static class DnsUtils
{
    /// <summary>
    ///     The system resolver has no timeout of its own. Keep an upper bound so a
    ///     broken network cannot leave callers waiting indefinitely, but stay
    ///     lenient enough for slow first lookups.
    /// </summary>
    private const int SystemResolverTimeout = 10000;

    private const long CacheTtlMs = 5 * 60 * 1000;

    private readonly record struct CacheEntry(IPAddress[] Addresses, long ExpiresAtTicks);

    /// <summary>
    ///     TTL-bounded DNS caches split by address family.
    ///     Keep Unspecified in its own bucket so single-family lookups never poison it.
    /// </summary>
    private static readonly ConcurrentDictionary<string, CacheEntry> CacheAny = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache6 = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Lookups run concurrently and identical requests share one query.
    /// </summary>
    private static readonly ConcurrentDictionary<(string Hostname, AddressFamily Inet, string? Dns, long Generation), AsyncLazy<IPAddress[]>> InFlight = new();

    private static readonly ConcurrentDictionary<string, LookupClient> Clients = new(StringComparer.Ordinal);
    private static long _cacheGeneration;

    public static async Task<IPAddress?> LookupAsync(string hostname, AddressFamily inet = AddressFamily.Unspecified,
        int timeout = 3000, string? dns = null, bool useCache = true)
    {
        var addresses = await LookupAllAsync(hostname, inet, timeout, dns, useCache);
        return addresses.FirstOrDefault();
    }

    public static async Task<IPAddress[]> LookupAllAsync(
        string hostname,
        AddressFamily inet = AddressFamily.Unspecified,
        int timeout = 3000,
        string? dns = null,
        bool useCache = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return [];

        hostname = hostname.Trim();

        if (IPAddress.TryParse(hostname, out var ip))
        {
            if (inet == AddressFamily.Unspecified || ip.AddressFamily == inet)
                return [ip];

            return [];
        }

        ct.ThrowIfCancellationRequested();

        if (useCache && GetCached(hostname, inet) is { Length: > 0 } cached)
            return cached;

        var normalizedDns = string.IsNullOrWhiteSpace(dns) ? null : dns.Trim();
        var generation = Interlocked.Read(ref _cacheGeneration);
        var key = (hostname.ToLowerInvariant(), inet, normalizedDns, generation);
        var lookup = InFlight.GetOrAdd(key,
            k => new AsyncLazy<IPAddress[]>(async () =>
            {
                try
                {
                    return await LookupNoCacheAsync(k.Hostname, k.Inet, timeout, k.Dns);
                }
                finally
                {
                    InFlight.TryRemove(k, out _);
                }
            }, null));

        try
        {
            return await lookup.GetValueAsync().WaitAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Lookup hostname {Hostname} failed", hostname);
            return [];
        }
    }

    private static IPAddress[]? GetCached(string hostname, AddressFamily inet)
    {
        return inet switch
        {
            AddressFamily.Unspecified => TryGetValid(CacheAny, hostname),
            AddressFamily.InterNetwork => TryGetValid(Cache, hostname),
            AddressFamily.InterNetworkV6 => TryGetValid(Cache6, hostname),
            _ => throw new ArgumentOutOfRangeException(nameof(inet))
        };
    }

    private static IPAddress[]? TryGetValid(ConcurrentDictionary<string, CacheEntry> cache, string hostname)
    {
        if (!cache.TryGetValue(hostname, out var entry))
            return null;

        if (Environment.TickCount64 < entry.ExpiresAtTicks)
            return entry.Addresses;

        cache.TryRemove(new KeyValuePair<string, CacheEntry>(hostname, entry));
        return null;
    }

    private static async Task<IPAddress[]> LookupNoCacheAsync(string hostname, AddressFamily inet, int timeout, string? dns)
    {
        var generation = Interlocked.Read(ref _cacheGeneration);
        IPAddress[] addresses = [];

        IPEndPoint? endpoint = null;
        var hasCustomDns = dns != null && TryParseDnsEndPoint(dns, out endpoint) && endpoint != null;
        if (hasCustomDns && endpoint != null)
            addresses = await QueryCustomDnsAsync(hostname, inet, timeout, endpoint);

        if (addresses.Length == 0)
            addresses = await QuerySystemDnsAsync(hostname, inet, timeout, isFallback: hasCustomDns);

        var filtered = addresses
            .Where(i => inet == AddressFamily.Unspecified || i.AddressFamily == inet)
            .OrderBy(i => i.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
            .Distinct()
            .ToArray();

        if (filtered.Length == 0)
            return [];

        if (Interlocked.Read(ref _cacheGeneration) != generation)
            return filtered;

        var expiresAt = Environment.TickCount64 + CacheTtlMs;
        if (inet == AddressFamily.Unspecified)
        {
            CacheAny[hostname] = new CacheEntry(filtered, expiresAt);
            var v4 = filtered.Where(i => i.AddressFamily == AddressFamily.InterNetwork).ToArray();
            var v6 = filtered.Where(i => i.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
            if (v4.Length > 0)
                Cache[hostname] = new CacheEntry(v4, expiresAt);
            if (v6.Length > 0)
                Cache6[hostname] = new CacheEntry(v6, expiresAt);
        }
        else if (inet == AddressFamily.InterNetwork)
        {
            Cache[hostname] = new CacheEntry(filtered, expiresAt);
        }
        else if (inet == AddressFamily.InterNetworkV6)
        {
            Cache6[hostname] = new CacheEntry(filtered, expiresAt);
        }

        return filtered;
    }

    private static async Task<IPAddress[]> QueryCustomDnsAsync(string hostname, AddressFamily inet, int timeout, IPEndPoint endpoint)
    {
        var client = Clients.GetOrAdd(endpoint.ToString(), _ => new LookupClient(new LookupClientOptions(endpoint)
        {
            UseCache = false,
            Retries = 1,
            Timeout = TimeSpan.FromMilliseconds(3000)
        }));

        using var cts = new CancellationTokenSource(Math.Max(500, timeout));
        try
        {
            var ipv4Task = inet is AddressFamily.Unspecified or AddressFamily.InterNetwork
                ? QuerySingleRecordAsync(client, hostname, QueryType.A, cts.Token)
                : null;
            var ipv6Task = inet is AddressFamily.Unspecified or AddressFamily.InterNetworkV6
                ? QuerySingleRecordAsync(client, hostname, QueryType.AAAA, cts.Token)
                : null;

            var list = new List<IPAddress>();
            if (ipv4Task != null)
                list.AddRange(await ipv4Task);
            if (ipv6Task != null)
                list.AddRange(await ipv6Task);

            return list.ToArray();
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Custom DNS lookup for {Hostname} via {Dns} failed, falling back to system DNS", hostname, endpoint);
            return [];
        }
    }

    private static async Task<IEnumerable<IPAddress>> QuerySingleRecordAsync(LookupClient client, string hostname, QueryType type, CancellationToken ct)
    {
        try
        {
            var response = await client.QueryAsync(hostname, type, cancellationToken: ct);
            return type == QueryType.A
                ? response.Answers.ARecords().Select(a => a.Address)
                : response.Answers.AaaaRecords().Select(a => a.Address);
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static async Task<IPAddress[]> QuerySystemDnsAsync(string hostname, AddressFamily inet, int timeout, bool isFallback)
    {
        var bounded = isFallback ? Math.Clamp(timeout, 1500, 3000) : Math.Max(timeout, SystemResolverTimeout);
        using var cts = new CancellationTokenSource(bounded);
        try
        {
            return inet == AddressFamily.Unspecified
                ? await Dns.GetHostAddressesAsync(hostname, cts.Token)
                : await Dns.GetHostAddressesAsync(hostname, inet, cts.Token);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "System DNS lookup for {Hostname} failed", hostname);
            return [];
        }
    }

    private static bool TryParseDnsEndPoint(string dns, out IPEndPoint? endPoint)
    {
        if (IPEndPoint.TryParse(dns, out endPoint))
        {
            if (endPoint.Port == 0)
                endPoint = new IPEndPoint(endPoint.Address, 53);
            return true;
        }

        if (IPAddress.TryParse(dns, out var address))
        {
            endPoint = new IPEndPoint(address, 53);
            return true;
        }

        endPoint = null;
        return false;
    }

    public static void ClearCache()
    {
        Interlocked.Increment(ref _cacheGeneration);
        CacheAny.Clear();
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

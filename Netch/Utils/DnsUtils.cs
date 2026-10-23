using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Netch.Utils;

public static class DnsUtils
{
    private readonly record struct CacheEntry(IPAddress Address, DateTimeOffset ExpiresAt);
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);
    /// <summary>
    ///     缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache6 = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<IPAddress?> LookupAsync(string hostname, AddressFamily inet = AddressFamily.Unspecified, int timeout = 3000)
    {
        // Local proxy endpoints are normally IP literals. Avoid serializing them
        // behind a DNS request and do not let a temporary resolver failure break
        // UDP detection for an already resolved address.
        if (IPAddress.TryParse(hostname, out var literalAddress))
            return inet == AddressFamily.Unspecified || literalAddress.AddressFamily == inet
                ? literalAddress
                : null;

        try
        {
            var cacheResult = inet switch
            {
                AddressFamily.Unspecified => GetCachedAddress(Cache, hostname) ?? GetCachedAddress(Cache6, hostname),
                AddressFamily.InterNetwork => GetCachedAddress(Cache, hostname),
                AddressFamily.InterNetworkV6 => GetCachedAddress(Cache6, hostname),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (cacheResult != null)
                return cacheResult;

            return await LookupNoCacheAsync(hostname, inet, timeout);
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Lookup hostname {Hostname} failed", hostname);
            return null;
        }
    }

    private static async Task<IPAddress?> LookupNoCacheAsync(string hostname, AddressFamily inet = AddressFamily.Unspecified, int timeout = 3000)
    {
        // A timed-out DNS operation may finish later; do not dispose its Task.
        var addresses = await Dns.GetHostAddressesAsync(hostname)
            .WaitAsync(TimeSpan.FromMilliseconds(Math.Max(1, timeout)));

        var result = addresses.FirstOrDefault(i => inet == AddressFamily.Unspecified || inet == i.AddressFamily);
        if (result == null)
            return null;

        var entry = new CacheEntry(result, DateTimeOffset.UtcNow.Add(CacheLifetime));
        switch (result.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                Cache[hostname] = entry;
                break;
            case AddressFamily.InterNetworkV6:
                Cache6[hostname] = entry;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private static IPAddress? GetCachedAddress(ConcurrentDictionary<string, CacheEntry> cache, string hostname)
    {
        if (!cache.TryGetValue(hostname, out var entry))
            return null;
        if (entry.ExpiresAt > DateTimeOffset.UtcNow)
            return entry.Address;
        cache.TryRemove(hostname, out _);
        return null;
    }

    public static void ClearCache()
    {
        Cache.Clear();
        Cache6.Clear();
    }

    public static string AppendPort(string host, ushort port = 53)
    {
        var schemeEnd = host.IndexOf("://", StringComparison.Ordinal);
        var defaultPort = schemeEnd >= 0 && host[..schemeEnd].Equals("tls", StringComparison.OrdinalIgnoreCase) && port == 53
            ? (ushort)853
            : port;
        var authorityStart = schemeEnd < 0 ? 0 : schemeEnd + 3;
        var authorityEnd = host.IndexOfAny(new[] { '/', '?', '#' }, authorityStart);
        if (authorityEnd < 0)
            authorityEnd = host.Length;

        var authority = host[authorityStart..authorityEnd];
        if (authority.Length == 0)
            return host;

        if (IPAddress.TryParse(authority, out var address) && address.AddressFamily == AddressFamily.InterNetworkV6)
            authority = $"[{authority}]:{defaultPort}";
        else if (authority.StartsWith('['))
        {
            if (!authority.Contains("]:"))
                authority += $":{defaultPort}";
        }
        else if (!authority.Contains(':'))
            authority += $":{defaultPort}";

        return host[..authorityStart] + authority + host[authorityEnd..];
    }
}

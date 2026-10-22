using System.Collections;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.Threading;

namespace Netch.Utils;

public static class DnsUtils
{
    private static readonly AsyncSemaphore Lock = new(1);

    /// <summary>
    ///     缓存
    /// </summary>
    private static readonly Hashtable Cache = new();
    private static readonly Hashtable Cache6 = new();

    public static async Task<IPAddress?> LookupAsync(string hostname, AddressFamily inet = AddressFamily.Unspecified, int timeout = 3000)
    {
        using var _ = await Lock.EnterAsync();
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
        using var task = Dns.GetHostAddressesAsync(hostname);
        using var resTask = await Task.WhenAny(task, Task.Delay(timeout));

        if (resTask == task)
        {
            var addresses = await task;

            var result = addresses.FirstOrDefault(i => inet == AddressFamily.Unspecified || inet == i.AddressFamily);
            if (result == null)
                return null;

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

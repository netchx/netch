using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Netch.Models;
using Netch.Servers;
using Socks5.Models;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;

namespace Netch.Utils;

public static class Socks5ServerTestUtils
{
    /// <summary>
    ///     Upper bound for the whole RFC 5780 test (DNS, SOCKS handshake and all
    ///     STUN steps), so the status bar can never stay at "Testing" forever.
    /// </summary>
    private static readonly TimeSpan NatTestTimeout = TimeSpan.FromSeconds(20);

    private static readonly TimeSpan StunDnsTimeout = TimeSpan.FromSeconds(3);

    public static IReadOnlyList<string> GetStunPresetEndpoints()
    {
        var presets = LoadStunServers()
            .Select(server => EndpointPresetHelper.FormatStunEndpoint(server.Hostname, server.Port))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return presets.Length > 0 ? presets : Constants.DefaultStunServers;
    }

    public static IReadOnlyList<StunServer> LoadStunServers()
    {
        var servers = new List<StunServer>();
        try
        {
            foreach (var line in File.ReadLines(Path.Combine(Global.NetchDir, Constants.STUNServersFile)))
                if (StunServer.TryParse(line.Trim(), out var server) && server.Port != 0)
                    servers.Add(server);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Debug(e, "Could not load STUN server presets");
        }

        return servers;
    }

    public static async Task<NatTypeTestResult> DiscoveryNatTypeAsync(SocksServer socks5, CancellationToken ctx = default)
    {
        ctx.ThrowIfCancellationRequested();
        var settings = Global.Settings;
        if (settings.STUN_Server_Port is < 1 or > ushort.MaxValue ||
            !StunServer.TryParse(settings.STUN_Server?.Trim() ?? "", out var selected, (ushort)settings.STUN_Server_Port) ||
            selected.Port == 0)
            return new NatTypeTestResult { Result = "Wrong STUN Server!" };

        var servers = GetCandidates(selected, LoadStunServers());
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx);
        timeoutCts.CancelAfter(NatTestTimeout);
        var token = timeoutCts.Token;
        StunResult5389? best = null;

        // Start the selected STUN lookup alongside the SOCKS lookup.
        var stunLookup = ResolveStunAsync(servers[0], token);

        try
        {
            var socksAddress = await DnsUtils.LookupAsync(socks5.Address).WaitAsync(token);
            if (socksAddress == null)
                return new NatTypeTestResult { Result = "Lookup Server hostname failed" };

            var socksOption = new Socks5CreateOption
            {
                Address = socksAddress,
                Port = (ushort)socks5.Port,
                UsernamePassword = new UsernamePassword { UserName = socks5.Username, Password = socks5.Password }
            };
            var local = new IPEndPoint(socksAddress.AddressFamily == AddressFamily.InterNetworkV6
                ? IPAddress.IPv6Any : IPAddress.Any, 0);

            for (var index = 0; index < servers.Count; index++)
            {
                var server = servers[index];
                var ip = await (index == 0 ? stunLookup : ResolveStunAsync(server, token));
                if (ip == null)
                    continue;

                // Filtering must start on a fresh UDP association for each STUN server.
                using var client = new StunClient5389UDP(new IPEndPoint(ip, server.Port), local, new Socks5UdpProxy(local, socksOption));
                await client.ConnectProxyAsync(token);
                try
                {
                    await client.QueryAsync(token);
                    // Stun.Net also treats cancellation as a missing STUN response.
                    token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException) when (!ctx.IsCancellationRequested && client.State.PublicEndPoint != null)
                {
                    return ToResult(client.State, "Incomplete");
                }

                var state = client.State;
                var result = GetSimpleResult(state);
                Log.Debug("NAT test via {StunServer}: {Binding}, {Mapping}, {Filtering}",
                    server, state.BindingTestResult, state.MappingBehavior, state.FilteringBehavior);
                if (result is "1" or "2" or "3")
                    return ToResult(state, result);

                if (best == null ||
                    (state.PublicEndPoint != null && best.PublicEndPoint == null) ||
                    (best.BindingTestResult == BindingTestResult.Fail && state.BindingTestResult != BindingTestResult.Fail))
                    best = state;
            }
        }
        catch (OperationCanceledException) when (!ctx.IsCancellationRequested)
        {
            return best?.PublicEndPoint == null ? new NatTypeTestResult { Result = "Timeout" } : ToResult(best, GetSimpleResult(best));
        }

        return best == null ? new NatTypeTestResult { Result = "STUN hostname lookup failed." } : ToResult(best, GetSimpleResult(best));
    }

    private static IReadOnlyList<StunServer> GetCandidates(StunServer selected, IReadOnlyList<StunServer> presets)
    {
        static (string Host, ushort Port) Key(StunServer server) => (server.Hostname.ToUpperInvariant(), server.Port);
        var isPreset = presets.Any(server => Key(server) == Key(selected));
        var isPreviousDefault = selected.Port == StunServer.DefaultPort &&
            selected.Hostname.Equals("stun.syncthing.net", StringComparison.OrdinalIgnoreCase);
        return isPreset || isPreviousDefault ? presets.Prepend(selected).DistinctBy(Key).ToArray() : [selected];
    }

    private static async Task<IPAddress?> ResolveStunAsync(StunServer server, CancellationToken token)
    {
        try
        {
            return await DnsUtils.LookupAsync(server.Hostname).WaitAsync(StunDnsTimeout, token);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    private static NatTypeTestResult ToResult(StunResult5389 state, string result) => new()
    {
        Result = result,
        LocalEnd = state.LocalEndPoint?.ToString(),
        PublicEnd = state.PublicEndPoint?.ToString()
    };

    private static string GetSimpleResult(StunResult5389 res)
    {
        switch (res.BindingTestResult, res.MappingBehavior, res.FilteringBehavior)
        {
            case (BindingTestResult.Fail, _, _):
                return "NoUDP";
            case (not BindingTestResult.Success, _, _):
                return res.BindingTestResult.ToString();
            case (_, MappingBehavior.Direct or MappingBehavior.EndpointIndependent, FilteringBehavior.EndpointIndependent):
                return "1";
            case (_, MappingBehavior.Direct or MappingBehavior.EndpointIndependent,
                FilteringBehavior.AddressDependent or FilteringBehavior.AddressAndPortDependent):
                return "2";
            case (_, MappingBehavior.AddressDependent or MappingBehavior.AddressAndPortDependent, _):
                return "3";
            case (_, MappingBehavior.UnsupportedServer, _):
            case (_, _, FilteringBehavior.UnsupportedServer):
                return "UnsupportedServer";
            default:
                return "Incomplete";
        }
    }

    public static async Task<int?> HttpConnectAsync(SocksServer socks5, CancellationToken ctx)
    {
        if (socks5.Port is <= 0 or > ushort.MaxValue)
            return null;

        var timeoutMs = Math.Clamp(Global.Settings.DelayTestTimeout, Constants.MinDelayTestTimeout, Constants.MaxDelayTestTimeout);
        var address = await DnsUtils.LookupAsync(socks5.Address).WaitAsync(ctx);
        if (address == null)
            return null;

        if (address.Equals(IPAddress.Any))
            address = IPAddress.Loopback;
        else if (address.Equals(IPAddress.IPv6Any))
            address = IPAddress.IPv6Loopback;

        var testUri = Uri.TryCreate(Global.Settings.DelayTestUrl, UriKind.Absolute, out var parsed) &&
                      (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)
            ? parsed
            : new Uri(Constants.DefaultDelayTestUrl);

        var delay = await CoreDelayTester.ProbeOverSocks5Async(
            address.ToString(),
            (ushort)socks5.Port,
            socks5.Username,
            socks5.Password,
            testUri,
            timeoutMs,
            Global.Settings.DelayTestSteadySecondRequest,
            ctx);

        return DelayStatus.IsValid(delay) ? delay : null;
    }
}

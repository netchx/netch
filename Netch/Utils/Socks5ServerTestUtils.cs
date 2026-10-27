using System.Diagnostics;
using System.Net;
using Netch.Models;
using Netch.Servers;
using Socks5.Models;
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

    private static readonly TimeSpan CloseProxyTimeout = TimeSpan.FromSeconds(2);

    public static async Task<NatTypeTestResult> DiscoveryNatTypeAsync(SocksServer socks5, CancellationToken ctx = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx);
        timeoutCts.CancelAfter(NatTestTimeout);

        try
        {
            return await DiscoveryNatTypeCoreAsync(socks5, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ctx.IsCancellationRequested)
        {
            return new NatTypeTestResult { Result = "Timeout" };
        }
    }

    private static async Task<NatTypeTestResult> DiscoveryNatTypeCoreAsync(SocksServer socks5, CancellationToken ctx)
    {
        var stunServer = Global.Settings.STUN_Server;
        var port = (ushort)Global.Settings.STUN_Server_Port;
        var local = new IPEndPoint(IPAddress.Any, 0);

        // Resolve the SOCKS server and the STUN server in parallel.
        var socks5AddressTask = DnsUtils.LookupAsync(socks5.Address);
        var stunAddressTask = DnsUtils.LookupAsync(stunServer);
        await Task.WhenAll(socks5AddressTask, stunAddressTask).WaitAsync(ctx);

        var socks5Option = new Socks5CreateOption
        {
            Address = await socks5AddressTask,
            Port = (ushort)socks5.Port,
            UsernamePassword = new UsernamePassword
            {
                UserName = socks5.Username,
                Password = socks5.Password
            }
        };

        var ip = await stunAddressTask;
        if (ip == null)
        {
            return new NatTypeTestResult { Result = "Wrong STUN Server!" };
        }

        using IUdpProxy proxy = ProxyFactory.CreateProxy(ProxyType.Socks5, new IPEndPoint(IPAddress.Loopback, 0), socks5Option);
        using var client = new StunClient5389UDP(new IPEndPoint(ip, port), local, proxy);

        await client.ConnectProxyAsync(ctx);
        try
        {
            await client.QueryAsync(ctx);
        }
        finally
        {
            // Close even when the test was cancelled or timed out.
            using var closeCts = new CancellationTokenSource(CloseProxyTimeout);
            try
            {
                await client.CloseProxyAsync(closeCts.Token);
            }
            catch (Exception e)
            {
                Log.Debug(e, "Close STUN proxy failed");
            }
        }

        var res = client.State;
        var result = GetSimpleResult(res);

        return new NatTypeTestResult
        {
            Result = result,
            LocalEnd = res.LocalEndPoint?.ToString(),
            PublicEnd = res.PublicEndPoint?.ToString()
        };
    }

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
            case (_, MappingBehavior.Direct or MappingBehavior.EndpointIndependent, _):
                return "2";
            case (_, MappingBehavior.AddressDependent or MappingBehavior.AddressAndPortDependent, _):
                return "3";
            case (_, MappingBehavior.Fail, _):
                return MappingBehavior.Fail.ToString();
            default:
                return res.FilteringBehavior.ToString();
        }
    }

    public static async Task<int?> HttpConnectAsync(SocksServer socks5, CancellationToken ctx)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx);
        timeoutCts.CancelAfter(Math.Max(1000, Global.Settings.RequestTimeout));
        ctx = timeoutCts.Token;

        var socks5Option = new Socks5CreateOption
        {
            Address = await DnsUtils.LookupAsync(socks5.Address).WaitAsync(ctx),
            Port = (ushort)socks5.Port,
            UsernamePassword = new UsernamePassword
            {
                UserName = socks5.Username,
                Password = socks5.Password
            }
        };

        var stopwatch = Stopwatch.StartNew();

        var result = await Socks5.Utils.Socks5TestUtils.Socks5ConnectAsync(socks5Option, token: ctx);

        stopwatch.Stop();
        if (result)
            return Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);

        return null;
    }
}
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
    public static async Task<NatTypeTestResult> DiscoveryNatTypeAsync(Socks5Server socks5, CancellationToken ctx = default)
    {
        var stunServer = Global.Settings.STUN_Server;
        var port = (ushort)Global.Settings.STUN_Server_Port;
        var timeoutMilliseconds = Math.Clamp(Global.Settings.RequestTimeout, 8000, 30000);
        var dnsTimeoutMilliseconds = Math.Min(3000, timeoutMilliseconds / 4);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ctx);
        timeoutCts.CancelAfter(timeoutMilliseconds);
        var token = timeoutCts.Token;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Resolve the SOCKS endpoint and STUN server together. STUN prefers
            // IPv4 because the game redirector normally creates an IPv4 relay.
            var socksAddressTask = DnsUtils.LookupAsync(socks5.Hostname, timeout: dnsTimeoutMilliseconds);
            var stunAddressTask = LookupStunAddressAsync(stunServer, dnsTimeoutMilliseconds);
            await Task.WhenAll(socksAddressTask, stunAddressTask);
            token.ThrowIfCancellationRequested();

            var socksAddress = await socksAddressTask;
            if (socksAddress == null)
                return new NatTypeTestResult { Result = "Wrong Proxy Server!" };

            var stunAddress = await stunAddressTask;
            if (stunAddress == null)
                return new NatTypeTestResult { Result = "Wrong STUN Server!" };

            var localAddress = stunAddress.AddressFamily == AddressFamily.InterNetworkV6
                ? IPAddress.IPv6Any
                : IPAddress.Any;
            var local = new IPEndPoint(localAddress, 0);
            var socks5Option = new Socks5CreateOption
            {
                Address = socksAddress,
                Port = socks5.Port,
                UsernamePassword = new UsernamePassword
                {
                    UserName = socks5.Username,
                    Password = socks5.Password
                }
            };

            using IUdpProxy proxy = ProxyFactory.CreateProxy(ProxyType.Socks5, local, socks5Option);
            using var client = new StunClient5389UDP(new IPEndPoint(stunAddress, port), local, proxy)
            {
                // Bound each RFC 5780 request as well as the whole operation so a
                // dead UDP relay cannot leave the UI in "Testing" indefinitely.
                ReceiveTimeout = TimeSpan.FromMilliseconds(Math.Clamp(timeoutMilliseconds / 5, 1500, 3000))
            };

            var connected = false;
            try
            {
                await client.ConnectProxyAsync(token);
                connected = true;
                await client.QueryAsync(token);
            }
            finally
            {
                if (connected)
                {
                    try
                    {
                        using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        await client.CloseProxyAsync(closeCts.Token);
                    }
                    catch (Exception e)
                    {
                        Log.Verbose(e, "Close UDP detection proxy failed");
                    }
                }
            }

            var res = client.State;
            var result = GetSimpleResult(res);
            Log.Debug("UDP NAT detection finished in {Elapsed} ms: {Result}", stopwatch.ElapsedMilliseconds, result);

            return new NatTypeTestResult
            {
                Result = result,
                LocalEnd = res.LocalEndPoint?.ToString(),
                PublicEnd = res.PublicEndPoint?.ToString()
            };
        }
        catch (OperationCanceledException) when (!ctx.IsCancellationRequested)
        {
            Log.Warning("UDP NAT detection timed out after {Elapsed} ms (limit {Timeout} ms)",
                stopwatch.ElapsedMilliseconds, timeoutMilliseconds);
            return new NatTypeTestResult { Result = "Timeout" };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Warning(e, "UDP NAT detection failed after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
            return new NatTypeTestResult { Result = "Error" };
        }
    }

    private static async Task<IPAddress?> LookupStunAddressAsync(string hostname, int timeoutMilliseconds)
    {
        var address = await DnsUtils.LookupAsync(hostname, AddressFamily.InterNetwork, timeoutMilliseconds);
        return address ?? await DnsUtils.LookupAsync(hostname, AddressFamily.InterNetworkV6, timeoutMilliseconds);
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

    public static async Task<int?> HttpConnectAsync(Socks5Server socks5, CancellationToken ctx)
    {
        var socks5Option = new Socks5CreateOption
        {
            Address = await DnsUtils.LookupAsync(socks5.Hostname),
            Port = socks5.Port,
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

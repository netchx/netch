using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers;
using Socks5.Models;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;

namespace Netch.Utils
{
    public static class NatTypeTester
    {
        public static async Task<NatTypeTestResult> StartAsync(Socks5Server socks5, CancellationToken ctx = default)
        {
            var stunServer = Global.Settings.STUN_Server;
            var port = (ushort)Global.Settings.STUN_Server_Port;
            var local = new IPEndPoint(IPAddress.Any, 0);

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

            var ip = await DnsUtils.LookupAsync(stunServer) ?? throw new MessageException("Wrong STUN Server!");

            using IUdpProxy proxy = ProxyFactory.CreateProxy(ProxyType.Socks5, new IPEndPoint(IPAddress.Loopback, 0), socks5Option);
            using var client = new StunClient5389UDP(new IPEndPoint(ip, port), local, proxy);

            await client.ConnectProxyAsync(ctx);
            try
            {
                await client.QueryAsync(ctx);
            }
            finally
            {
                await client.CloseProxyAsync(ctx);
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
                case (not BindingTestResult.Success, _, _):
                    return res.BindingTestResult.ToString();
                case (_, MappingBehavior.Direct or MappingBehavior.EndpointIndependent, FilteringBehavior.EndpointIndependent):
                    return "1";
                case (_, MappingBehavior.Direct or MappingBehavior.EndpointIndependent, _):
                    return "2";
                case (_, MappingBehavior.AddressDependent or MappingBehavior.AddressAndPortDependent, _):
                    return "3";
                default:
                    return res.FilteringBehavior.ToString();
            }
        }
    }
}
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Netch.Utils;
using Vanara.PInvoke;

namespace Netch.Models
{
    public class OutboundAdapter : IAdapter
    {
        public OutboundAdapter()
        {
            // 寻找出口适配器
            if (IpHlpApi.GetBestRoute(BitConverter.ToUInt32(IPAddress.Parse("114.114.114.114").GetAddressBytes(), 0), 0, out var pRoute) != 0)
            {
                throw new MessageException("GetBestRoute 搜索失败");
            }

            NetworkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .First(ni => ni.Supports(NetworkInterfaceComponent.IPv4) &&
                             ni.GetIPProperties().GetIPv4Properties().Index == pRoute.dwForwardIfIndex);

            Address = new IPAddress(pRoute.dwForwardNextHop.S_addr);
            InterfaceIndex = (int) pRoute.dwForwardIfIndex;
            Gateway = new IPAddress(pRoute.dwForwardNextHop.S_un_b);

            Logging.Info($"出口 网关 地址：{Gateway}");
            Logging.Info($"出口适配器：{NetworkInterface.Name} {NetworkInterface.Id} {NetworkInterface.Description}, index: {InterfaceIndex}");
        }

        public IPAddress Address { get; }

        /// <summary>
        ///     索引
        /// </summary>
        public int InterfaceIndex { get; }

        /// <summary>
        ///     网关
        /// </summary>
        public IPAddress Gateway { get; }

        public NetworkInterface NetworkInterface { get; }
    }
}
using System;
using System.Net;
using System.Net.NetworkInformation;
using Netch.Interfaces;
using Netch.Utils;
using Vanara.PInvoke;

namespace Netch.Models.Adapter
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

            NetworkInterface = NetworkInterfaceUtils.Get((int)pRoute.dwForwardIfIndex);

            Address = new IPAddress(pRoute.dwForwardNextHop.S_addr);
            InterfaceIndex = (ulong)pRoute.dwForwardIfIndex;
            Gateway = new IPAddress(pRoute.dwForwardNextHop.S_un_b);

            Global.Logger.Info($"出口 网关 地址：{Gateway}");
            Global.Logger.Info($"出口适配器：{NetworkInterface.Name} {NetworkInterface.Id} {NetworkInterface.Description}, index: {InterfaceIndex}");
        }

        public IPAddress Address { get; }

        /// <summary>
        ///     索引
        /// </summary>
        public ulong InterfaceIndex { get; }

        /// <summary>
        ///     网关
        /// </summary>
        public IPAddress Gateway { get; }

        public NetworkInterface NetworkInterface { get; }
    }
}
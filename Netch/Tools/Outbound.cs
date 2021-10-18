using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Netch.Tools
{
    public class Outbound
    {
        /// <summary>
        ///     索引
        /// </summary>
        public uint Index;

        /// <summary>
        ///     适配器
        /// </summary>
        public NetworkInterface Interface;

        /// <summary>
        ///     地址
        /// </summary>
        public IPAddress Address;

        /// <summary>
        ///     掩码
        /// </summary>
        public IPAddress Netmask;

        /// <summary>
        ///     网关
        /// </summary>
        public IPAddress Gateway;

        /// <summary>
        ///     获取数据
        /// </summary>
        /// <returns></returns>
        public bool Get()
        {
            if (Vanara.PInvoke.IpHlpApi.GetBestRoute(BitConverter.ToUInt32(IPAddress.Parse("114.114.114.114").GetAddressBytes(), 0), 0, out var route) != Vanara.PInvoke.Win32Error.NO_ERROR)
                return false;

            this.Index = route.dwForwardIfIndex;
            this.Interface = NetworkInterface.GetAllNetworkInterfaces().First(nic =>
            {
                var ipp = nic.GetIPProperties();

                if (nic.Supports(NetworkInterfaceComponent.IPv4))
                {
                    return ipp.GetIPv4Properties().Index == this.Index;
                }

                return false;
            });

            if (this.Interface == null)
                return false;

            var addr = this.Interface.GetIPProperties().UnicastAddresses.First(ipf =>
            {
                return ipf.Address.AddressFamily == AddressFamily.InterNetwork;
            });

            this.Address = addr.Address;
            this.Netmask = addr.IPv4Mask;
            this.Gateway = new IPAddress(route.dwForwardNextHop.S_un_b);
            return true;
        }
    }
}

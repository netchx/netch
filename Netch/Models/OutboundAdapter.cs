using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using Netch.Controllers;
using Netch.Utils;
using Vanara.PInvoke;

namespace Netch.Models
{
    public class OutboundAdapter : IAdapter
    {
        public OutboundAdapter(bool logging = true)
        {
            // 寻找出口适配器
            if (IpHlpApi.GetBestRoute(BitConverter.ToUInt32(IPAddress.Parse("114.114.114.114").GetAddressBytes(), 0), 0, out var pRoute) != 0)
            {
                throw new MessageException("GetBestRoute 搜索失败");
            }

            NetworkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .First(ni => ni.Supports(NetworkInterfaceComponent.IPv4) &&
                             ni.GetIPProperties().GetIPv4Properties().Index == pRoute.dwForwardIfIndex);

            Index = (int) pRoute.dwForwardIfIndex;
            Gateway = new IPAddress(pRoute.dwForwardNextHop.S_un_b);
            _parametersRegistry =
                Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{NetworkInterface.Id}", true)!;

            if (logging)
            {
                Logging.Info($"出口 网关 地址：{Gateway}");
                Logging.Info($"出口适配器：{NetworkInterface.Name} {NetworkInterface.Id} {NetworkInterface.Description}, index: {Index}");
            }
        }

        /// <summary>
        ///     索引
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     网关
        /// </summary>
        public IPAddress Gateway { get; }

        public NetworkInterface NetworkInterface { get; }


        public string DNS
        {
            get
            {
                try
                {
                    return (string) _parametersRegistry.GetValue("NameServer");
                }
                catch
                {
                    return string.Empty;
                }
            }
            set => _parametersRegistry.SetValue("NameServer", value, RegistryValueKind.String);
        }

        private readonly RegistryKey _parametersRegistry;
    }
}
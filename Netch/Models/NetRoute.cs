using System;
using System.Net;
using Vanara.PInvoke;

namespace Netch.Models
{
    public struct NetRoute
    {
        public static NetRoute TemplateBuilder(IPAddress gateway, int interfaceIndex, int metric = 0)
        {
            return new()
            {
                Gateway = gateway,
                InterfaceIndex = interfaceIndex,
                Metric = metric
            };
        }

        public static NetRoute GetBestRouteTemplate(out IPAddress address)
        {
            if (IpHlpApi.GetBestRoute(BitConverter.ToUInt32(IPAddress.Parse("114.114.114.114").GetAddressBytes(), 0), 0, out var route) != 0)
                throw new MessageException("GetBestRoute 搜索失败");

            address = new IPAddress(route.dwForwardNextHop.S_addr);
            var gateway = new IPAddress(route.dwForwardNextHop.S_un_b);
            return TemplateBuilder(gateway, (int)route.dwForwardIfIndex);
        }

        public int InterfaceIndex;

        public IPAddress Gateway;

        public IPAddress Network;

        public byte Cidr;

        public int Metric;

        public NetRoute FillTemplate(string network, byte cidr, int? metric = null)
        {
            return FillTemplate(IPAddress.Parse(network), cidr, metric);
        }

        public NetRoute FillTemplate(IPAddress network, byte cidr, int? metric = null)
        {
            var o = (NetRoute)MemberwiseClone();
            o.Network = network;
            o.Cidr = cidr;
            if (metric != null)
                o.Metric = (int)metric;

            return o;
        }
    }
}
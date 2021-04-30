using System.Net;
using System.Net.NetworkInformation;
using Netch.Interfaces;
using Netch.Interops;
using Netch.Utils;

namespace Netch.Models.Adapter
{
    public class TunAdapter : IAdapter
    {
        public TunAdapter()
        {
            
            InterfaceIndex = RouteHelper.ConvertLuidToIndex(tun2socks.tun_luid());
            NetworkInterface = NetworkInterfaceUtils.Get((int)InterfaceIndex);
            Gateway = IPAddress.Parse(Global.Settings.TUNTAP.Gateway);

            Global.Logger.Info($"WinTUN 适配器：{NetworkInterface.Name} {NetworkInterface.Id} {NetworkInterface.Description}, index: {InterfaceIndex}");
        }


        public ulong InterfaceIndex { get; }

        public IPAddress Gateway { get; }

        public NetworkInterface NetworkInterface { get; }
    }
}

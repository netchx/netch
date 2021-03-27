using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Netch.Interops;
using Netch.Utils;

namespace Netch.Models
{
    public class TunAdapter : IAdapter
    {
        public TunAdapter()
        {
            InterfaceIndex = (int) NativeMethods.ConvertLuidToIndex(TUNInterop.tun_luid());
            NetworkInterface = NetworkInterface.GetAllNetworkInterfaces().First(i => i.GetIPProperties().GetIPv4Properties().Index == InterfaceIndex);
            Gateway = IPAddress.Parse(Global.Settings.TUNTAP.Gateway);

            Logging.Info($"WinTUN 适配器：{NetworkInterface.Name} {NetworkInterface.Id} {NetworkInterface.Description}, index: {InterfaceIndex}");
        }


        public int InterfaceIndex { get; }

        public IPAddress Gateway { get; }

        public NetworkInterface NetworkInterface { get; }
    }
}
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Netch.Controllers;
using Netch.Interops;
using Netch.Utils;

namespace Netch.Models
{
    public class TunAdapter : IAdapter
    {
        private const string ComponentIdWintun = "wintun";

        public TunAdapter()
        {
            AdapterId = AdapterUtils.GetAdapterId(ComponentIdWintun) ?? throw new Exception("wintun adapter not found");
            NetworkInterface = NetworkInterface.GetAllNetworkInterfaces().First(i => i.Id == AdapterId);
            InterfaceIndex = NetworkInterface.GetIPProperties().GetIPv4Properties().Index;
            Gateway = IPAddress.Parse(Global.Settings.TUNTAP.Gateway);

            Logging.Info($"WinTUN 适配器：{NetworkInterface.Name} {NetworkInterface.Id} {NetworkInterface.Description}, index: {InterfaceIndex}");
        }

        public string AdapterId { get; set; }

        public int InterfaceIndex { get; }

        public IPAddress Gateway { get; }

        public NetworkInterface NetworkInterface { get; }
    }
}
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Netch.Controllers;
using Netch.Utils;

namespace Netch.Models
{
    public class TapAdapter : IAdapter
    {
        public TapAdapter()
        {
            Index = -1;
            ComponentID = TUNTAP.GetComponentID() ?? throw new MessageException("TAP 适配器未安装");

            // 根据 ComponentID 寻找 Tap适配器
            NetworkInterface = NetworkInterface.GetAllNetworkInterfaces().First(i => i.Id == ComponentID);
            Index = NetworkInterface.GetIPProperties().GetIPv4Properties().Index;
            Logging.Info($"TAP 适配器：{NetworkInterface.Name} {NetworkInterface.Id} {NetworkInterface.Description}, index: {Index}");
        }

        public string ComponentID { get; }

        public int Index { get; }

        public IPAddress Gateway => IPAddress.Parse(Global.Settings.TUNTAP.Gateway);

        public NetworkInterface NetworkInterface { get; }
    }
}
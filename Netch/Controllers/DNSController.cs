using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Netch.Controllers
{
    public class DNSController : IController
    {

        public string Name { get; } = "DNS Service";

        /// <summary>
        ///     启动DNS服务
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (!aiodns_dial(Encoding.UTF8.GetBytes(Path.GetFullPath(Global.Settings.AioDNS.RulePath)),
                Encoding.UTF8.GetBytes($"{Global.Settings.AioDNS.ChinaDNS}:53"),
                Encoding.UTF8.GetBytes($"{Global.Settings.AioDNS.OtherDNS}:53"))
            )
                return false;
            return
                aiodns_init();
        }

        public void Stop()
        {
            aiodns_free();
        }

        #region NativeMethods

        [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aiodns_dial(byte[] chinacon, byte[] chinadns, byte[] otherdns);

        [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aiodns_init();

        [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern void aiodns_free();

        #endregion
    }
}
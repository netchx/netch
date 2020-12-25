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
            aiodns_dial((int) NameList.TYPE_REST, null);
            aiodns_dial((int) NameList.TYPE_ADDR, Encoding.UTF8.GetBytes($"{Global.Settings.LocalAddress}:53"));
            aiodns_dial((int) NameList.TYPE_LIST, Encoding.UTF8.GetBytes(Path.GetFullPath(Global.Settings.AioDNS.RulePath)));
            aiodns_dial((int) NameList.TYPE_CDNS, Encoding.UTF8.GetBytes($"{Global.Settings.AioDNS.ChinaDNS}:53"));
            aiodns_dial((int) NameList.TYPE_ODNS, Encoding.UTF8.GetBytes($"{Global.Settings.AioDNS.OtherDNS}:53"));
            aiodns_dial((int) NameList.TYPE_METH, Encoding.UTF8.GetBytes(Global.Settings.AioDNS.Protocol));

            return aiodns_init();
        }

        public void Stop()
        {
            aiodns_free();
        }

        #region NativeMethods

        [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aiodns_dial(int name, byte[] value);

        [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aiodns_init();

        [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern void aiodns_free();

        enum NameList : int
        {
            TYPE_REST,
            TYPE_ADDR,
            TYPE_LIST,
            TYPE_CDNS,
            TYPE_ODNS,
            TYPE_METH
        }

        #endregion
    }
}
using System.Runtime.InteropServices;

namespace Netch.Controllers.Other.DNS
{
    public class AioDNSController : Interface.IController
    {
        private enum NameList : int
        {
            TYPE_REST,
            TYPE_ADDR,
            TYPE_LIST,
            TYPE_CDNS,
            TYPE_ODNS
        }

        private static class Methods
        {
            [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aiodns_dial(NameList name, string value);

            [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aiodns_init();

            [DllImport("aiodns.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern void aiodns_free();
        }

        public bool Create(Models.Server.Server s, Models.Mode.Mode m)
        {
            Methods.aiodns_dial(NameList.TYPE_REST, "");
            Methods.aiodns_dial(NameList.TYPE_ADDR, ":53");
            Methods.aiodns_dial(NameList.TYPE_LIST, "Bin\\aiodns.conf");
            Methods.aiodns_dial(NameList.TYPE_CDNS, Global.Config.AioDNS.ChinaDNS);
            Methods.aiodns_dial(NameList.TYPE_ODNS, Global.Config.AioDNS.OtherDNS);

            return Methods.aiodns_init();
        }

        public bool Delete()
        {
            Methods.aiodns_free();

            return true;
        }
    }
}

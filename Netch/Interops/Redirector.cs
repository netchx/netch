using System.Runtime.InteropServices;

namespace Netch.Interops
{
    public static class Redirector
    {
        public enum NameList
        {
            TYPE_FILTERLOOPBACK,
            TYPE_FILTERICMP,
            TYPE_FILTERTCP,
            TYPE_FILTERUDP,

            TYPE_CLRNAME,
            TYPE_ADDNAME,
            TYPE_BYPNAME,

            TYPE_DNSHOST,

            TYPE_TCPLISN,
            TYPE_TCPTYPE,
            TYPE_TCPHOST,
            TYPE_TCPUSER,
            TYPE_TCPPASS,
            TYPE_TCPMETH,
            TYPE_TCPPROT,
            TYPE_TCPPRPA,
            TYPE_TCPOBFS,
            TYPE_TCPOBPA,

            TYPE_UDPLISN,
            TYPE_UDPTYPE,
            TYPE_UDPHOST,
            TYPE_UDPUSER,
            TYPE_UDPPASS,
            TYPE_UDPMETH,
            TYPE_UDPPROT,
            TYPE_UDPPRPA,
            TYPE_UDPOBFS,
            TYPE_UDPOBPA
        }

        public static bool Dial(NameList name, string value)
        {
            Global.Logger.Debug($"Dial {name} {value}");
            return aio_dial(name, value);
        }

        public static bool Init()
        {
            return aio_init();
        }

        public static bool Free()
        {
            return aio_free();
        }

        public const int UdpNameListOffset = (int)NameList.TYPE_UDPLISN - (int)NameList.TYPE_TCPLISN;

        private const string Redirector_bin = "Redirector.bin";

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_dial(NameList name, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_init();

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_free();

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong aio_getUP();

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong aio_getDL();
    }
}
using System.Runtime.InteropServices;
using System.Text;
using Netch.Utils;

namespace Netch.Controllers
{
    public static class RedirectorInterop
    {
        public enum NameList
        {
            //bool
            TYPE_FILTERLOOPBACK,
            TYPE_FILTERTCP,
            TYPE_FILTERUDP,
            TYPE_FILTERIP,
            TYPE_FILTERCHILDPROC, //子进程捕获

            TYPE_TCPLISN,
            TYPE_TCPTYPE,
            TYPE_TCPHOST,
            TYPE_TCPUSER,
            TYPE_TCPPASS,
            TYPE_TCPMETH,

            TYPE_UDPTYPE,
            TYPE_UDPHOST,
            TYPE_UDPUSER,
            TYPE_UDPPASS,
            TYPE_UDPMETH,

            TYPE_ADDNAME,
            TYPE_ADDFIP,

            TYPE_BYPNAME,

            TYPE_CLRNAME,
            TYPE_CLRFIP,

            //str addr x.x.x.x only ipv4
            TYPE_REDIRCTOR_DNS,
            TYPE_REDIRCTOR_ICMP
        }

        public static bool Dial(NameList name, string value)
        {
            Logging.Debug($"Dial {name} {value}");
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

        public const int UdpNameListOffset = (int) NameList.TYPE_UDPTYPE - (int) NameList.TYPE_TCPTYPE;

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
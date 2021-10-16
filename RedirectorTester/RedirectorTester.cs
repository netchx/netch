using System;
using System.Runtime.InteropServices;

namespace RedirectorTester
{
    public class RedirectorTester
    {
        public enum NameList : int
        {
            AIO_FILTERLOOPBACK,
            AIO_FILTERINTRANET,
            AIO_FILTERPARENT,
            AIO_FILTERICMP,
            AIO_FILTERTCP,
            AIO_FILTERUDP,
            AIO_FILTERDNS,

            AIO_ICMPING,

            AIO_DNSHOST,
            AIO_DNSPORT,

            AIO_TGTHOST,
            AIO_TGTPORT,
            AIO_TGTUSER,
            AIO_TGTPASS,

            AIO_CLRNAME,
            AIO_ADDNAME,
            AIO_BYPNAME
        }

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aio_register([MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aio_unregister([MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aio_dial(NameList name, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool aio_init();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern void aio_free();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong aio_getUP();

        [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong aio_getDL();

        public static void Main(string[] args)
        {
            aio_dial(NameList.AIO_FILTERLOOPBACK, "false");
            aio_dial(NameList.AIO_FILTERINTRANET, "true");
            aio_dial(NameList.AIO_FILTERPARENT, "true");
            aio_dial(NameList.AIO_FILTERICMP, "true");
            aio_dial(NameList.AIO_FILTERTCP, "true");
            aio_dial(NameList.AIO_FILTERUDP, "true");
            aio_dial(NameList.AIO_FILTERDNS, "true");

            aio_dial(NameList.AIO_ICMPING, "10");

            aio_dial(NameList.AIO_DNSHOST, "1.1.1.1");
            aio_dial(NameList.AIO_DNSPORT, "53");

            aio_dial(NameList.AIO_TGTHOST, "127.0.0.1");
            aio_dial(NameList.AIO_TGTPORT, "1080");
            aio_dial(NameList.AIO_TGTUSER, "");
            aio_dial(NameList.AIO_TGTPASS, "");

            aio_dial(NameList.AIO_CLRNAME, "");
            aio_dial(NameList.AIO_ADDNAME, "Telegram");
            aio_dial(NameList.AIO_ADDNAME, "NatTypeTester");

            aio_init();
            Console.ReadLine();
            aio_free();
        }
    }
}

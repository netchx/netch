using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;

namespace Netch.Interops
{
    public static class Redirector
    {
        public enum NameList
        {
            AIO_FILTERLOOPBACK,
            AIO_FILTERINTRANET, // LAN
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

        public static bool Dial(NameList name, string value)
        {
            Log.Verbose($"[Redirector] Dial {name}: {value}");
            return aio_dial(name, value);
        }

        public static async Task<bool> InitAsync()
        {
            return await Task.Run(aio_init).ConfigureAwait(false);
        }

        public static async Task<bool> FreeAsync()
        {
            return await Task.Run(aio_free).ConfigureAwait(false);
        }

        private const string Redirector_bin = "Redirector.bin";

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_dial(NameList name, [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_init();

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool aio_free();

        /*
        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong aio_getUP();

        [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong aio_getDL();
        */
    }
}
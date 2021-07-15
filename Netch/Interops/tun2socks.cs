using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Netch.Interops
{
    public static class tun2socks
    {
        public enum NameList
        {
            TYPE_BYPBIND,
            TYPE_BYPLIST,
            TYPE_DNSADDR,
            TYPE_ADAPMTU,
            TYPE_TCPREST,
            TYPE_TCPTYPE,
            TYPE_TCPHOST,
            TYPE_TCPUSER,
            TYPE_TCPPASS,
            TYPE_TCPMETH,
            TYPE_TCPPROT,
            TYPE_TCPPRPA,
            TYPE_TCPOBFS,
            TYPE_TCPOBPA,
            TYPE_UDPREST,
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
            Log.Verbose( $"[tun2socks] Dial {name}: {value}");
            return tun_dial(name, Encoding.UTF8.GetBytes(value));
        }

        public static bool Init()
        {
            Log.Verbose("[tun2socks] init");
            return tun_init();
        }

        public static async Task<bool> FreeAsync()
        {
            return await Task.Run(tun_free).ConfigureAwait(false);
        }

        private const string tun2socks_bin = "tun2socks.bin";

        [DllImport(tun2socks_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool tun_dial(NameList name, byte[] value);

        [DllImport(tun2socks_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool tun_init();

        [DllImport(tun2socks_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool tun_free();

        [DllImport(tun2socks_bin, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong tun_luid();

        [DllImport(tun2socks_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong tun_getUP();

        [DllImport(tun2socks_bin, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong tun_getDL();
    }
}
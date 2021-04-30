using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Netch.Interops
{
    public static class RouteHelper
    {
        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong ConvertLuidToIndex(ulong id);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CreateIPv4(string address, string netmask, ulong index);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CreateUnicastIP(AddressFamily inet, string address, byte cidr, ulong index);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool RefreshIPTable(AddressFamily inet, ulong index);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CreateRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DeleteRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);
    }
}
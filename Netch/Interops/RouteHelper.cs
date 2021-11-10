using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.IpHelper;
using static Windows.Win32.PInvoke;

namespace Netch.Interops
{
    public static unsafe class RouteHelper
    {
        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong ConvertLuidToIndex(ulong id);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CreateIPv4(string address, string netmask, ulong index);

        public static bool CreateUnicastIP(AddressFamily inet, string address, byte cidr, ulong index)
        {
            MIB_UNICASTIPADDRESS_ROW addr;
            InitializeUnicastIpAddressEntry(&addr);

            addr.InterfaceIndex = (uint)index;
            addr.OnLinkPrefixLength = cidr;

            if (inet == AddressFamily.InterNetwork)
            {
                addr.Address.Ipv4.sin_family = (ushort)ADDRESS_FAMILY.AF_INET;
                if (inet_pton((int)inet, address, &addr.Address.Ipv4.sin_addr) == 0)
                    return false;
            }
            else if (inet == AddressFamily.InterNetworkV6)
            {
                addr.Address.Ipv6.sin6_family = (ushort)ADDRESS_FAMILY.AF_INET6;
                if (inet_pton((int)inet, address, &addr.Address.Ipv6.sin6_addr) == 0)
                    return false;
            }
            else
            {
                return false;
            }

            // Create a Handle to be notified of IP address changes
            HANDLE handle = default;
            using var obj = new Semaphore(1, 1);
            void Callback(void* context, MIB_UNICASTIPADDRESS_ROW* row, MIB_NOTIFICATION_TYPE type) => obj.Release(1);

            // Use NotifyUnicastIpAddressChange to determine when the address is ready
            obj.WaitOne();
            NotifyUnicastIpAddressChange((ushort)ADDRESS_FAMILY.AF_INET, Callback, null, new BOOLEAN(), ref handle);

            if (CreateUnicastIpAddressEntry(&addr) != 0)
            {
                // ignored return state because i feel great
                CancelMibChangeNotify2(handle);
                return false;
            }

            obj.WaitOne();

            return true;
        }

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool RefreshIPTable(AddressFamily inet, ulong index);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CreateRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DeleteRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);
    }
}
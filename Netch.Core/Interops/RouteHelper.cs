using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.WinSock;
using Windows.Win32.NetworkManagement.IpHelper;

namespace Netch.Interops;

public static unsafe class RouteHelper
{
    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ConvertLuidToIndex(ulong id);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CreateIPv4(string address, string netmask, ulong index);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CreateUnicastIP(AddressFamily inet, string address, byte cidr, ulong index);

    public static bool CreateUnicastIPCS(AddressFamily inet, string address, byte cidr, ulong index)
    {
        if (inet is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            return false;

        MIB_UNICASTIPADDRESS_ROW addr;
        PInvoke.InitializeUnicastIpAddressEntry(&addr);

        addr.InterfaceIndex = (uint)index;
        addr.OnLinkPrefixLength = cidr;
        var winFamily = inet == AddressFamily.InterNetwork ? ADDRESS_FAMILY.AF_INET : ADDRESS_FAMILY.AF_INET6;

#pragma warning disable CA1416
        if (inet == AddressFamily.InterNetwork)
        {
            addr.Address.Ipv4.sin_family = winFamily;
            if (PInvoke.inet_pton((int)inet, address, &addr.Address.Ipv4.sin_addr) == 0)
                return false;
        }
        else
        {
            addr.Address.Ipv6.sin6_family = winFamily;
            if (PInvoke.inet_pton((int)inet, address, &addr.Address.Ipv6.sin6_addr) == 0)
                return false;
        }
#pragma warning restore CA1416

        HANDLE handle = default;
        using var preferred = new Semaphore(0, 1);

        void Callback(void* context, MIB_UNICASTIPADDRESS_ROW* row, MIB_NOTIFICATION_TYPE type)
        {
            if (type == MIB_NOTIFICATION_TYPE.MibInitialNotification)
                return;

            var state = PInvoke.GetUnicastIpAddressEntry(row);
            if (state != WIN32_ERROR.NO_ERROR)
            {
                Log.Error("GetUnicastIpAddressEntry failed: {State}", state);
                return;
            }

            if (row->DadState == NL_DAD_STATE.IpDadStatePreferred)
            {
                try
                {
                    preferred.Release();
                }
                catch (Exception e)
                {
                    Log.Error(e, "semaphore disposed");
                }
            }
        }

        PInvoke.NotifyUnicastIpAddressChange(winFamily, Callback, null, new BOOLEAN(byte.MaxValue), ref handle);

        try
        {
            var state = PInvoke.CreateUnicastIpAddressEntry(&addr);
            if (state != WIN32_ERROR.NO_ERROR)
            {
                Log.Error("CreateUnicastIpAddressEntry failed: {State}", state);
                return false;
            }

            if (!preferred.WaitOne(TimeSpan.FromSeconds(10)))
            {
                Log.Error("Wait unicast IP usable timeout");
                return false;
            }

            return true;
        }
        finally
        {
            PInvoke.CancelMibChangeNotify2(handle);
        }
    }

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool RefreshIPTable(AddressFamily inet, ulong index);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CreateRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint CreateRouteStatus(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GetBestRouteTemplate(AddressFamily inet, string destination, StringBuilder gateway, uint gatewaySize, out uint index);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DeleteRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);
}

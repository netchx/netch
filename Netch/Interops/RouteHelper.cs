using System.Net.Sockets;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.WinSock;
using Windows.Win32.NetworkManagement.IpHelper;
using static Windows.Win32.PInvoke;

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

        // https://docs.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-createunicastipaddressentry#remarks

        HANDLE handle = default;
        using var obj = new Semaphore(0, 1);

        void Callback(void* context, MIB_UNICASTIPADDRESS_ROW* row, MIB_NOTIFICATION_TYPE type)
        {
            if (type != MIB_NOTIFICATION_TYPE.MibInitialNotification)
            {
                NTSTATUS state;
                if ((state = GetUnicastIpAddressEntry(row)) != 0)
                {
                    Log.Error("GetUnicastIpAddressEntry failed: {State}", state.Value);
                    return;
                }

                if (row -> DadState == NL_DAD_STATE.IpDadStatePreferred)
                {
                    try
                    {
                        obj.Release();
                    }
                    catch (Exception e)
                    {
                        // i don't trust win32 api
                        Log.Error(e, "semaphore disposed");
                    }
                }
            }
        }

        NotifyUnicastIpAddressChange((ushort)ADDRESS_FAMILY.AF_INET, Callback, null, new BOOLEAN(byte.MaxValue), ref handle);

        try
        {
            NTSTATUS state;
            if ((state = CreateUnicastIpAddressEntry(&addr)) != 0)
            {
                Log.Error("CreateUnicastIpAddressEntry failed: {State}", state.Value);
                return false;
            }

            if (!obj.WaitOne(TimeSpan.FromSeconds(10)))
            {
                Log.Error("Wait unicast IP usable timeout");
                return false;
            }

            return true;
        }
        finally
        {
            CancelMibChangeNotify2(handle);
        }
    }

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool RefreshIPTable(AddressFamily inet, ulong index);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CreateRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);

    [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DeleteRoute(AddressFamily inet, string address, byte cidr, string gateway, ulong index, int metric);
}
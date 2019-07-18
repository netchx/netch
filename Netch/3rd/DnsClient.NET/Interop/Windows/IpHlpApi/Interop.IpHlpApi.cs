using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        internal const uint ERROR_SUCCESS = 0;
        internal const uint ERROR_INVALID_FUNCTION = 1;
        internal const uint ERROR_NO_SUCH_DEVICE = 2;
        internal const uint ERROR_INVALID_DATA = 13;
        internal const uint ERROR_INVALID_PARAMETER = 87;
        internal const uint ERROR_BUFFER_OVERFLOW = 111;
        internal const uint ERROR_INSUFFICIENT_BUFFER = 122;
        internal const uint ERROR_NO_DATA = 232;
        internal const uint ERROR_IO_PENDING = 997;
        internal const uint ERROR_NOT_FOUND = 1168;

        [DllImport(Libraries.IpHlpApi)]
        internal extern static uint GetAdaptersAddresses(
            AddressFamily family,
            uint flags,
            IntPtr pReserved,
            IntPtr adapterAddresses,
            ref uint outBufLen);

        [DllImport(Libraries.IpHlpApi, ExactSpelling = true)]
        internal extern static uint GetNetworkParams(IntPtr pFixedInfo, ref uint pOutBufLen);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct FIXED_INFO
        {
            public const int MAX_HOSTNAME_LEN = 128;
            public const int MAX_DOMAIN_NAME_LEN = 128;
            public const int MAX_SCOPE_ID_LEN = 256;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_HOSTNAME_LEN + 4)]
            public string hostName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_DOMAIN_NAME_LEN + 4)]
            public string domainName;

            public IntPtr currentDnsServer; // IpAddressList*
            public IP_ADDR_STRING DnsServerList;
            public uint nodeType;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_SCOPE_ID_LEN + 4)]
            public string scopeId;

            public bool enableRouting;
            public bool enableProxy;
            public bool enableDns;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct IP_ADDR_STRING
        {
            public IntPtr Next; // struct _IpAddressList*

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpAddress;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpMask;

            public uint Context;
        }
    }
}
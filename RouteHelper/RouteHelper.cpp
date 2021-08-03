#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include <WinSock2.h>
#include <WS2tcpip.h>
#include <ws2ipdef.h>
#include <iphlpapi.h>
#include <netioapi.h>
#include <Windows.h>

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(hModule);
    UNREFERENCED_PARAMETER(lpReserved);
    UNREFERENCED_PARAMETER(ul_reason_for_call);

    return TRUE;
}

BOOL make(PMIB_IPFORWARD_ROW2 rule, USHORT inet, const char* address, UINT8 cidr, const char* gateway, ULONG index, ULONG metric)
{
    rule->InterfaceIndex = index;
    rule->DestinationPrefix.PrefixLength = cidr;

    if (AF_INET == inet)
    {
        rule->DestinationPrefix.Prefix.Ipv4.sin_family = inet;
        if (!inet_pton(inet, address, &rule->DestinationPrefix.Prefix.Ipv4.sin_addr))
        {
            return FALSE;
        }

        if (strlen(gateway))
        {
            rule->NextHop.Ipv4.sin_family = inet;
            if (!inet_pton(inet, gateway, &rule->NextHop.Ipv4.sin_addr))
            {
                return FALSE;
            }
        }
    }
    else if (AF_INET6 == inet)
    {
        rule->DestinationPrefix.Prefix.Ipv6.sin6_family = inet;
        if (!inet_pton(inet, address, &rule->DestinationPrefix.Prefix.Ipv6.sin6_addr))
        {
            return FALSE;
        }

        if (strlen(gateway))
        {
            rule->NextHop.Ipv6.sin6_family = inet;
            if (!inet_pton(inet, gateway, &rule->NextHop.Ipv6.sin6_addr))
            {
                return FALSE;
            }
        }
    }

    rule->ValidLifetime = 0xffffffff;
    rule->PreferredLifetime = 0xffffffff;
    rule->Metric = metric;
    rule->Protocol = MIB_IPPROTO_NETMGMT;
    return TRUE;
}

extern "C" {
    __declspec(dllexport) ULONG __cdecl ConvertLuidToIndex(ULONG64 id)
    {
        NET_LUID luid;
        luid.Value = id;

        NET_IFINDEX index = 0;
        if (NO_ERROR != ConvertInterfaceLuidToIndex(&luid, &index))
        {
            return 0;
        }

        return index;
    }

    __declspec(dllexport) BOOL __cdecl CreateIPv4(const char* address, const char* netmask, ULONG index)
    {
        ULONG addr = inet_addr(address);
        ULONG mask = inet_addr(netmask);
        ULONG ctx = 0;
        ULONG inst = 0;

        return (NO_ERROR == AddIPAddress(addr, mask, index, &ctx, &inst)) ? TRUE : FALSE;
    }

    __declspec(dllexport) BOOL __cdecl CreateUnicastIP(USHORT inet, const char* address, UINT8 cidr, ULONG index)
    {
        MIB_UNICASTIPADDRESS_ROW addr;
        InitializeUnicastIpAddressEntry(&addr);

        addr.InterfaceIndex = index;
        addr.OnLinkPrefixLength = cidr;

        if (AF_INET == inet)
        {
            addr.Address.Ipv4.sin_family = inet;
            if (!inet_pton(inet, address, &addr.Address.Ipv4.sin_addr))
            {
                return FALSE;
            }
        }
        else if (AF_INET6 == inet)
        {
            addr.Address.Ipv6.sin6_family = inet;
            if (!inet_pton(inet, address, &addr.Address.Ipv6.sin6_addr))
            {
                return FALSE;
            }
        }
        else
        {
            return FALSE;
        }

        return (NO_ERROR == CreateUnicastIpAddressEntry(&addr)) ? TRUE : FALSE;
    }

    __declspec(dllexport) BOOL __cdecl RefreshIPTable(USHORT inet, ULONG index)
    {
        if (NO_ERROR != FlushIpPathTable(inet))
        {
            return FALSE;
        }

        return (NO_ERROR == FlushIpNetTable2(inet, index)) ? TRUE : FALSE;
    }

    __declspec(dllexport) BOOL __cdecl CreateRoute(USHORT inet, const char* address, UINT8 cidr, const char* gateway, ULONG index, ULONG metric = 1)
    {
        MIB_IPFORWARD_ROW2 rule;
        InitializeIpForwardEntry(&rule);

        if (!make(&rule, inet, address, cidr, gateway, index, metric))
        {
            return FALSE;
        }

        return (NO_ERROR == CreateIpForwardEntry2(&rule)) ? TRUE : FALSE;
    }

    __declspec(dllexport) BOOL __cdecl DeleteRoute(USHORT inet, const char* address, UINT8 cidr, const char* gateway, ULONG index, ULONG metric = 1)
    {
        MIB_IPFORWARD_ROW2 rule;
        InitializeIpForwardEntry(&rule);

        if (!make(&rule, inet, address, cidr, gateway, index, metric))
        {
            return FALSE;
        }

        return (NO_ERROR == DeleteIpForwardEntry2(&rule)) ? TRUE : FALSE;
    }
}

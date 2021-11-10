#include "Based.h"
#include "WaitGroup.h"

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(hModule);
    UNREFERENCED_PARAMETER(lpReserved);
    UNREFERENCED_PARAMETER(ul_reason_for_call);

    return TRUE;
}

WaitGroup wg;
void UnicastIPChangeCallback(PVOID ctx, PMIB_UNICASTIPADDRESS_ROW row, MIB_NOTIFICATION_TYPE type)
{
    UNREFERENCED_PARAMETER(ctx);
    UNREFERENCED_PARAMETER(row);
    UNREFERENCED_PARAMETER(type);

    wg.Done();
}

bool make(PMIB_IPFORWARD_ROW2 rule, USHORT inet, const char* address, UINT8 cidr, const char* gateway, ULONG index, ULONG metric)
{
    rule->InterfaceIndex = index;
    rule->DestinationPrefix.PrefixLength = cidr;

    if (AF_INET == inet)
    {
        rule->DestinationPrefix.Prefix.Ipv4.sin_family = inet;
        if (inet_pton(inet, address, &rule->DestinationPrefix.Prefix.Ipv4.sin_addr) != 1)
        {
            return false;
        }

        if (strlen(gateway))
        {
            rule->NextHop.Ipv4.sin_family = inet;
            if (inet_pton(inet, gateway, &rule->NextHop.Ipv4.sin_addr) != 1)
            {
                return false;
            }
        }
    }
    else if (AF_INET6 == inet)
    {
        rule->DestinationPrefix.Prefix.Ipv6.sin6_family = inet;
        if (inet_pton(inet, address, &rule->DestinationPrefix.Prefix.Ipv6.sin6_addr) != 1)
        {
            return false;
        }

        if (strlen(gateway))
        {
            rule->NextHop.Ipv6.sin6_family = inet;
            if (inet_pton(inet, gateway, &rule->NextHop.Ipv6.sin6_addr) != 1)
            {
                return false;
            }
        }
    }

    rule->ValidLifetime = 0xffffffff;
    rule->PreferredLifetime = 0xffffffff;
    rule->Metric = metric;
    rule->Protocol = MIB_IPPROTO_NETMGMT;
    return true;
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

    __declspec(dllexport) void __cdecl WaitForUnicastIP()
    {
        wg.Add(1);

        HANDLE hCallback = NULL;
        NotifyUnicastIpAddressChange(AF_INET, UnicastIPChangeCallback, NULL, FALSE, &hCallback);

        wg.Wait();

        CancelMibChangeNotify2(hCallback);
    }

    __declspec(dllexport) BOOL __cdecl CreateIPv4(const char* address, const char* netmask, ULONG index)
    {
        ULONG addr = 0;
        if (inet_pton(AF_INET, address, &addr) != 1)
        {
            return FALSE;
        }

        ULONG mask = 0;
        if (inet_pton(AF_INET, netmask, &mask) != 1)
        {
            return FALSE;
        }

        ULONG context = 0;
        ULONG instance = 0;
        return (NO_ERROR == AddIPAddress(addr, mask, index, &context, &instance)) ? TRUE : FALSE;
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

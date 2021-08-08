#include "Based.h"
#include "EventHandler.h"
#include "IPEventHandler.h"
#include "Utils.h"

extern BOOL filterLoopback;
extern BOOL filterICMP;
extern BOOL filterTCP;
extern BOOL filterUDP;
extern USHORT tcpPort;
extern USHORT udpPort;
extern vector<wstring> bypassList;
extern vector<wstring> handleList;

NF_EventHandler EventHandler = {
	threadStart,
	threadEnd,
	tcpConnectRequest,
	tcpConnected,
	tcpClosed,
	tcpReceive,
	tcpSend,
	tcpCanReceive,
	tcpCanSend,
	udpCreated,
	udpConnectRequest,
	udpClosed,
	udpReceive,
	udpSend,
	udpCanReceive,
	udpCanSend
};

NF_IPEventHandler IPEventHandler = {
	ipReceive,
	ipSend
};

BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved)
{
	UNREFERENCED_PARAMETER(hModule);
	UNREFERENCED_PARAMETER(dwReason);
	UNREFERENCED_PARAMETER(lpReserved);

	return TRUE;
}

extern "C" {
	__declspec(dllexport) BOOL __cdecl aio_dial(INT name, LPWSTR value)
	{
		switch (name)
		{
		case AIO_FILTERLOOPBACK:
			filterLoopback = (wstring(value).find(L"false") == string::npos);
			break;
		case AIO_FILTERICMP:
			filterICMP = (wstring(value).find(L"false") == string::npos);
			break;
		case AIO_FILTERTCP:
			filterTCP = (wstring(value).find(L"false") == string::npos);
			break;
		case AIO_FILTERUDP:
			filterUDP = (wstring(value).find(L"false") == string::npos);
			break;
		case AIO_CLRNAME:
			bypassList.clear();
			handleList.clear();
			break;
		case AIO_BYPNAME:
			try
			{
				std::wregex checker(value);
			}
			catch (regex_error) {
				return FALSE;
			}

			bypassList.emplace_back(value);
			break;
		case AIO_ADDNAME:
			try
			{
				std::wregex checker(value);
			}
			catch (regex_error) {
				return FALSE;
			}

			handleList.emplace_back(value);
			break;
		case AIO_TCPPORT:
			tcpPort = (USHORT)atoi(ws2s(value).c_str());
			break;
		case AIO_UDPPORT:
			udpPort = (USHORT)atoi(ws2s(value).c_str());
			break;
		default:
			return FALSE;
		}

		return TRUE;
	}

	__declspec(dllexport) BOOL __cdecl aio_init()
	{
		WSADATA data;
		if (WSAStartup(MAKEWORD(2, 2), &data) != NO_ERROR)
		{
			puts("[Redirector][aio_init] WSAStartup != NO_ERROR");
			return FALSE;
		}

		nf_adjustProcessPriviledges();
		if (!eh_init())
		{
			puts("[Redirector][aio_init] !eh_init");
			return FALSE;
		}

		if (nf_init("netfilter2", &EventHandler) != NF_STATUS_SUCCESS)
		{
			puts("[Redirector][aio_init] nf_init != NF_STATUS_SUCCESS");
			return FALSE;
		}

		NF_RULE rule;
		if (!filterLoopback)
		{
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "127.0.0.1", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.0.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.remoteIpAddress[15] = 1;
			memset(rule.remoteIpAddressMask, 0xff, sizeof(rule.remoteIpAddressMask));
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);
		}

		if (filterICMP)
		{
			nf_setIPEventHandler(&IPEventHandler);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			rule.protocol = IPPROTO_ICMP;
			rule.direction = NF_D_OUT;
			rule.filteringFlag = NF_FILTER_AS_IP_PACKETS;
			nf_addRule(&rule, FALSE);
		}

		if (filterTCP)
		{
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			rule.protocol = IPPROTO_TCP;
			rule.direction = NF_D_OUT;
			rule.filteringFlag = NF_INDICATE_CONNECT_REQUESTS;
			nf_addRule(&rule, FALSE);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.protocol = IPPROTO_TCP;
			rule.direction = NF_D_OUT;
			rule.filteringFlag = NF_INDICATE_CONNECT_REQUESTS;
			nf_addRule(&rule, FALSE);
		}

		if (filterUDP)
		{
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			rule.protocol = IPPROTO_UDP;
			rule.filteringFlag = NF_FILTER;
			nf_addRule(&rule, FALSE);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.protocol = IPPROTO_UDP;
			rule.filteringFlag = NF_FILTER;
			nf_addRule(&rule, FALSE);
		}

		return TRUE;
	}

	__declspec(dllexport) void __cdecl aio_free()
	{
		nf_deleteRules();
		nf_free();
		eh_free();

		WSACleanup();
		return;
	}
}

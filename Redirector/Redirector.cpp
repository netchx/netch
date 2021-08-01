#include "Based.h"

#include "Utils.h"
#include "EventHandler.h"
#include "IPEventHandler.h"

typedef enum _AIO_TYPE {
	AIO_FILTERLOOP,
	AIO_FILTERICMP,
	AIO_FILTERTCP,
	AIO_FILTERUDP,

	AIO_CLRNAME,
	AIO_ADDNAME,
	AIO_BYPNAME,

	AIO_DNSHOOK,
	AIO_DNSHOST,
	AIO_DNSPORT,

	AIO_TCPPORT,
	AIO_UDPPORT
} AIO_TYPE;

extern BOOL filterLoop;
extern BOOL filterICMP;
extern BOOL filterTCP;
extern BOOL filterUDP;
extern BOOL dnsHook;
extern string dnsHost;
extern USHORT dnsPort;
extern USHORT tcpLisn;
extern USHORT udpLisn;
extern vector<wstring> handleList;
extern vector<wstring> bypassList;

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

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	UNREFERENCED_PARAMETER(hModule);
	UNREFERENCED_PARAMETER(ul_reason_for_call);
	UNREFERENCED_PARAMETER(lpReserved);

	return TRUE;
}

extern "C" {
	__declspec(dllexport) BOOL __cdecl aio_dial(INT name, LPWSTR value)
	{
		UNREFERENCED_PARAMETER(name);
		UNREFERENCED_PARAMETER(value);

		switch (name)
		{
		case AIO_FILTERLOOP:
			break;
		case AIO_FILTERICMP:
			break;
		case AIO_FILTERTCP:
			break;
		case AIO_FILTERUDP:
			break;
		case AIO_DNSHOOK:
			break;
		case AIO_DNSHOST:
			break;
		case AIO_DNSPORT:
			break;
		case AIO_TCPPORT:
			break;
		case AIO_UDPPORT:
			break;
		default:
			return FALSE;
		}

		return TRUE;
	}

	__declspec(dllexport) BOOL __cdecl aio_init()
	{
		{
			WSADATA data;
			UNREFERENCED_PARAMETER(WSAStartup(MAKEWORD(2, 2), &data));
		}

		nf_adjustProcessPriviledges();

		return FALSE;
	}

	__declspec(dllexport) void __cdecl aio_free()
	{
		nf_deleteRules();
		nf_free();

		eh_free();

		UNREFERENCED_PARAMETER(WSACleanup());
		return;
	}
}

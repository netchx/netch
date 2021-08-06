#include "Based.h"
#include "EventHandler.h"
#include "IPEventHandler.h"

extern BOOL filterLoopback;
extern BOOL filterICMP;
extern BOOL filterTCP;
extern BOOL filterUDP;
extern USHORT tcpPort;
extern USHORT udpPort;
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
		UNREFERENCED_PARAMETER(name);
		UNREFERENCED_PARAMETER(value);

		switch (name)
		{
		case AIO_FILTERLOOPBACK:
			break;
		case AIO_FILTERICMP:
			break;
		case AIO_FILTERTCP:
			break;
		case AIO_FILTERUDP:
			break;
		case AIO_CLRNAME:
			break;
		case AIO_BYPNAME:
			break;
		case AIO_ADDNAME:
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

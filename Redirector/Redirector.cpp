#include "Data.h"
#include "Utils.h"
#include "EventHandler.h"

extern BOOL   filterLoopback;
extern BOOL   filterICMP;
extern BOOL   filterTCP;
extern BOOL   filterUDP;
extern USHORT tcpLisn;
extern USHORT udpLisn;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	UNREFERENCED_PARAMETER(hModule);
	UNREFERENCED_PARAMETER(ul_reason_for_call);
	UNREFERENCED_PARAMETER(lpReserved);

	return TRUE;
}

#ifdef __cplusplus
extern "C" {
#endif

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
	case AIO_TCPLISN:
		break;
	case AIO_UDPLISN:
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

	return FALSE;
}

__declspec(dllexport) VOID __cdecl aio_free()
{
	UNREFERENCED_PARAMETER(WSACleanup());

	return;
}

#ifdef __cplusplus
}
#endif

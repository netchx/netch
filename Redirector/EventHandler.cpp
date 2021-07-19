#include "EventHandler.h"

#include "DNS.h"
#include "Data.h"

#include <stdio.h>

#include <map>
#include <regex>
#include <string>
#include <vector>

using namespace std;

extern BOOL dnsHook;
extern USHORT tcpLisn;
extern USHORT udpLisn;

typedef struct _TCPINFO {
	DWORD PID;
	PBYTE Target;
} TCPINFO, * PTCPINFO;

typedef struct _UDPINFO {
	SOCKET Socket;
} UDPINFO, * PUDPINFO;

vector<wstring> handleList;
vector<wstring> bypassList;

HANDLE TCPLock = NULL;
HANDLE UDPLock = NULL;
map<ENDPOINT_ID, PTCPINFO> TCPContext;
map<ENDPOINT_ID, PUDPINFO> UDPContext;

wstring getProcessName(DWORD id)
{
	if (id == 0)
	{
		return L"Idle";
	}

	if (id == 4)
	{
		return L"System";
	}

	wchar_t name[MAX_PATH];
	if (!nf_getProcessNameFromKernel(id, name, MAX_PATH))
	{
		if (!nf_getProcessNameW(id, name, MAX_PATH))
		{
			return L"Unknown";
		}
	}

	wchar_t result[MAX_PATH];
	if (GetLongPathNameW(name, result, MAX_PATH))
	{
		return result;
	}

	return name;
}

BOOL checkBypassName(DWORD id)
{
	auto name = getProcessName(id);

	for (size_t i = 0; i < bypassList.size(); i++)
	{
		if (regex_search(name, wregex(bypassList[i])))
		{
			return TRUE;
		}
	}

	return FALSE;
}

BOOL checkHandleName(DWORD id)
{
	auto name = getProcessName(id);

	for (size_t i = 0; i < handleList.size(); i++)
	{
		if (regex_search(name, wregex(handleList[i])))
		{
			return TRUE;
		}
	}

	return FALSE;
}

void eh_init()
{
	if (!TCPLock)
	{
		TCPLock = CreateMutex(NULL, FALSE, NULL);
	}

	if (!UDPLock)
	{
		UDPLock = CreateMutex(NULL, FALSE, NULL);
	}

	dns_init();
}

void eh_free()
{
	WaitForSingleObject(TCPLock, INFINITE);
	WaitForSingleObject(UDPLock, INFINITE);

	for (auto& [k, v] : TCPContext)
	{
		if (v->Target)
		{
			free(v->Target);
			v->Target = NULL;
		}
	}
	TCPContext.clear();

	for (auto& [k, v] : UDPContext)
	{
		if (v->Socket)
		{
			closesocket(v->Socket);
			v->Socket = NULL;
		}
	}
	UDPContext.clear();

	ReleaseMutex(TCPLock);
	ReleaseMutex(UDPLock);

	CloseHandle(TCPLock);
	CloseHandle(UDPLock);

	TCPLock = NULL;
	UDPLock = NULL;

	dns_free();
}

void threadStart()
{

}

void threadEnd()
{

}

void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	nf_tcpDisableFiltering(id);
}

void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	UNREFERENCED_PARAMETER(info);

	printf("[Redirector][EventHandler][tcpConnected][%llu]\n", id);
}

void tcpCanSend(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void tcpSend(ENDPOINT_ID id, const char* buffer, int length)
{
	nf_tcpPostSend(id, buffer, length);
}

void tcpCanReceive(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void tcpReceive(ENDPOINT_ID id, const char* buffer, int length)
{
	nf_tcpPostReceive(id, buffer, length);
}

void tcpClosed(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{

}

void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	nf_udpDisableFiltering(id);
}

void udpConnectRequest(ENDPOINT_ID id, PNF_UDP_CONN_REQUEST info)
{
	UNREFERENCED_PARAMETER(id);
	UNREFERENCED_PARAMETER(info);
}

void udpCanSend(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void udpSend(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options)
{
	nf_udpPostSend(id, target, buffer, length, options);
}

void udpCanReceive(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void udpReceive(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options)
{
	nf_udpPostReceive(id, target, buffer, length, options);
}

void udpClosed(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{

}

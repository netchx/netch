#include "EventHandler.h"

#include "DNSHandler.h"
#include "TCPHandler.h"
#include "UDPHandler.h"

BOOL Started = FALSE;
BOOL filterLoop = FALSE;
BOOL filterICMP = TRUE;
BOOL filterTCP = TRUE;
BOOL filterUDP = TRUE;
BOOL dnsHook = FALSE;
string dnsHost = "";
USHORT dnsPort = 0;
USHORT tcpLisn = 0;
USHORT udpLisn = 0;

vector<wstring> handleList;
vector<wstring> bypassList;

mutex tcpLock;
mutex udpLock;
map<ENDPOINT_ID, PTCPINFO> tcpContext;
map<ENDPOINT_ID, PUDPINFO> udpContext;

PDNSHandler dnsHandler = NULL;
PTCPHandler tcpHandler = NULL;
PUDPHandler udpHandler = NULL;

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

	wchar_t data[MAX_PATH];
	if (GetLongPathNameW(name, data, MAX_PATH))
	{
		return data;
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

BOOL eh_init()
{
	dnsHandler = new DNSHandler(dnsHost, dnsPort);
	tcpHandler = new TCPHandler(tcpLisn);
	udpHandler = new UDPHandler();
	
	if (!tcpHandler->init())
	{
		return FALSE;
	}

	return TRUE;
}

void eh_free()
{
	lock_guard<mutex> tlg(tcpLock);
	lock_guard<mutex> ulg(udpLock);

	for (auto& [k, v] : tcpContext)
	{
		continue;
	}
	tcpContext.clear();

	for (auto& [k, v] : udpContext)
	{
		if (v->Socket)
		{
			closesocket(v->Socket);
			v->Socket = NULL;
		}
	}
	udpContext.clear();

	if (dnsHandler)
	{
		dnsHandler->free();

		delete dnsHandler;
		dnsHandler = NULL;
	}

	if (tcpHandler)
	{
		tcpHandler->free();

		delete tcpHandler;
		tcpHandler = NULL;
	}

	if (udpHandler)
	{
		delete udpHandler;
		udpHandler = NULL;
	}
}

void threadStart()
{

}

void threadEnd()
{

}

void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	if (checkBypassName(info->processId))
	{
		nf_tcpDisableFiltering(id);

		printf("[Redirector][EventHandler][tcpConnectRequest][%llu] this->checkBypassName\n", id);
		return;
	}

	if (!checkHandleName(info->processId))
	{
		nf_tcpDisableFiltering(id);

		printf("[Redirector][EventHandler][tcpConnectRequest][%llu] !this->checkHandleName\n", id);
		return;
	}
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
	printf("[Redirector][EventHandler][tcpClosed][%llu]\n", id);
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
	printf("[Redirector][EventHandler][udpClosed][%llu]\n", id);

	lock_guard<mutex> lg(udpLock);
	if (udpContext.find(id) != udpContext.end())
	{
		udpContext.erase(id);
	}
}

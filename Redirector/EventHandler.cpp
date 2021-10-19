#include "EventHandler.h"

#include "DNSHandler.h"
#include "TCPHandler.h"

extern bool filterParent;
extern bool filterTCP;
extern bool filterUDP;
extern bool filterDNS;

extern bool dnsOnly;

extern vector<wstring> bypassList;
extern vector<wstring> handleList;

extern USHORT tcpListen;

DWORD CurrentID = 0;

mutex udpContextLock;
map<ENDPOINT_ID, SocksHelper::PUDP> udpContext;

atomic_ullong UP = { 0 };
atomic_ullong DL = { 0 };

wstring ConvertIP(PSOCKADDR addr)
{
	WCHAR buffer[MAX_PATH] = L"";
	DWORD bufferLength = MAX_PATH;

	if (addr->sa_family == AF_INET)
	{
		WSAAddressToStringW(addr, sizeof(SOCKADDR_IN), NULL, buffer, &bufferLength);
	}
	else
	{
		WSAAddressToStringW(addr, sizeof(SOCKADDR_IN6), NULL, buffer, &bufferLength);
	}

	return buffer;
}

wstring GetProcessName(DWORD id)
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

bool checkBypassName(DWORD id)
{
	auto name = GetProcessName(id);

	for (size_t i = 0; i < bypassList.size(); i++)
	{
		if (regex_search(name, wregex(bypassList[i])))
		{
			return true;
		}
	}

	return false;
}

bool checkHandleName(DWORD id)
{
	{
		auto name = GetProcessName(id);

		for (size_t i = 0; i < handleList.size(); i++)
		{
			if (regex_search(name, wregex(handleList[i])))
			{
				return true;
			}
		}
	}

	if (filterParent)
	{
		PROCESSENTRY32W PE;
		memset(&PE, 0, sizeof(PROCESSENTRY32W));
		PE.dwSize = sizeof(PROCESSENTRY32W);

		auto hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
		if (hSnapshot == INVALID_HANDLE_VALUE)
		{
			return false;
		}

		if (!Process32FirstW(hSnapshot, &PE))
		{
			CloseHandle(hSnapshot);
			return false;
		}

		do {
			if (PE.th32ProcessID == id)
			{
				auto name = GetProcessName(PE.th32ParentProcessID);

				for (size_t i = 0; i < handleList.size(); i++)
				{
					if (regex_search(name, wregex(handleList[i])))
					{
						CloseHandle(hSnapshot);
						return true;
					}
				}
			}
		} while (Process32NextW(hSnapshot, &PE));

		CloseHandle(hSnapshot);
	}

	return false;
}

bool eh_init()
{
	CurrentID = GetCurrentProcessId();

	if (!DNSHandler::INIT())
		return false;

	if (!TCPHandler::INIT())
		return false;

	return true;
}

void eh_free()
{
	lock_guard<mutex> lg(udpContextLock);

	TCPHandler::FREE();

	for (auto i : udpContext)
		delete i.second;
	udpContext.clear();

	UP = 0;
	DL = 0;
}

void threadStart()
{

}

void threadEnd()
{

}

void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	if (CurrentID == info->processId)
	{
		nf_tcpDisableFiltering(id);
		return;
	}

	if (!filterTCP)
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!filterTCP] " << GetProcessName(info->processId) << endl;
		return;
	}

	if (checkBypassName(info->processId))
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][checkBypassName] " << GetProcessName(info->processId) << endl;
		return;
	}

	if (!checkHandleName(info->processId))
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!checkHandleName] " << GetProcessName(info->processId) << endl;
		return;
	}

	if (info->ip_family != AF_INET && info->ip_family != AF_INET6)
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!IPv4 && !IPv6] " << GetProcessName(info->processId) << endl;
		return;
	}

	SOCKADDR_IN6 client;
	memcpy(&client, info->localAddress, sizeof(SOCKADDR_IN6));

	SOCKADDR_IN6 remote;
	memcpy(&remote, info->remoteAddress, sizeof(SOCKADDR_IN6));

	if (info->ip_family == AF_INET)
	{
		auto addr = (PSOCKADDR_IN)info->remoteAddress;
		addr->sin_family = AF_INET;
		addr->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		addr->sin_port = tcpListen;
	}

	if (info->ip_family == AF_INET6)
	{
		auto addr = (PSOCKADDR_IN6)info->remoteAddress;
		IN6ADDR_SETLOOPBACK(addr);
		addr->sin6_port = tcpListen;
	}

	TCPHandler::CreateHandler(client, remote);
	wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "] " << ConvertIP((PSOCKADDR)&client) << " -> " << ConvertIP((PSOCKADDR)&remote) << endl;
}

void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	wcout << "[Redirector][EventHandler][tcpConnected][" << id << "][" << info->processId << "][" << ConvertIP((PSOCKADDR)info->remoteAddress) << "] " << GetProcessName(info->processId) << endl;
}

void tcpCanSend(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void tcpSend(ENDPOINT_ID id, const char* buffer, int length)
{
	UP += length;

	nf_tcpPostSend(id, buffer, length);
}

void tcpCanReceive(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void tcpReceive(ENDPOINT_ID id, const char* buffer, int length)
{
	DL += length;

	nf_tcpPostReceive(id, buffer, length);
}

void tcpClosed(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	SOCKADDR_IN6 client;
	memcpy(&client, info->localAddress, sizeof(SOCKADDR_IN6));

	TCPHandler::DeleteHandler(client);

	printf("[Redirector][EventHandler][tcpClosed][%llu][%lu]\n", id, info->processId);
}

void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	if (CurrentID == info->processId)
	{
		nf_udpDisableFiltering(id);
		return;
	}

	if (!filterUDP)
	{
		if (!filterDNS) nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!filterUDP] " << GetProcessName(info->processId) << endl;
		return;
	}

	if (checkBypassName(info->processId))
	{
		if (dnsOnly) nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][checkBypassName] " << GetProcessName(info->processId) << endl;
		return;
	}

	if (!checkHandleName(info->processId))
	{
		if (dnsOnly) nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!checkHandleName] " << GetProcessName(info->processId) << endl;
		return;
	}

	wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "] " << GetProcessName(info->processId) << endl;

	lock_guard<mutex> lg(udpContextLock);
	udpContext[id] = new SocksHelper::UDP();
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
	if (DNSHandler::IsDNS((PSOCKADDR_IN6)target))
	{
		if (!filterDNS)
		{
			nf_udpPostSend(id, target, buffer, length, options);

			wcout << "[Redirector][EventHandler][udpSend][" << id << "] B DNS to " << ConvertIP((PSOCKADDR)target) << endl;
			return;
		}
		else
		{
			UP += length;
			DNSHandler::CreateHandler(id, (PSOCKADDR_IN6)target, buffer, length, options);

			wcout << "[Redirector][EventHandler][udpSend][" << id << "] H DNS to " << ConvertIP((PSOCKADDR)target) << endl;
			return;
		}
	}

	udpContextLock.lock();
	if (udpContext.find(id) == udpContext.end())
	{
		udpContextLock.unlock();

		nf_udpPostSend(id, target, buffer, length, options);
		return;
	}
	auto remote = udpContext[id];
	udpContextLock.unlock();

	if (remote->tcpSocket == INVALID_SOCKET && !remote->Associate())
		return;

	if (remote->udpSocket == INVALID_SOCKET)
	{
		if (!remote->CreateUDP())
			return;

		auto option = (PNF_UDP_OPTIONS)new char[sizeof(NF_UDP_OPTIONS) + options->optionsLength]();
		memcpy(option, options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);

		thread(udpReceiveHandler, id, remote, option).detach();
	}

	if (remote->Send((PSOCKADDR_IN6)target, buffer, length) == length)
		UP += length;
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
	UNREFERENCED_PARAMETER(info);

	printf("[Redirector][EventHandler][udpClosed][%llu]\n", id);

	lock_guard<mutex> lg(udpContextLock);
	if (udpContext.find(id) != udpContext.end())
	{
		delete udpContext[id];

		udpContext.erase(id);
	}
}

void udpReceiveHandler(ENDPOINT_ID id, SocksHelper::PUDP remote, PNF_UDP_OPTIONS options)
{
	char buffer[1458];

	while (remote->tcpSocket != INVALID_SOCKET && remote->udpSocket != INVALID_SOCKET)
	{
		SOCKADDR_IN6 target;

		int length = remote->Read(&target, buffer, sizeof(buffer), NULL);
		if (length == 0 || length == SOCKET_ERROR)
			break;

		DL += length;

		nf_udpPostReceive(id, (unsigned char*)&target, buffer, length, options);
	}

	delete[] options;
}

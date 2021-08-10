#include "EventHandler.h"

#include "TCPHandler.h"

extern BOOL filterTCP;
extern BOOL filterUDP;
extern USHORT udpPort;
extern vector<wstring> bypassList;
extern vector<wstring> handleList;

extern mutex tcpLock;
extern mutex udpLock;
extern map<ENDPOINT_ID, PTCPINFO> tcpContext;
extern map<ENDPOINT_ID, PUDPINFO> udpContext;

PTCPHandler tcpHandler = NULL;

wstring getAddrString(PSOCKADDR addr)
{
	WCHAR buffer[MAX_PATH] = L"";
	DWORD bufferLength = MAX_PATH;

	if (addr->sa_family == AF_INET)
	{
		WSAAddressToString(addr, sizeof(SOCKADDR_IN), NULL, buffer, &bufferLength);
	}
	else
	{
		WSAAddressToString(addr, sizeof(SOCKADDR_IN6), NULL, buffer, &bufferLength);
	}

	return buffer;
}

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
	tcpHandler = new TCPHandler();
	if (!tcpHandler->init())
	{
		return FALSE;
	}

	return TRUE;
}

void eh_free()
{
	{
		lock_guard<mutex> lg(tcpLock);

		for (auto& [k, v] : tcpContext)
		{
			delete v;
			continue;
		}
		tcpContext.clear();

		if (tcpHandler)
		{
			tcpHandler->free();

			delete tcpHandler;
			tcpHandler = NULL;
		}
	}
	
	{
		lock_guard<mutex> lg(udpLock);

		for (auto& [k, v] : udpContext)
		{
			if (v->Socket)
			{
				closesocket(v->Socket);
				v->Socket = NULL;
			}

			delete v;
			continue;
		}
		udpContext.clear();
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
	if (!filterTCP)
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!filterTCP] " << getProcessName(info->processId) << endl;
		return;
	}

	if (checkBypassName(info->processId))
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][checkBypassName] " << getProcessName(info->processId) << endl;
		return;
	}

	if (!checkHandleName(info->processId))
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!checkHandleName] " << getProcessName(info->processId) << endl;
		return;
	}

	if (info->ip_family != AF_INET && info->ip_family != AF_INET6)
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!IPv4 && !IPv6] " << getProcessName(info->processId) << endl;
		return;
	}

	tcpHandler->Create(id, info);
	wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][" << getAddrString((PSOCKADDR)info->remoteAddress) <<  "] " << getProcessName(info->processId) << endl;

	if (info->ip_family == AF_INET)
	{
		auto target = (PSOCKADDR_IN)info->remoteAddress;
		target->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		target->sin_port = htons(tcpHandler->ListenIPv4);
	}

	if (info->ip_family == AF_INET6)
	{
		auto target = (PSOCKADDR_IN6)info->remoteAddress;
		memset(target->sin6_addr.u.Byte, 0, 16);
		target->sin6_addr.u.Byte[15] = 0x01;
		target->sin6_port = htons(tcpHandler->ListenIPv6);
	}
}

void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	wcout << "[Redirector][EventHandler][tcpConnected][" << id << "][" << info->processId << "][" << getAddrString((PSOCKADDR)info->remoteAddress) << "] " << getProcessName(info->processId) << endl;
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
	tcpHandler->Delete(id);

	printf("[Redirector][EventHandler][tcpClosed][%llu][%lu]\n", id, info->processId);
}

void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	if (!filterUDP)
	{
		nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!filterUDP] " << getProcessName(info->processId) << endl;
		return;
	}

	if (checkBypassName(info->processId))
	{
		nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][checkBypassName] " << getProcessName(info->processId) << endl;
		return;
	}

	if (!checkHandleName(info->processId))
	{
		nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!checkHandleName] " << getProcessName(info->processId) << endl;
		return;
	}

	lock_guard<mutex> lg(udpLock);
	udpContext[id] = new UDPINFO();
	udpContext[id]->PID = info->processId;
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
	lock_guard<mutex> lg(udpLock);
	if (udpContext.find(id) == udpContext.end())
	{
		nf_udpPostSend(id, target, buffer, length, options);
		return;
	}

	if (!udpContext[id]->Socket)
	{
		auto client = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (!client)
		{
			printf("[Redirector][EventHandler][udpSend][%llu] Create socket failed: %d\n", id, WSAGetLastError());
			return;
		}

		SOCKADDR_IN addr;
		addr.sin_family = AF_INET;
		addr.sin_addr.S_un.S_addr = INADDR_ANY;
		addr.sin_port = 0;

		if (bind(client, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN)) == SOCKET_ERROR)
		{
			closesocket(client);

			printf("[Redirector][EventHandler][udpSend][%llu] Bind socket failed: %d\n", id, WSAGetLastError());
			return;
		}

		addr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		addr.sin_port = htons(udpPort);

		if (sendto(client, (PCHAR)&udpContext[id]->PID, 4, 0, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN)) == SOCKET_ERROR)
		{
			closesocket(client);

			printf("[Redirector][EventHandler][udpSend][%llu] Send initial data failed: %d\n", id, WSAGetLastError());
			return;
		}
		udpContext[id]->Socket = client;

		auto data = new BYTE[sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1]();
		memcpy(data, options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);
		thread(&udpBeginReceive, id, client, data).detach();
	}

	char* data = NULL;
	int dataLength = 0;
	if (((PSOCKADDR)target)->sa_family == AF_INET)
	{
		dataLength = length + 7;
		data = new char[dataLength]();
		data[0] = 0x01;

		auto addr = (PSOCKADDR_IN)target;
		memcpy(data + 1, &addr->sin_addr, 4);
		memcpy(data + 5, &addr->sin_port, 2);
		memcpy(data + 7, buffer, length);
	}
	else if (((PSOCKADDR)target)->sa_family == AF_INET6)
	{
		dataLength = length + 19;
		data = new char[dataLength]();
		data[0] = 0x04;

		auto addr = (PSOCKADDR_IN6)target;
		memcpy(data + 1, &addr->sin6_addr, 16);
		memcpy(data + 17, &addr->sin6_port, 2);
		memcpy(data + 19, buffer, length);
	}
	else
	{
		nf_udpPostSend(id, target, buffer, length, options);
		return;
	}

	if (data)
	{
		SOCKADDR_IN remote;
		remote.sin_family = AF_INET;
		remote.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		remote.sin_port = htons(udpPort);

		if (sendto(udpContext[id]->Socket, data, dataLength, 0, (PSOCKADDR)&remote, sizeof(SOCKADDR_IN)) == SOCKET_ERROR)
		{
			printf("[Redirector][EventHandler][udpSend][%llu] Send data failed: %d\n", id, WSAGetLastError());
		}

		delete[] data;
	}
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

	lock_guard<mutex> lg(udpLock);
	if (udpContext.find(id) != udpContext.end())
	{
		if (udpContext[id]->Socket != NULL)
		{
			closesocket(udpContext[id]->Socket);
			udpContext[id]->Socket = NULL;
		}

		udpContext.erase(id);
	}

	printf("[Redirector][EventHandler][udpClosed][%llu]\n", id);
}

void udpBeginReceive(ENDPOINT_ID id, SOCKET client, PBYTE data)
{
	auto buffer = new char[NF_TCP_PACKET_BUF_SIZE]();

	while (true)
	{
		SOCKADDR_IN remote;
		int remoteLength = sizeof(SOCKADDR_IN);

		int length = recvfrom(client, buffer, NF_TCP_PACKET_BUF_SIZE, 0, (PSOCKADDR)&remote, &remoteLength);
		if (length == 0)
		{
			break;
		}

		if (length == SOCKET_ERROR)
		{
			int last = WSAGetLastError();
			if (last == 10004)
			{
				continue;
			}
			else if (last == 10038)
			{
				break;
			}

			printf("[Redirector][udpBeginReceive][%llu] Receive failed: %d\n", id, last);
			break;
		}

		if (buffer[0] == 0x01 && length > 7)
		{
			SOCKADDR_IN target;
			target.sin_family = AF_INET;
			memcpy(&target.sin_addr, buffer + 1, 4);
			memcpy(&target.sin_port, buffer + 5, 2);

			nf_udpPostReceive(id, (PBYTE)&target, buffer + 7, length - 7, (PNF_UDP_OPTIONS)data);
		}
		else if (buffer[0] == 0x04 && length > 19)
		{
			SOCKADDR_IN6 target;
			target.sin6_family = AF_INET6;
			memcpy(&target.sin6_addr, buffer + 1, 16);
			memcpy(&target.sin6_port, buffer + 17, 2);

			nf_udpPostReceive(id, (PBYTE)&target, buffer + 19, length - 19, (PNF_UDP_OPTIONS)data);
		}
	}

	delete[] data;
	delete[] buffer;
}

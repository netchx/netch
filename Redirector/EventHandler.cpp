#include "EventHandler.h"

#include "TCPHandler.h"

extern BOOL filterTCP;
extern BOOL filterUDP;
extern vector<wstring> bypassList;
extern vector<wstring> handleList;

extern USHORT tcpListen;

mutex udpContextLock;
map<ENDPOINT_ID, SocksHelper::PUDP> udpContext;

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
	auto name = GetProcessName(id);

	for (size_t i = 0; i < handleList.size(); i++)
	{
		if (regex_search(name, wregex(handleList[i])))
		{
			return true;
		}
	}

	return false;
}

bool eh_init()
{
	return TCPHandler::Init();
}

void eh_free()
{
	TCPHandler::Free();
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
	SOCKADDR_IN6 client;
	memcpy(&client, info->localAddress, sizeof(SOCKADDR_IN6));

	TCPHandler::DeleteHandler(client);

	printf("[Redirector][EventHandler][tcpClosed][%llu][%lu]\n", id, info->processId);
}

void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	if (!filterUDP)
	{
		nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!filterUDP] " << GetProcessName(info->processId) << endl;
		return;
	}

	if (checkBypassName(info->processId))
	{
		nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][checkBypassName] " << GetProcessName(info->processId) << endl;
		return;
	}

	if (!checkHandleName(info->processId))
	{
		nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!checkHandleName] " << GetProcessName(info->processId) << endl;
		return;
	}

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
	udpContextLock.lock();
	if (udpContext.find(id) == udpContext.end())
	{
		udpContextLock.unlock();

		nf_udpPostSend(id, target, buffer, length, options);
		return;
	}

	auto conn = udpContext[id];
	udpContextLock.unlock();

	if (conn->tcpSocket == INVALID_SOCKET)
	{
		auto tcpSocket = SocksHelper::Utils::Connect();
		if (tcpSocket == INVALID_SOCKET)
		{
			printf("[Redirector][EventHandler][udpSend][%llu] Connect to remote server failed\n", id);
			return;
		}

		if (!SocksHelper::Utils::Handshake(tcpSocket))
		{
			closesocket(tcpSocket);

			printf("[Redirector][EventHandler][udpSend][%llu] Handshake failed\n", id);
			return;
		}

		conn->tcpSocket = tcpSocket;
	}

	if (conn->udpSocket == INVALID_SOCKET)
	{
		if (!conn->Associate())
		{
			closesocket(conn->tcpSocket);
			conn->tcpSocket = INVALID_SOCKET;

			printf("[Redirector][EventHandler][udpSend][%llu] UDP Associate failed\n", id);
			return;
		}

		if (!conn->CreateUDP())
		{
			closesocket(conn->tcpSocket);
			conn->tcpSocket = INVALID_SOCKET;

			printf("[Redirector][EventHandler][udpSend][%llu] Create UDP socket failed\n", id);
			return;
		}

		PNF_UDP_OPTIONS data = new NF_UDP_OPTIONS();
		memcpy(data, options, sizeof(NF_UDP_OPTIONS));

		thread(udpBeginReceive, id, conn, data).detach();
	}

	if (conn->Send((PSOCKADDR)target, buffer, length) == SOCKET_ERROR)
	{
		closesocket(conn->tcpSocket);
		closesocket(conn->udpSocket);

		conn->tcpSocket = INVALID_SOCKET;
		conn->udpSocket = INVALID_SOCKET;

		printf("[Redirector][EventHandler][udpSend][%llu] Send data failed\n", id);
		return;
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

	printf("[Redirector][EventHandler][udpClosed][%llu]\n", id);

	lock_guard<mutex> lg(udpContextLock);
	if (udpContext.find(id) != udpContext.end())
	{
		delete udpContext[id];

		udpContext.erase(id);
	}
}

void udpBeginReceive(ENDPOINT_ID id, SocksHelper::PUDP conn, PNF_UDP_OPTIONS data)
{
	char buffer[1458];

	while (conn->udpSocket != INVALID_SOCKET)
	{
		SOCKADDR_IN6 target;
		int targetLength = sizeof(SOCKADDR_IN6);

		int length = conn->Read((PSOCKADDR)&target, buffer, 1458);
		if (length == 0 || length == SOCKET_ERROR)
		{
			break;
		}

		nf_udpPostReceive(id, (unsigned char*)&target, buffer, length, data);
	}

	delete data;
}

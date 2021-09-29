#include "TCPHandler.h"

SOCKET tcpSocket = INVALID_SOCKET;
USHORT tcpListen = 0;

mutex tcpLock;
map<USHORT, SOCKADDR_IN6> tcpContext;

bool TCPHandler::Init()
{
	auto lg = lock_guard<mutex>(tcpLock);

	if (tcpSocket != INVALID_SOCKET)
	{
		closesocket(tcpSocket);
		tcpSocket = INVALID_SOCKET;
	}

	auto client = socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
	if (client == INVALID_SOCKET)
	{
		printf("[Redirector][TCPHandler::Init] Create socket failed: %d\n", WSAGetLastError());
		return false;
	}

	{
		int v6only = 0;
		if (setsockopt(client, IPPROTO_IPV6, IPV6_V6ONLY, (char*)&v6only, sizeof(v6only)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::Init] Set socket option failed: %d\n", WSAGetLastError());

			closesocket(client);
			return false;
		}
	}

	{
		SOCKADDR_IN6 addr;
		IN6ADDR_SETANY(&addr);

		if (bind(client, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN6)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::Init] Bind socket failed: %d\n", WSAGetLastError());

			closesocket(client);
			return false;
		}
	}
	
	if (listen(client, 1024) == SOCKET_ERROR)
	{
		printf("[Redirector][TCPHandler::Init] Listen socket failed: %d\n", WSAGetLastError());

		closesocket(client);
		return false;
	}

	{
		SOCKADDR_IN6 addr;
		int addrLength = sizeof(SOCKADDR_IN6);
		if (getsockname(client, (PSOCKADDR)&addr, &addrLength) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::Init] Get listen address failed: %d\n", WSAGetLastError());

			closesocket(client);
			return false;
		}

		tcpListen = (addr.sin6_family == AF_INET6) ? addr.sin6_port : ((PSOCKADDR_IN)&addr)->sin_port;
	}

	tcpSocket = client;

	thread(TCPHandler::Accept).detach();
	return true;
}

void TCPHandler::Free()
{
	auto lg = lock_guard<mutex>(tcpLock);

	if (tcpSocket != INVALID_SOCKET)
	{
		closesocket(tcpSocket);
		tcpSocket = INVALID_SOCKET;
	}
	tcpListen = 0;

	tcpContext.clear();
}

void TCPHandler::CreateHandler(SOCKADDR_IN6 client, SOCKADDR_IN6 remote)
{
	auto lg = lock_guard<mutex>(tcpLock);

	auto id = (client.sin6_family == AF_INET6) ? client.sin6_port : ((PSOCKADDR_IN)&client)->sin_port;
	if (tcpContext.find(id) != tcpContext.end())
	{
		tcpContext.erase(id);
	}

	tcpContext[id] = remote;
}

void TCPHandler::DeleteHandler(SOCKADDR_IN6 client)
{
	auto lg = lock_guard<mutex>(tcpLock);

	auto id = (client.sin6_family == AF_INET6) ? client.sin6_port : ((PSOCKADDR_IN)&client)->sin_port;
	if (tcpContext.find(id) != tcpContext.end())
	{
		tcpContext.erase(id);
	}
}

void TCPHandler::Accept()
{
	while (tcpSocket != INVALID_SOCKET)
	{
		auto client = accept(tcpSocket, NULL, NULL);
		if (client == INVALID_SOCKET)
		{
			printf("[Redirector][TCPHandler::Accept] Accept client failed: %d\n", WSAGetLastError());
			return;
		}

		thread(TCPHandler::Handle, client).detach();
	}
}

void TCPHandler::Handle(SOCKET client)
{
	USHORT id = 0;
	{
		SOCKADDR_IN6 addr;
		int addrLength = sizeof(SOCKADDR_IN6);
		if (getpeername(client, (PSOCKADDR)&addr, &addrLength) == SOCKET_ERROR)
		{
			closesocket(client);
			return;
		}

		id = (addr.sin6_family == AF_INET6) ? addr.sin6_port : ((PSOCKADDR_IN)&addr)->sin_port;
	}

	auto remote = SocksHelper::Utils::Connect();
	if (remote == INVALID_SOCKET)
	{
		closesocket(client);
		return;
	}

	if (!SocksHelper::Utils::Handshake(remote))
	{
		closesocket(client);
		closesocket(remote);
		return;
	}

	tcpLock.lock();
	if (tcpContext.find(id) == tcpContext.end())
	{
		tcpLock.unlock();

		closesocket(client);
		return;
	}

	auto target = tcpContext[id];
	tcpLock.unlock();

	auto conn = new SocksHelper::TCP(remote);
	if (!conn->Connect(&target))
	{
		delete conn;

		closesocket(client);
		return;
	}

	thread(TCPHandler::Send, client, conn).detach();
	TCPHandler::Read(client, conn);

	closesocket(client);
	closesocket(remote);
	delete conn;
}

void TCPHandler::Read(SOCKET client, SocksHelper::PTCP remote)
{
	char buffer[1446];
	
	while (tcpSocket != INVALID_SOCKET)
	{
		int length = remote->Read(buffer, 1446);
		if (length == 0 || length == SOCKET_ERROR)
		{
			return;
		}

		if (send(client, buffer, length, 0) != length)
		{
			return;
		}
	}
}

void TCPHandler::Send(SOCKET client, SocksHelper::PTCP remote)
{
	char buffer[1446];

	while (tcpSocket != INVALID_SOCKET)
	{
		int length = recv(client, buffer, 1446, 0);
		if (length == 0 || length == SOCKET_ERROR)
		{
			return;
		}

		if (remote->Send(buffer, length) != length)
		{
			return;
		}
	}
}

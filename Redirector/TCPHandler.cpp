#include "TCPHandler.h"

atomic<SOCKET> tcpSocket{ INVALID_SOCKET };
atomic<USHORT> tcpListen{ 0 };

mutex tcpLock;
map<USHORT, SOCKADDR_IN6> tcpContext;
map<SOCKET, shared_ptr<SocksHelper::TCP>> activeConnections;

bool TCPHandler::INIT()
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
		printf("[Redirector][TCPHandler::INIT] Create socket failed: %d\n", WSAGetLastError());
		return false;
	}

	{
		int v6only = 0;
		if (setsockopt(client, IPPROTO_IPV6, IPV6_V6ONLY, (char*)&v6only, sizeof(v6only)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::INIT] Set socket option failed: %d\n", WSAGetLastError());

			closesocket(client);
			return false;
		}
	}

	{
		SOCKADDR_IN6 addr;
		IN6ADDR_SETANY(&addr);

		if (bind(client, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN6)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::INIT] Bind socket failed: %d\n", WSAGetLastError());

			closesocket(client);
			return false;
		}
	}
	
	if (listen(client, 1024) == SOCKET_ERROR)
	{
		printf("[Redirector][TCPHandler::INIT] Listen socket failed: %d\n", WSAGetLastError());

		closesocket(client);
		return false;
	}

	{
		SOCKADDR_IN6 addr;
		int addrLength = sizeof(SOCKADDR_IN6);
		if (getsockname(client, (PSOCKADDR)&addr, &addrLength) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::INIT] Get listen address failed: %d\n", WSAGetLastError());

			closesocket(client);
			return false;
		}

		tcpListen = (addr.sin6_family == AF_INET6) ? addr.sin6_port : ((PSOCKADDR_IN)&addr)->sin_port;
	}

	tcpSocket = client;

	thread(TCPHandler::Accept).detach();
	return true;
}

void TCPHandler::FREE()
{
	auto lg = lock_guard<mutex>(tcpLock);

	if (tcpSocket != INVALID_SOCKET)
	{
		closesocket(tcpSocket);

		tcpSocket = INVALID_SOCKET;
	}
	tcpListen = 0;
	// Closing the listening socket is not enough: accepted connections may
	// still be blocked in recv() on either the game or the SOCKS server.
	for (const auto& connection : activeConnections)
	{
		shutdown(connection.first, SD_BOTH);
		if (connection.second->tcpSocket != INVALID_SOCKET)
			shutdown(connection.second->tcpSocket, SD_BOTH);
	}

	tcpContext.clear();
}

void TCPHandler::CreateHandler(SOCKADDR_IN6 client, SOCKADDR_IN6 remote)
{
	auto lg = lock_guard<mutex>(tcpLock);

	auto id = (client.sin6_family == AF_INET) ? ((PSOCKADDR_IN)&client)->sin_port : client.sin6_port;
	if (tcpContext.find(id) != tcpContext.end())
		tcpContext.erase(id);

	tcpContext[id] = remote;
}

void TCPHandler::DeleteHandler(SOCKADDR_IN6 client)
{
	auto lg = lock_guard<mutex>(tcpLock);

	auto id = (client.sin6_family == AF_INET) ? ((PSOCKADDR_IN)&client)->sin_port : client.sin6_port;
	if (tcpContext.find(id) != tcpContext.end())
		tcpContext.erase(id);
}

void TCPHandler::Accept()
{
	while (tcpSocket != INVALID_SOCKET)
	{
		auto client = accept(tcpSocket, NULL, NULL);
		if (client == INVALID_SOCKET)
		{
			int lasterr = WSAGetLastError();
			if (lasterr == 10004)
				return;

			printf("[Redirector][TCPHandler::Accept] Accept client failed: %d\n", lasterr);
			return;
		}

		BOOL noDelay = TRUE;
		if (setsockopt(client, IPPROTO_TCP, TCP_NODELAY, (char*)&noDelay, sizeof(noDelay)) == SOCKET_ERROR)
			printf("[Redirector][TCPHandler::Accept] Disable Nagle failed: %d\n", WSAGetLastError());

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

		id = (addr.sin6_family == AF_INET) ? ((PSOCKADDR_IN)&addr)->sin_port : addr.sin6_port;
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

	auto remote = make_shared<SocksHelper::TCP>();
	if (!remote->Connect(&target))
	{
		closesocket(client);
		return;
	}
	{
		lock_guard<mutex> lg(tcpLock);
		if (tcpSocket == INVALID_SOCKET)
		{
			closesocket(client);
			return;
		}
		activeConnections[client] = remote;
	}

	thread(TCPHandler::Send, client, remote).detach();
	TCPHandler::Read(client, remote.get());

	// Wake the detached sender before closing the client socket. It retains
	// shared ownership of the upstream connection until it finishes.
	shutdown(client, SD_BOTH);
	shutdown(remote->tcpSocket, SD_BOTH);
	{
		lock_guard<mutex> lg(tcpLock);
		activeConnections.erase(client);
	}
	closesocket(client);
}

void TCPHandler::Read(SOCKET client, SocksHelper::PTCP remote)
{
	char buffer[1446];
	
	while (tcpSocket != INVALID_SOCKET)
	{
		int length = remote->Read(buffer, sizeof(buffer));
		if (length == 0 || length == SOCKET_ERROR)
			return;

		int sent = 0;
		while (sent < length)
		{
			const int count = send(client, buffer + sent, length - sent, 0);
			if (count <= 0)
				return;
			sent += count;
		}
	}
}

void TCPHandler::Send(SOCKET client, shared_ptr<SocksHelper::TCP> remote)
{
	char buffer[1446];

	while (tcpSocket != INVALID_SOCKET)
	{
		int length = recv(client, buffer, sizeof(buffer), 0);
		if (length == 0 || length == SOCKET_ERROR)
			return;

		if (remote->Send(buffer, length) != length)
			return;
	}
}

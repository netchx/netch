#include "TCPHandler.h"

TCPHandler::TCPHandler(USHORT tcpPort)
{
	this->tcpPort = tcpPort;
}

BOOL TCPHandler::init()
{
	{
		this->IPv4Socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		if (!this->IPv4Socket)
		{
			printf("[Redirector][TCPHandler::init][IPv4] Create socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		SOCKADDR_IN addr;
		addr.sin_family = AF_INET;
		addr.sin_port = 0;

		if (bind(this->IPv4Socket, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv4] Bind socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		if (listen(this->IPv4Socket, 1024) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv4] Listen socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}
	}

	{
		this->IPv6Socket = socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
		if (!this->IPv6Socket)
		{
			printf("[Redirector][TCPHandler::init][IPv6] Create socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		SOCKADDR_IN6 addr;
		addr.sin6_family = AF_INET6;
		addr.sin6_port = 0;

		if (bind(this->IPv6Socket, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN6)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv6] Bind socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		if (listen(this->IPv6Socket, 1024) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv6] Listen socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}
	}

	this->tcpLock = CreateMutex(NULL, FALSE, NULL);
	if (!this->tcpLock)
	{
		return FALSE;
	}

	this->Started = TRUE;
	thread(&TCPHandler::IPv4, this).detach();
	thread(&TCPHandler::IPv6, this).detach();
	return TRUE;
}

void TCPHandler::free()
{
	WaitForSingleObject(this->tcpLock, INFINITE);

	if (this->IPv4Socket)
	{
		closesocket(this->IPv4Socket);
		this->IPv4Socket = NULL;
	}

	if (this->IPv6Socket)
	{
		closesocket(this->IPv6Socket);
		this->IPv6Socket = NULL;
	}

	for (auto& [k, v] : this->tcpContext)
	{
		continue;
	}
	this->tcpContext.clear();

	if (this->tcpLock)
	{
		CloseHandle(this->tcpLock);
		this->tcpLock = NULL;
	}
}

void TCPHandler::IPv4()
{
	while (this->Started && this->IPv4Socket)
	{
		SOCKADDR_IN addr;
		int addrLength = 0;

		auto client = accept(this->IPv4Socket, (PSOCKADDR)&addr, &addrLength);
		if (!client)
		{
			printf("[Redirector][TCPHandler::IPv4Handler] Accept client failed: %d\n", WSAGetLastError());
			return;
		}

		WaitForSingleObject(this->tcpLock, INFINITE);
		if (this->tcpContext.find(addr.sin_port) == this->tcpContext.end())
		{
			ReleaseMutex(this->tcpLock);

			closesocket(client);
			continue;
		}
		ReleaseMutex(this->tcpLock);

		thread(&TCPHandler::Handle, this, client, addr.sin_port).detach();
	}
}

void TCPHandler::IPv6()
{
	while (this->Started && this->IPv6Socket)
	{

	}
}

void TCPHandler::Handle(SOCKET client, USHORT side)
{
	SOCKADDR_IN addr;
	addr.sin_family = AF_INET;
	addr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
	addr.sin_port = htons(this->tcpPort);

	WaitForSingleObject(this->tcpLock, INFINITE);
	auto data = this->tcpContext[side];
	ReleaseMutex(this->tcpLock);

	auto remote = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (!remote)
	{
		printf("[Redirector][TCPHandler::Handle] Create socket failed: %d\n", WSAGetLastError());

		closesocket(client);
		return;
	}

	if (connect(remote, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN)) != NO_ERROR)
	{
		printf("[Redirector][TCPHandler::Handle] Connect to remote failed: %d\n", WSAGetLastError());

		closesocket(client);
		closesocket(remote);
		return;
	}
}

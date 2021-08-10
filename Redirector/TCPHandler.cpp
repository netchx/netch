#include "TCPHandler.h"

extern USHORT tcpPort;

extern mutex tcpLock;
extern map<ENDPOINT_ID, PTCPINFO> tcpContext;

void IoConn(SOCKET client, SOCKET remote)
{
	auto buffer =  new char[NF_TCP_PACKET_BUF_SIZE]();

	while (true)
	{
		auto length = recv(client, buffer, NF_TCP_PACKET_BUF_SIZE, 0);
		if (!length)
		{
			if (length == SOCKET_ERROR)
			{
				printf("[Redirector][TCPHandler][IoConn] Receive failed: %d\n", WSAGetLastError());
				break;
			}
			
			continue;
		}

		auto sended = send(remote, buffer, length, 0);
		if (!sended && sended != length)
		{
			printf("[Redirector][TCPHandler][IoConn] Send failed: %d\n", WSAGetLastError());
			break;
		}
	}

	delete[] buffer;
}

BOOL TCPHandler::init()
{
	{
		this->SocketIPv4 = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		if (!this->SocketIPv4)
		{
			printf("[Redirector][TCPHandler::init][IPv4] Create socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		SOCKADDR_IN addr;
		addr.sin_family = AF_INET;
		addr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		addr.sin_port = 0;

		if (bind(this->SocketIPv4, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv4] Bind socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		if (listen(this->SocketIPv4, 1024) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv4] Listen socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		int addrLength = 0;
		if (getsockname(this->SocketIPv4, (PSOCKADDR)&addr, &addrLength) != NO_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv4] Get local address failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		this->ListenIPv4 = addr.sin_port;
	}

	{
		this->SocketIPv6 = socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
		if (!this->SocketIPv6)
		{
			printf("[Redirector][TCPHandler::init][IPv6] Create socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		SOCKADDR_IN6 addr;
		addr.sin6_family = AF_INET6;
		addr.sin6_addr.u.Byte[15] = 1;
		addr.sin6_port = 0;

		if (bind(this->SocketIPv6, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN6)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv6] Bind socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		if (listen(this->SocketIPv6, 1024) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv6] Listen socket failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		int addrLength = 0;
		if (getsockname(this->SocketIPv6, (PSOCKADDR)&addr, &addrLength) != NO_ERROR)
		{
			printf("[Redirector][TCPHandler::init][IPv6] Get local address failed: %d\n", WSAGetLastError());
			return FALSE;
		}

		this->ListenIPv6 = addr.sin6_port;
	}

	thread(&TCPHandler::IPv4, this).detach();
	thread(&TCPHandler::IPv6, this).detach();
	return TRUE;
}

void TCPHandler::free()
{
	lock_guard<mutex> lg(this->Lock);

	if (this->SocketIPv4)
	{
		closesocket(this->SocketIPv4);
		this->SocketIPv4 = NULL;
	}

	if (this->SocketIPv6)
	{
		closesocket(this->SocketIPv6);
		this->SocketIPv6 = NULL;
	}

	this->Context.clear();
}

void TCPHandler::Create(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	auto uid = (info->ip_family == AF_INET) ? ((PSOCKADDR_IN)info->localAddress)->sin_port : ((PSOCKADDR_IN6)info->localAddress)->sin6_port;

	auto data = new TCPINFO();
	data->PID = info->processId;
	memcpy(data->Client, info->localAddress, NF_MAX_ADDRESS_LENGTH);
	memcpy(data->Target, info->remoteAddress, NF_MAX_ADDRESS_LENGTH);

	lock_guard<mutex> lga(tcpLock);
	lock_guard<mutex> lgb(this->Lock);
	tcpContext[id] = data;
	this->Context[uid] = id;
}

void TCPHandler::Delete(ENDPOINT_ID id)
{
	lock_guard<mutex> lga(tcpLock);
	lock_guard<mutex> lgb(this->Lock);

	if (tcpContext.find(id) != tcpContext.end())
	{
		delete tcpContext[id];

		tcpContext.erase(id);
	}

	USHORT uid = 0;
	for (auto i = this->Context.begin(); i != this->Context.end(); i++)
	{
		if (i->second == id)
		{
			uid = i->first;
			break;
		}
	}

	if (uid)
	{
		this->Context.erase(uid);
	}
}

void TCPHandler::IPv4()
{
	SOCKADDR_IN addr;
	int addrLength = 0;

	while (this->SocketIPv4)
	{
		auto client = accept(this->SocketIPv4, (PSOCKADDR)&addr, &addrLength);
		if (!client)
		{
			printf("[Redirector][TCPHandler::IPv4] Accept client failed: %d\n", WSAGetLastError());
			return;
		}

		{
			lock_guard<mutex> lg(this->Lock);
			if (this->Context.find(addr.sin_port) == this->Context.end())
			{
				closesocket(client);
				continue;
			}
		}

		thread(&TCPHandler::Handle, this, client, addr.sin_port).detach();
	}
}

void TCPHandler::IPv6()
{
	SOCKADDR_IN6 addr;
	int addrLength = 0;

	while (this->SocketIPv6)
	{
		auto client = accept(this->SocketIPv6, (PSOCKADDR)&addr, &addrLength);
		if (!client)
		{
			printf("[Redirector][TCPHandler::IPv6] Accept client failed: %d\n", WSAGetLastError());
			return;
		}

		{
			lock_guard<mutex> lg(this->Lock);
			if (this->Context.find(addr.sin6_port) == this->Context.end())
			{
				closesocket(client);
				continue;
			}
		}

		thread(&TCPHandler::Handle, this, client, addr.sin6_port).detach();
	}
}

void TCPHandler::Handle(SOCKET client, USHORT side)
{
	tcpLock.lock();
	this->Lock.lock();
	PTCPINFO data = tcpContext[this->Context[side]];
	this->Lock.unlock();
	tcpLock.unlock();

	SOCKADDR_IN addr;
	addr.sin_family = AF_INET;
	addr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
	addr.sin_port = htons(tcpPort);

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

	if (((PSOCKADDR)data->Target)->sa_family == AF_INET)
	{
		auto target = (PSOCKADDR_IN)data->Target;

		char buffer[11];
		buffer[0] = 0x01;
		memcpy(buffer + 1, &target->sin_addr.S_un.S_addr, 4);
		memcpy(buffer + 5, &target->sin_port, 2);
		memcpy(buffer + 7, &data->PID, 4);

		if (send(remote, buffer, 11, 0) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::Handle] Send request failed: %d\n", WSAGetLastError());

			closesocket(client);
			closesocket(remote);
			return;
		}
	}
	else
	{
		auto target = (PSOCKADDR_IN6)data->Target;

		char buffer[23];
		buffer[0] = 0x04;
		memcpy(buffer + 1, target->sin6_addr.u.Byte, 16);
		memcpy(buffer + 17, &target->sin6_port, 2);
		memcpy(buffer + 19, &data->PID, 4);

		if (send(remote, buffer, 23, 0) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::Handle] Send request failed: %d\n", WSAGetLastError());

			closesocket(client);
			closesocket(remote);
			return;
		}
	}

	thread(IoConn, client, remote).detach();
	IoConn(remote, client);

	closesocket(client);
	closesocket(remote);
}

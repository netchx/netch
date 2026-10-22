#include "SocksHelper.h"

#include "Utils.h"

extern wstring tgtHost;
extern wstring tgtPort;
extern string tgtUsername;
extern string tgtPassword;

// A blocking TCP socket can still read or write fewer bytes than requested.
static bool SendAll(SOCKET socket, const char* buffer, int length)
{
	int sent = 0;
	while (sent < length)
	{
		const int count = send(socket, buffer + sent, length - sent, 0);
		if (count <= 0)
			return false;
		sent += count;
	}
	return true;
}

static bool ReceiveExactly(SOCKET socket, char* buffer, int length)
{
	int received = 0;
	while (received < length)
	{
		const int count = recv(socket, buffer + received, length - received, 0);
		if (count <= 0)
			return false;
		received += count;
	}
	return true;
}

SOCKET SocksHelper::Connect()
{
	auto client = socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
	if (client == INVALID_SOCKET)
	{
		printf("[Redirector][SocksHelper::Connect] Create socket failed: %d\n", WSAGetLastError());
		return INVALID_SOCKET;
	}

	{
		int v6only = 0;
		if (setsockopt(client, IPPROTO_IPV6, IPV6_V6ONLY, (char*)&v6only, sizeof(v6only)) == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::Connect] Set socket option failed: %d\n", WSAGetLastError());

			closesocket(client);
			return INVALID_SOCKET;
		}
	}

	timeval timeout{};
	timeout.tv_sec = 4;

	if (!WSAConnectByNameW(client, (LPWSTR)tgtHost.c_str(), (LPWSTR)tgtPort.c_str(), NULL, NULL, NULL, NULL, &timeout, NULL))
	{
		printf("[Redirector][SocksHelper::Connect] Connect to remote server failed: %d\n", WSAGetLastError());

		closesocket(client);
		return INVALID_SOCKET;
	}

	// Game traffic often consists of small writes; avoid Nagle delays on the
	// connection from Redirector to the SOCKS server.
	BOOL noDelay = TRUE;
	if (setsockopt(client, IPPROTO_TCP, TCP_NODELAY, (char*)&noDelay, sizeof(noDelay)) == SOCKET_ERROR)
		printf("[Redirector][SocksHelper::Connect] Disable Nagle failed: %d\n", WSAGetLastError());

	{
		DWORD returned = 0;

		tcp_keepalive data = { 1, 120000, 10000 };
		WSAIoctl(client, SIO_KEEPALIVE_VALS, &data, sizeof(data), NULL, 0, &returned, NULL, NULL);
	}

	return client;
}

bool SocksHelper::Handshake(SOCKET client)
{
	char buffer[1024];
	memset(buffer, 0, sizeof(buffer));

	/* Client Hello */
	buffer[0] = 0x05;
	buffer[1] = 0x02;
	buffer[2] = 0x00;
	buffer[3] = 0x02;
	if (!SendAll(client, buffer, 4))
	{
		printf("[Redirector][SocksHelper::Handshake] Send client hello failed: %d\n", WSAGetLastError());
		return false;
	}

	/* Server Choice */
	if (!ReceiveExactly(client, buffer, 2))
	{
		printf("[Redirector][SocksHelper::Handshake] Receive server choice failed: %d\n", WSAGetLastError());
		return false;
	}

	/* Authentication */
	if (buffer[1] == 0x02)
	{
		memset(buffer, 0, sizeof(buffer));
		buffer[0] = 0x01;

		BYTE ulength = tgtUsername.length() & 0xff;
		BYTE plength = tgtPassword.length() & 0xff;

		/* Username */
		buffer[1] = 0x00;
		if (ulength != 0)
		{
			buffer[1] = ulength;
			memcpy(buffer + 1 + 1, tgtUsername.c_str(), ulength);
		}

		/* Password */
		buffer[1 + 1 + ulength] = 0x00;
		if (plength != 0)
		{
			buffer[1 + 1 + ulength] = plength;
			memcpy(buffer + 1 + 1 + ulength + 1, tgtPassword.c_str(), plength);
		}

		auto length = 1 + 1 + ulength + 1 + plength;
		if (!SendAll(client, buffer, length))
		{
			printf("[Redirector][SocksHelper::Handshake] Send authentication request failed: %d\n", WSAGetLastError());
			return false;
		}

		/* Server Response */
		if (!ReceiveExactly(client, buffer, 2))
		{
			printf("[Redirector][SocksHelper::Handshake] Receive server response failed: %d\n", WSAGetLastError());
			return false;
		}

		if (buffer[1] != 0x00)
		{
			puts("[Redirector][SocksHelper::Handshake] Authentication failed");
			return false;
		}
	}
	else if (buffer[1] != 0x00)
	{
		return false;
	}

	return true;
}

bool SocksHelper::SplitAddr(SOCKET client, PSOCKADDR_IN6 addr)
{
	char addrType;
	if (!ReceiveExactly(client, (char*)&addrType, 1))
	{
		printf("[Redirector][SocksHelper::SplitAddr] Read address type failed: %d\n", WSAGetLastError());
		return false;
	}

	if (addrType == 0x01)
	{
		auto ipv4 = (PSOCKADDR_IN)addr;
		ipv4->sin_family = AF_INET;

		if (!ReceiveExactly(client, (char*)&ipv4->sin_addr, 4))
		{
			printf("[Redirector][SocksHelper::SplitAddr] Read IPv4 address failed: %d\n", WSAGetLastError());
			return false;
		}

		if (!ReceiveExactly(client, (char*)&ipv4->sin_port, 2))
		{
			printf("[Redirector][SocksHelper::SplitAddr] Read IPv4 port failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else if (addrType == 0x04)
	{
		addr->sin6_family = AF_INET6;

		if (!ReceiveExactly(client, (char*)&addr->sin6_addr, 16))
		{
			printf("[Redirector][SocksHelper::SplitAddr] Read IPv6 address failed: %d\n", WSAGetLastError());
			return false;
		}

		if (!ReceiveExactly(client, (char*)&addr->sin6_port, 2))
		{
			printf("[Redirector][SocksHelper::SplitAddr] Read IPv6 port failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else
	{
		printf("[Redirector][SocksHelper::SplitAddr] Unsupported address family: %d\n", addrType);
		return false;
	}

	return true;
}

SocksHelper::TCP::~TCP()
{
	if (this->tcpSocket != INVALID_SOCKET)
	{
		closesocket(this->tcpSocket);

		this->tcpSocket = INVALID_SOCKET;
	}
}

bool SocksHelper::TCP::Connect(PSOCKADDR_IN6 target)
{
	this->tcpSocket = SocksHelper::Connect();
	if (this->tcpSocket == INVALID_SOCKET)
		return false;

	if (!SocksHelper::Handshake(this->tcpSocket))
		return false;

	/* Connect Request */
	if (target->sin6_family == AF_INET)
	{
		char buffer[10]{};
		buffer[0] = 0x05;
		buffer[1] = 0x01;
		buffer[2] = 0x00;
		buffer[3] = 0x01;

		auto addr = (PSOCKADDR_IN)target;
		memcpy(buffer + 4, &addr->sin_addr, 4);
		memcpy(buffer + 8, &addr->sin_port, 2);

		if (!SendAll(this->tcpSocket, buffer, 10))
		{
			printf("[Redirector][SocksHelper::TCP::Connect] Send connect request failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else
	{
		char buffer[22]{};
		buffer[0] = 0x05;
		buffer[1] = 0x01;
		buffer[2] = 0x00;
		buffer[3] = 0x04;

		auto addr = target;
		memcpy(buffer + 4, &addr->sin6_addr, 16);
		memcpy(buffer + 20, &addr->sin6_port, 2);

		if (!SendAll(this->tcpSocket, buffer, sizeof(buffer)))
		{
			printf("[Redirector][SocksHelper::TCP::Connect] Send connect request failed: %d\n", WSAGetLastError());
			return false;
		}
	}

	/* Server Response */
	char buffer[3];
	if (!ReceiveExactly(this->tcpSocket, buffer, 3))
	{
		printf("[Redirector][SocksHelper::TCP::Connect] Receive server response failed: %d\n", WSAGetLastError());
		return false;
	}

	if (buffer[1] != 0x00)
		return false;

	SOCKADDR_IN6 addr;
	return SocksHelper::SplitAddr(this->tcpSocket, &addr);
}

int SocksHelper::TCP::Send(const char* buffer, int length)
{
	if (this->tcpSocket != INVALID_SOCKET)
		return SendAll(this->tcpSocket, buffer, length) ? length : SOCKET_ERROR;

	return SOCKET_ERROR;
}

int SocksHelper::TCP::Read(char* buffer, int length)
{
	if (this->tcpSocket != INVALID_SOCKET)
		return recv(this->tcpSocket, buffer, length, 0);

	return SOCKET_ERROR;
}

SocksHelper::UDP::~UDP()
{
	Stop();
}

void SocksHelper::UDP::Stop()
{
	stopping = true;

	const SOCKET tcp = tcpSocket.exchange(INVALID_SOCKET);
	if (tcp != INVALID_SOCKET)
	{
		shutdown(tcp, SD_BOTH);
		closesocket(tcp);
	}
	const SOCKET udp = udpSocket.exchange(INVALID_SOCKET);
	if (udp != INVALID_SOCKET)
		closesocket(udp);
}

void SocksHelper::UDP::Run(std::shared_ptr<UDP> remote)
{
	// SOCKS5 UDP ASSOCIATE keeps its control TCP connection open; it does
	// not echo incoming control bytes. Observe closure to end the session.
	char buffer[1];
	while (!remote->stopping)
	{
		const SOCKET tcp = remote->tcpSocket;
		if (tcp == INVALID_SOCKET || recv(tcp, buffer, sizeof(buffer), 0) <= 0)
			break;
	}
	remote->Stop();
}

bool SocksHelper::UDP::Associate()
{
	this->tcpSocket = SocksHelper::Connect();
	if (this->tcpSocket == INVALID_SOCKET)
		return false;

	if (!SocksHelper::Handshake(this->tcpSocket))
		return false;

	char buffer[10]{};
	buffer[0] = 0x05;
	buffer[1] = 0x03;
	buffer[3] = 0x01;

	if (!SendAll(this->tcpSocket, buffer, 10))
	{
		printf("[Redirector][SocksHelper::UDP::Associate] Send udp associate request failed: %d\n", WSAGetLastError());
		return false;
	}

	if (!ReceiveExactly(this->tcpSocket, buffer, 3))
	{
		printf("[Redirector][SocksHelper::UDP::Associate] Receive udp associate response failed: %d\n", WSAGetLastError());
		return false;
	}

	if (buffer[1] != 0x00)
	{
		printf("[Redirector][SocksHelper::UDP::Associate] UDP associate failed: %d\n", buffer[1]);
		return false;
	}

	return SocksHelper::SplitAddr(this->tcpSocket, &this->address);
}

bool SocksHelper::UDP::CreateUDP()
{
	if (this->address.sin6_family == AF_INET)
	{
		this->udpSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (this->udpSocket == INVALID_SOCKET)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Create IPv4 socket failed: %d\n", WSAGetLastError());
			return false;
		}

		SOCKADDR_IN bindaddr;
		memset(&bindaddr, 0, sizeof(SOCKADDR_IN));
		bindaddr.sin_family = AF_INET;

		if (bind(this->udpSocket, (PSOCKADDR)&bindaddr, sizeof(SOCKADDR_IN)) == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Listen IPv4 socket failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else
	{
		this->udpSocket = socket(AF_INET6, SOCK_DGRAM, IPPROTO_UDP);
		if (this->udpSocket == INVALID_SOCKET)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Create IPv6 socket failed: %d\n", WSAGetLastError());
			return false;
		}

		SOCKADDR_IN6 bindaddr;
		memset(&bindaddr, 0, sizeof(SOCKADDR_IN6));
		bindaddr.sin6_family = AF_INET6;

		if (bind(this->udpSocket, (PSOCKADDR)&bindaddr, sizeof(SOCKADDR_IN6)) == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Listen IPv6 socket failed: %d\n", WSAGetLastError());
			return false;
		}
	}

	// An ICMP error for one game destination must not break the shared UDP relay.
	BOOL reportConnectionReset = FALSE;
	DWORD returned = 0;
	WSAIoctl(this->udpSocket, SIO_UDP_CONNRESET, &reportConnectionReset,
		sizeof(reportConnectionReset), NULL, 0, &returned, NULL, NULL);

	thread(SocksHelper::UDP::Run, shared_from_this()).detach();
	return true;
}

int SocksHelper::UDP::Send(PSOCKADDR_IN6 target, const char* buffer, int length)
{
	if (this->udpSocket == INVALID_SOCKET)
		return SOCKET_ERROR;

	if (target->sin6_family != AF_INET && target->sin6_family != AF_INET6)
		return SOCKET_ERROR;

	const int headerLength = target->sin6_family == AF_INET ? 10 : 22;
	if (length < 0 || length > 65535 - headerLength)
		return SOCKET_ERROR;

	const int dataLength = headerLength + length;
	this->sendBuffer.resize(dataLength);
	auto data = this->sendBuffer.data();
	data[0] = data[1] = data[2] = 0;
	data[3] = (target->sin6_family == AF_INET) ? 0x01 : 0x04;

	if (target->sin6_family == AF_INET)
	{
		auto ipv4 = (PSOCKADDR_IN)target;

		memcpy(data + 4, &ipv4->sin_addr, 4);
		memcpy(data + 8, &ipv4->sin_port, 2);
	}
	else
	{
		memcpy(data + 4, &target->sin6_addr, 16);
		memcpy(data + 20, &target->sin6_port, 2);
	}

	if (length > 0)
		memcpy(data + headerLength, buffer, length);

	if (sendto(this->udpSocket, data, dataLength, 0, (PSOCKADDR)&this->address, (this->address.sin6_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6))) != dataLength)
	{
		printf("[Redirector][SocksHelper::UDP::Send] Send packet failed: %d\n", WSAGetLastError());
		return SOCKET_ERROR;
	}

	return length;
}

int SocksHelper::UDP::Read(PSOCKADDR_IN6 target, char* buffer, int length, PTIMEVAL timeout)
{
	if (this->udpSocket == INVALID_SOCKET || length < 4)
		return SOCKET_ERROR;

	const auto deadline = timeout == NULL ? chrono::steady_clock::time_point{} :
		chrono::steady_clock::now() + chrono::seconds(timeout->tv_sec) + chrono::microseconds(timeout->tv_usec);

	for (;;)
	{
		if (timeout != NULL)
		{
			const auto remaining = chrono::duration_cast<chrono::microseconds>(deadline - chrono::steady_clock::now()).count();
			if (remaining <= 0)
				return 0;

			timeval wait{};
			wait.tv_sec = static_cast<long>(remaining / 1000000);
			wait.tv_usec = static_cast<long>(remaining % 1000000);
			fd_set fds;
			FD_ZERO(&fds);
			FD_SET(this->udpSocket, &fds);

			const int ready = select(NULL, &fds, NULL, NULL, &wait);
			if (ready == 0 || ready == SOCKET_ERROR)
				return ready;
		}

		const int size = recvfrom(this->udpSocket, buffer, length, 0, NULL, NULL);
		if (size == SOCKET_ERROR)
		{
			// A truncated or malformed datagram does not invalidate the association.
			if (WSAGetLastError() == WSAEMSGSIZE)
				continue;
			return SOCKET_ERROR;
		}
		if (size < 4 || buffer[0] != 0 || buffer[1] != 0 || buffer[2] != 0)
			continue;

		SOCKADDR_IN6 addr{};
		int headerLength;
		if (buffer[3] == 0x01)
		{
			if (size < 10)
				continue;
			auto ipv4 = (PSOCKADDR_IN)&addr;
			ipv4->sin_family = AF_INET;

			memcpy(&ipv4->sin_addr, buffer + 4, 4);
			memcpy(&ipv4->sin_port, buffer + 8, 2);
			headerLength = 10;
		}
		else if (buffer[3] == 0x04)
		{
			if (size < 22)
				continue;
			addr.sin6_family = AF_INET6;

			memcpy(&addr.sin6_addr, buffer + 4, 16);
			memcpy(&addr.sin6_port, buffer + 20, 2);
			headerLength = 22;
		}
		else
		{
			continue;
		}

		memmove(buffer, buffer + headerLength, size - headerLength);
		if (target != NULL)
			memcpy(target, &addr, sizeof(SOCKADDR_IN6));

		return size - headerLength;
	}
}

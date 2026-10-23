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

	// Bound SOCKS negotiation. These timeouts are removed after negotiation,
	// since established TCP streams and UDP control channels may be idle.
	DWORD handshakeTimeoutMs = 2000;
	setsockopt(client, SOL_SOCKET, SO_RCVTIMEO, (char*)&handshakeTimeoutMs, sizeof(handshakeTimeoutMs));
	setsockopt(client, SOL_SOCKET, SO_SNDTIMEO, (char*)&handshakeTimeoutMs, sizeof(handshakeTimeoutMs));

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
	if (tgtUsername.size() > 255 || tgtPassword.size() > 255)
	{
		puts("[Redirector][SocksHelper::Handshake] SOCKS credentials exceed the 255-byte limit");
		return false;
	}
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
	if (buffer[0] != 0x05)
		return false;
	if (buffer[1] == 0x02)
	{
		memset(buffer, 0, sizeof(buffer));
		buffer[0] = 0x01;

		BYTE ulength = static_cast<BYTE>(tgtUsername.length());
		BYTE plength = static_cast<BYTE>(tgtPassword.length());

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

		if (buffer[0] != 0x01 || buffer[1] != 0x00)
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
	memset(addr, 0, sizeof(SOCKADDR_IN6));
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
	else if (addrType == 0x03)
	{
		unsigned char hostnameLength;
		if (!ReceiveExactly(client, (char*)&hostnameLength, 1) || hostnameLength == 0)
			return false;

		char hostname[256]{};
		unsigned short port;
		if (!ReceiveExactly(client, hostname, hostnameLength) ||
			!ReceiveExactly(client, (char*)&port, 2))
			return false;

		addrinfo hints{};
		hints.ai_family = AF_UNSPEC;
		hints.ai_socktype = SOCK_DGRAM;
		addrinfo* resolved = nullptr;
		if (getaddrinfo(hostname, nullptr, &hints, &resolved) != 0 || resolved == nullptr)
		{
			puts("[Redirector][SocksHelper::SplitAddr] Resolve SOCKS reply hostname failed");
			return false;
		}
		for (auto current = resolved; current != nullptr; current = current->ai_next)
		{
			if (current->ai_family != AF_INET && current->ai_family != AF_INET6)
				continue;
			memcpy(addr, current->ai_addr, current->ai_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6));
			break;
		}
		freeaddrinfo(resolved);
		if (addr->sin6_family == AF_INET)
			((PSOCKADDR_IN)addr)->sin_port = port;
		else if (addr->sin6_family == AF_INET6)
			addr->sin6_port = port;
		else
			return false;
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
	if (!SocksHelper::SplitAddr(this->tcpSocket, &addr))
		return false;
	DWORD noTimeout = 0;
	setsockopt(this->tcpSocket, SOL_SOCKET, SO_RCVTIMEO, (char*)&noTimeout, sizeof(noTimeout));
	setsockopt(this->tcpSocket, SOL_SOCKET, SO_SNDTIMEO, (char*)&noTimeout, sizeof(noTimeout));
	return true;
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

	if (!SocksHelper::SplitAddr(this->tcpSocket, &this->address))
		return false;
	// RFC 1928 permits an unspecified BND.ADDR. In that case the UDP
	// relay lives at the same host as the SOCKS TCP control connection.
	const bool unspecifiedV4 = this->address.sin6_family == AF_INET &&
		((PSOCKADDR_IN)&this->address)->sin_addr.s_addr == INADDR_ANY;
	const bool unspecifiedV6 = this->address.sin6_family == AF_INET6 &&
		IN6_IS_ADDR_UNSPECIFIED(&this->address.sin6_addr);
	if (unspecifiedV4 || unspecifiedV6)
	{
		SOCKADDR_IN6 peer{};
		int peerLength = sizeof(peer);
		if (getpeername(this->tcpSocket, (PSOCKADDR)&peer, &peerLength) == SOCKET_ERROR)
			return false;

		const USHORT port = this->address.sin6_family == AF_INET ?
			((PSOCKADDR_IN)&this->address)->sin_port : this->address.sin6_port;
		if (peer.sin6_family == AF_INET6 && IN6_IS_ADDR_V4MAPPED(&peer.sin6_addr))
		{
			SOCKADDR_IN mapped{};
			mapped.sin_family = AF_INET;
			mapped.sin_port = port;
			memcpy(&mapped.sin_addr, &peer.sin6_addr.u.Byte[12], sizeof(mapped.sin_addr));
			memcpy(&this->address, &mapped, sizeof(mapped));
		}
		else if (peer.sin6_family == AF_INET6)
		{
			this->address = peer;
			this->address.sin6_port = port;
		}
		else if (peer.sin6_family == AF_INET)
		{
			((PSOCKADDR_IN)&peer)->sin_port = port;
			this->address = peer;
		}
		else
			return false;
	}
	DWORD noTimeout = 0;
	setsockopt(this->tcpSocket, SOL_SOCKET, SO_RCVTIMEO, (char*)&noTimeout, sizeof(noTimeout));
	setsockopt(this->tcpSocket, SOL_SOCKET, SO_SNDTIMEO, (char*)&noTimeout, sizeof(noTimeout));
	return true;
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
	// The SOCKS5 envelope is part of the UDP payload sent to the proxy.
	// Keep the complete datagram within the IPv4 UDP payload limit, which is
	// also safe when the proxy address is IPv6.
	constexpr int maxUdpPayloadLength = 65507;
	if (length < 0 || length > maxUdpPayloadLength - headerLength)
		return SOCKET_ERROR;

	char header[22]{};
	header[3] = (target->sin6_family == AF_INET) ? 0x01 : 0x04;

	if (target->sin6_family == AF_INET)
	{
		auto ipv4 = (PSOCKADDR_IN)target;

		memcpy(header + 4, &ipv4->sin_addr, 4);
		memcpy(header + 8, &ipv4->sin_port, 2);
	}
	else
	{
		memcpy(header + 4, &target->sin6_addr, 16);
		memcpy(header + 20, &target->sin6_port, 2);
	}

	// Send the SOCKS5 header and game payload as one datagram without copying
	// the payload into a temporary contiguous buffer on every packet.
	WSABUF buffers[2] = {
		{ static_cast<ULONG>(headerLength), header },
		{ static_cast<ULONG>(length), const_cast<char*>(buffer) }
	};
	DWORD bytesSent = 0;
	const DWORD bufferCount = length > 0 ? 2 : 1;
	const int dataLength = headerLength + length;
	const int sendResult = WSASendTo(this->udpSocket, buffers, bufferCount, &bytesSent, 0,
		(PSOCKADDR)&this->address,
		(this->address.sin6_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6)),
		NULL, NULL);
	if (sendResult == SOCKET_ERROR)
	{
		printf("[Redirector][SocksHelper::UDP::Send] Send packet failed: %d\n", WSAGetLastError());
		return SOCKET_ERROR;
	}
	if (bytesSent != static_cast<DWORD>(dataLength))
	{
		printf("[Redirector][SocksHelper::UDP::Send] Incomplete datagram: %lu/%d\n", bytesSent, dataLength);
		return SOCKET_ERROR;
	}

	return length;
}

int SocksHelper::UDP::Read(PSOCKADDR_IN6 target, char* buffer, int length, int* payloadOffset, PTIMEVAL timeout)
{
	if (this->udpSocket == INVALID_SOCKET || length < 4)
		return SOCKET_ERROR;
	if (payloadOffset != NULL)
		*payloadOffset = 0;

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

		if (target != NULL)
			memcpy(target, &addr, sizeof(SOCKADDR_IN6));
		if (payloadOffset != NULL)
			*payloadOffset = headerLength;

		return size - headerLength;
	}
}

#include "SocksHelper.h"

#include "Utils.h"

extern wstring tgtHost;
extern wstring tgtPort;
extern string tgtUsername;
extern string tgtPassword;

constexpr DWORD SocksHandshakeTimeoutMs = 4000;
constexpr long long UdpRetryCooldownMs = 2000;

static void ClearSocketTimeout(SOCKET socket)
{
	const DWORD timeout = 0;
	setsockopt(socket, SOL_SOCKET, SO_RCVTIMEO, (const char*)&timeout, sizeof(timeout));
	setsockopt(socket, SOL_SOCKET, SO_SNDTIMEO, (const char*)&timeout, sizeof(timeout));
}

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

	// WSAConnectByName has a connect timeout, but SOCKS negotiation uses
	// blocking recv calls. Bound both directions so an unresponsive proxy
	// cannot pin a redirector callback thread indefinitely.
	const DWORD ioTimeout = SocksHandshakeTimeoutMs;
	setsockopt(client, SOL_SOCKET, SO_RCVTIMEO, (const char*)&ioTimeout, sizeof(ioTimeout));
	setsockopt(client, SOL_SOCKET, SO_SNDTIMEO, (const char*)&ioTimeout, sizeof(ioTimeout));

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

		if (tgtUsername.length() > 255 || tgtPassword.length() > 255)
		{
			puts("[Redirector][SocksHelper::Handshake] SOCKS5 credentials are limited to 255 bytes");
			return false;
		}

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
	memset(addr, 0, sizeof(SOCKADDR_IN6));
	unsigned char addrType;
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
	else if (addrType == 0x03)
	{
		unsigned char hostLength = 0;
		if (!ReceiveExactly(client, (char*)&hostLength, 1) || hostLength == 0)
			return false;

		string host(hostLength, '\0');
		if (!ReceiveExactly(client, host.data(), hostLength))
			return false;

		USHORT port = 0;
		if (!ReceiveExactly(client, (char*)&port, sizeof(port)))
			return false;

		addrinfo hints{};
		hints.ai_socktype = SOCK_DGRAM;
		addrinfo* resolved = NULL;
		if (getaddrinfo(host.c_str(), NULL, &hints, &resolved) != 0)
			return false;

		bool copied = false;
		for (auto item = resolved; item != NULL; item = item->ai_next)
		{
			if (item->ai_family == AF_INET)
			{
				memcpy(addr, item->ai_addr, sizeof(SOCKADDR_IN));
				((PSOCKADDR_IN)addr)->sin_port = port;
				copied = true;
				break;
			}
			if (item->ai_family == AF_INET6)
			{
				memcpy(addr, item->ai_addr, sizeof(SOCKADDR_IN6));
				addr->sin6_port = port;
				copied = true;
				break;
			}
		}

		freeaddrinfo(resolved);
		if (!copied)
			return false;
	}
	else
	{
		printf("[Redirector][SocksHelper::SplitAddr] Unsupported address family: %d\n", addrType);
		return false;
	}

	return true;
}

static bool IsWildcardAddress(PSOCKADDR_IN6 address)
{
	if (address->sin6_family == AF_INET)
		return ((PSOCKADDR_IN)address)->sin_addr.S_un.S_addr == htonl(INADDR_ANY);

	return address->sin6_family == AF_INET6 && IN6_IS_ADDR_UNSPECIFIED(&address->sin6_addr);
}

static bool ReplaceWildcardAddressFromPeer(SOCKET client, PSOCKADDR_IN6 address)
{
	SOCKADDR_STORAGE peer{};
	int peerLength = sizeof(peer);
	if (getpeername(client, (PSOCKADDR)&peer, &peerLength) == SOCKET_ERROR)
		return false;

	const auto port = address->sin6_family == AF_INET
		? ((PSOCKADDR_IN)address)->sin_port
		: address->sin6_port;

	if (address->sin6_family == AF_INET && peer.ss_family == AF_INET)
	{
		((PSOCKADDR_IN)address)->sin_addr = ((PSOCKADDR_IN)&peer)->sin_addr;
		((PSOCKADDR_IN)address)->sin_port = port;
		return true;
	}

	if (address->sin6_family == AF_INET6 && peer.ss_family == AF_INET6)
	{
		address->sin6_addr = ((PSOCKADDR_IN6)&peer)->sin6_addr;
		address->sin6_port = port;
		return true;
	}

	// A dual-mode socket may report an IPv4 peer as an IPv4-mapped IPv6
	// address. Preserve the family returned by SOCKS and copy only the IP.
	if (address->sin6_family == AF_INET && peer.ss_family == AF_INET6 &&
		IN6_IS_ADDR_V4MAPPED(&((PSOCKADDR_IN6)&peer)->sin6_addr))
	{
		memcpy(&((PSOCKADDR_IN)address)->sin_addr,
			((PSOCKADDR_IN6)&peer)->sin6_addr.u.Byte + 12, sizeof(IN_ADDR));
		((PSOCKADDR_IN)address)->sin_port = port;
		return true;
	}

	if (address->sin6_family == AF_INET6 && peer.ss_family == AF_INET)
	{
		memset(&address->sin6_addr, 0, sizeof(address->sin6_addr));
		address->sin6_addr.u.Byte[10] = 0xff;
		address->sin6_addr.u.Byte[11] = 0xff;
		memcpy(address->sin6_addr.u.Byte + 12,
			&((PSOCKADDR_IN)&peer)->sin_addr, sizeof(IN_ADDR));
		address->sin6_port = port;
		return true;
	}

	return false;
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
	const bool connected = SocksHelper::SplitAddr(this->tcpSocket, &addr);
	if (connected)
		ClearSocketTimeout(this->tcpSocket);
	return connected;
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
	retryAfterMs = chrono::duration_cast<chrono::milliseconds>(
		chrono::steady_clock::now().time_since_epoch()).count() + UdpRetryCooldownMs;

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

	if (IsWildcardAddress(&this->address) && !ReplaceWildcardAddressFromPeer(this->tcpSocket, &this->address))
	{
		puts("[Redirector][SocksHelper::UDP::Associate] SOCKS server returned an unusable wildcard address");
		return false;
	}

	ClearSocketTimeout(this->tcpSocket);
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

bool SocksHelper::UDP::CanRetry() const
{
	const auto now = chrono::duration_cast<chrono::milliseconds>(
		chrono::steady_clock::now().time_since_epoch()).count();
	return now >= retryAfterMs.load();
}

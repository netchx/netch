#include "SocksHelper.h"

#include "Utils.h"

constexpr DWORD SocksHandshakeTimeoutMs = 4000;
constexpr long long UdpRetryCooldownMs = 2000;

namespace
{
	mutex g_udpIoLock;
	condition_variable g_udpIoCv;
	int g_activeUdpIoOps = 0;

	void AcquireUdpIoOp()
	{
		lock_guard<mutex> lg(g_udpIoLock);
		++g_activeUdpIoOps;
	}

	void ReleaseUdpIoOp()
	{
		lock_guard<mutex> lg(g_udpIoLock);
		if (g_activeUdpIoOps > 0)
			--g_activeUdpIoOps;
		if (g_activeUdpIoOps == 0)
			g_udpIoCv.notify_all();
	}
}

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
	auto client = WSASocketW(AF_INET6, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
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

	wstring hostCopy;
	wstring portCopy;
	{
		shared_lock<shared_mutex> sl(configLock);
		hostCopy = tgtHost;
		portCopy = tgtPort;
	}

	if (!WSAConnectByNameW(client, const_cast<LPWSTR>(hostCopy.c_str()), const_cast<LPWSTR>(portCopy.c_str()), NULL, NULL, NULL, NULL, &timeout, NULL))
	{
		printf("[Redirector][SocksHelper::Connect] Connect to remote server failed: %d\n", WSAGetLastError());

		closesocket(client);
		return INVALID_SOCKET;
	}

	// Required after WSAConnectByNameW so shutdown()/getpeername() see full connected state.
	setsockopt(client, SOL_SOCKET, SO_UPDATE_CONNECT_CONTEXT, NULL, 0);

	// WSAConnectByName has a connect timeout, but SOCKS negotiation uses
	// blocking recv calls. Bound both directions so an unresponsive proxy
	// cannot pin a worker thread indefinitely.
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
	string userCopy;
	string passCopy;
	{
		shared_lock<shared_mutex> sl(configLock);
		userCopy = tgtUsername;
		passCopy = tgtPassword;
	}

	if (userCopy.length() > 255 || passCopy.length() > 255)
	{
		puts("[Redirector][SocksHelper::Handshake] SOCKS5 credentials are limited to 255 bytes");
		return false;
	}

	const bool hasAuth = !userCopy.empty() || !passCopy.empty();
	char buffer[1024];
	memset(buffer, 0, sizeof(buffer));

	/* Client Hello */
	buffer[0] = 0x05;
	buffer[1] = hasAuth ? 0x02 : 0x01;
	buffer[2] = 0x00;
	buffer[3] = 0x02;
	if (!SendAll(client, buffer, hasAuth ? 4 : 3))
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

	if (buffer[0] != 0x05)
		return false;

	/* Authentication */
	if (buffer[1] == 0x02)
	{
		if (!hasAuth)
			return false;

		memset(buffer, 0, sizeof(buffer));
		buffer[0] = 0x01;

		BYTE ulength = static_cast<BYTE>(userCopy.length());
		BYTE plength = static_cast<BYTE>(passCopy.length());

		/* Username */
		buffer[1] = 0x00;
		if (ulength != 0)
		{
			buffer[1] = ulength;
			memcpy(buffer + 1 + 1, userCopy.c_str(), ulength);
		}

		/* Password */
		buffer[1 + 1 + ulength] = 0x00;
		if (plength != 0)
		{
			buffer[1 + 1 + ulength] = plength;
			memcpy(buffer + 1 + 1 + ulength + 1, passCopy.c_str(), plength);
		}

		auto length = 1 + 1 + ulength + 1 + plength;
		if (!SendAll(client, buffer, static_cast<int>(length)))
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

bool SocksHelper::SplitAddr(SOCKET client, PSOCKADDR_IN6 addr, bool resolveDomain)
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

		if (!resolveDomain)
			return true;

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

bool SocksHelper::UnpackSocks5UdpPacket(const char* buffer, int length, PSOCKADDR_IN6 target, int* payloadOffset, int* payloadLength)
{
	if (buffer == nullptr || length < 4 || buffer[0] != 0 || buffer[1] != 0 || buffer[2] != 0)
		return false;

	SOCKADDR_IN6 addr{};
	int headerLength = 0;
	if (buffer[3] == 0x01)
	{
		if (length < 10)
			return false;
		auto ipv4 = (PSOCKADDR_IN)&addr;
		ipv4->sin_family = AF_INET;

		memcpy(&ipv4->sin_addr, buffer + 4, 4);
		memcpy(&ipv4->sin_port, buffer + 8, 2);
		headerLength = 10;
	}
	else if (buffer[3] == 0x04)
	{
		if (length < 22)
			return false;
		addr.sin6_family = AF_INET6;

		memcpy(&addr.sin6_addr, buffer + 4, 16);
		memcpy(&addr.sin6_port, buffer + 20, 2);
		headerLength = 22;
	}
	else
	{
		return false;
	}

	if (target != nullptr)
		memcpy(target, &addr, sizeof(SOCKADDR_IN6));
	if (payloadOffset != nullptr)
		*payloadOffset = headerLength;
	if (payloadLength != nullptr)
		*payloadLength = length - headerLength;

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
	Close();
}

void SocksHelper::TCP::Close()
{
	lock_guard<mutex> lg(ioLock_);
	const SOCKET s = tcpSocket.exchange(INVALID_SOCKET);
	if (s != INVALID_SOCKET)
	{
		shutdown(s, SD_BOTH);
		closesocket(s);
	}
}

bool SocksHelper::TCP::Connect(PSOCKADDR_IN6 target)
{
	const SOCKET s = SocksHelper::Connect();
	if (s == INVALID_SOCKET)
		return false;

	tcpSocket.store(s);

	if (!SocksHelper::Handshake(s))
	{
		Close();
		return false;
	}

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

		if (!SendAll(s, buffer, 10))
		{
			printf("[Redirector][SocksHelper::TCP::Connect] Send connect request failed: %d\n", WSAGetLastError());
			Close();
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

		if (!SendAll(s, buffer, sizeof(buffer)))
		{
			printf("[Redirector][SocksHelper::TCP::Connect] Send connect request failed: %d\n", WSAGetLastError());
			Close();
			return false;
		}
	}

	/* Server Response */
	char buffer[3];
	if (!ReceiveExactly(s, buffer, 3))
	{
		printf("[Redirector][SocksHelper::TCP::Connect] Receive server response failed: %d\n", WSAGetLastError());
		Close();
		return false;
	}

	if (buffer[0] != 0x05 || buffer[1] != 0x00)
	{
		Close();
		return false;
	}

	SOCKADDR_IN6 addr;
	const bool connected = SocksHelper::SplitAddr(s, &addr, false);
	if (connected)
		ClearSocketTimeout(s);
	else
		Close();
	return connected;
}

int SocksHelper::TCP::Send(const char* buffer, int length)
{
	lock_guard<mutex> lg(ioLock_);
	const SOCKET s = tcpSocket.load();
	if (s != INVALID_SOCKET)
		return SendAll(s, buffer, length) ? length : SOCKET_ERROR;

	return SOCKET_ERROR;
}

int SocksHelper::TCP::Read(char* buffer, int length)
{
	lock_guard<mutex> lg(ioLock_);
	const SOCKET s = tcpSocket.load();
	if (s != INVALID_SOCKET)
		return recv(s, buffer, length, 0);

	return SOCKET_ERROR;
}

SocksHelper::UDP::~UDP()
{
	Stop();
}

void SocksHelper::UDP::Stop()
{
	stopping = true;
	ready = false;
	retryAfterMs = chrono::duration_cast<chrono::milliseconds>(
		chrono::steady_clock::now().time_since_epoch()).count() + UdpRetryCooldownMs;

	lock_guard<mutex> lg(ioLock_);
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

void SocksHelper::UDP::NotifyClosed()
{
	if (!closeNotified_.exchange(true) && onClose_)
		onClose_();
}

static bool IsSameUdpEndpoint(const SOCKADDR_IN6& from, const SOCKADDR_IN6& expected)
{
	if (expected.sin6_family == AF_INET)
	{
		const auto* exp4 = reinterpret_cast<const SOCKADDR_IN*>(&expected);
		if (from.sin6_family == AF_INET)
		{
			const auto* from4 = reinterpret_cast<const SOCKADDR_IN*>(&from);
			return from4->sin_port == exp4->sin_port &&
				from4->sin_addr.S_un.S_addr == exp4->sin_addr.S_un.S_addr;
		}
		if (from.sin6_family == AF_INET6 && IN6_IS_ADDR_V4MAPPED(&from.sin6_addr))
		{
			return from.sin6_port == exp4->sin_port &&
				memcmp(from.sin6_addr.u.Byte + 12, &exp4->sin_addr, sizeof(IN_ADDR)) == 0;
		}
		return false;
	}

	if (expected.sin6_family == AF_INET6)
	{
		if (from.sin6_family == AF_INET6)
		{
			return from.sin6_port == expected.sin6_port &&
				memcmp(&from.sin6_addr, &expected.sin6_addr, sizeof(IN6_ADDR)) == 0;
		}
		if (from.sin6_family == AF_INET && IN6_IS_ADDR_V4MAPPED(&expected.sin6_addr))
		{
			const auto* from4 = reinterpret_cast<const SOCKADDR_IN*>(&from);
			return from4->sin_port == expected.sin6_port &&
				memcmp(expected.sin6_addr.u.Byte + 12, &from4->sin_addr, sizeof(IN_ADDR)) == 0;
		}
	}

	return false;
}

bool SocksHelper::UDP::Associate()
{
	const SOCKET s = SocksHelper::Connect();
	if (s == INVALID_SOCKET)
		return false;
	tcpSocket.store(s);

	if (!SocksHelper::Handshake(s))
		return false;

	char buffer[10]{};
	buffer[0] = 0x05;
	buffer[1] = 0x03;
	buffer[3] = 0x01;

	if (!SendAll(s, buffer, 10))
	{
		printf("[Redirector][SocksHelper::UDP::Associate] Send udp associate request failed: %d\n", WSAGetLastError());
		return false;
	}

	if (!ReceiveExactly(s, buffer, 3))
	{
		printf("[Redirector][SocksHelper::UDP::Associate] Receive udp associate response failed: %d\n", WSAGetLastError());
		return false;
	}

	if (buffer[0] != 0x05 || buffer[1] != 0x00)
	{
		printf("[Redirector][SocksHelper::UDP::Associate] UDP associate failed: %d\n", buffer[1]);
		return false;
	}

	if (!SocksHelper::SplitAddr(s, &this->address))
		return false;

	if (IsWildcardAddress(&this->address) && !ReplaceWildcardAddressFromPeer(s, &this->address))
	{
		puts("[Redirector][SocksHelper::UDP::Associate] SOCKS server returned an unusable wildcard address");
		return false;
	}

	ClearSocketTimeout(s);
	return true;
}

bool SocksHelper::UDP::CreateUDP()
{
	SOCKET u = INVALID_SOCKET;
	if (this->address.sin6_family == AF_INET)
	{
		u = WSASocketW(AF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, WSA_FLAG_OVERLAPPED);
		if (u == INVALID_SOCKET)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Create IPv4 socket failed: %d\n", WSAGetLastError());
			return false;
		}

		SOCKADDR_IN bindaddr;
		memset(&bindaddr, 0, sizeof(SOCKADDR_IN));
		bindaddr.sin_family = AF_INET;

		if (::bind(u, (PSOCKADDR)&bindaddr, sizeof(SOCKADDR_IN)) == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Listen IPv4 socket failed: %d\n", WSAGetLastError());
			closesocket(u);
			return false;
		}
	}
	else
	{
		u = WSASocketW(AF_INET6, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, WSA_FLAG_OVERLAPPED);
		if (u == INVALID_SOCKET)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Create IPv6 socket failed: %d\n", WSAGetLastError());
			return false;
		}

		int v6only = 0;
		if (setsockopt(u, IPPROTO_IPV6, IPV6_V6ONLY, (const char*)&v6only, sizeof(v6only)) == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Set IPV6_V6ONLY=0 failed: %d\n", WSAGetLastError());
			closesocket(u);
			return false;
		}

		SOCKADDR_IN6 bindaddr;
		memset(&bindaddr, 0, sizeof(SOCKADDR_IN6));
		bindaddr.sin6_family = AF_INET6;

		if (::bind(u, (PSOCKADDR)&bindaddr, sizeof(SOCKADDR_IN6)) == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::UDP::CreateUDP] Listen IPv6 socket failed: %d\n", WSAGetLastError());
			closesocket(u);
			return false;
		}
	}

	// Preserve 1 MiB UDP buffers so game/voice bursts are not silently dropped.
	const int udpBufferSize = 1024 * 1024;
	setsockopt(u, SOL_SOCKET, SO_RCVBUF, (const char*)&udpBufferSize, sizeof(udpBufferSize));
	setsockopt(u, SOL_SOCKET, SO_SNDBUF, (const char*)&udpBufferSize, sizeof(udpBufferSize));

	// An ICMP error for one game destination must not break the shared UDP relay.
	BOOL reportConnectionReset = FALSE;
	DWORD returned = 0;
	WSAIoctl(u, SIO_UDP_CONNRESET, &reportConnectionReset,
		sizeof(reportConnectionReset), NULL, 0, &returned, NULL, NULL);

	this->udpSocket.store(u);
	return true;
}

bool SocksHelper::UDP::StartAsync(PacketCallback onPacket, CloseCallback onClose)
{
	if (stopping.load())
		return false;

	const SOCKET tcp = tcpSocket.load();
	const SOCKET udp = udpSocket.load();
	if (tcp == INVALID_SOCKET || udp == INVALID_SOCKET)
		return false;

	if (!IocpPool::Associate(reinterpret_cast<HANDLE>(tcp)) ||
		!IocpPool::Associate(reinterpret_cast<HANDLE>(udp)))
		return false;

	onPacket_ = std::move(onPacket);
	onClose_ = std::move(onClose);
	ctrlOp_ = std::make_unique<CtrlRecvOp>();
	recvOp_ = std::make_unique<UdpRecvOp>();

	if (!PostCtrlRecv() || !PostUdpRecv())
	{
		Stop();
		NotifyClosed();
		return false;
	}

	{
		lock_guard<mutex> lg(ioLock_);
		if (stopping.load())
			return false;
		ready = true;
	}

	return true;
}

bool SocksHelper::UDP::PostCtrlRecv()
{
	lock_guard<mutex> lg(ioLock_);
	const SOCKET tcp = tcpSocket.load();
	if (tcp == INVALID_SOCKET || stopping.load() || !ctrlOp_)
		return false;

	ZeroMemory(&ctrlOp_->overlapped, sizeof(OVERLAPPED));
	ctrlOp_->complete = &UDP::CtrlComplete;
	ctrlOp_->flags = 0;
	ctrlOp_->owner = shared_from_this();

	AcquireUdpIoOp();
	WSABUF wbuf{ 1, ctrlOp_->buf };
	const int rc = WSARecv(tcp, &wbuf, 1, NULL, &ctrlOp_->flags, &ctrlOp_->overlapped, NULL);
	if (rc == SOCKET_ERROR)
	{
		const int err = WSAGetLastError();
		if (err != WSA_IO_PENDING)
		{
			ctrlOp_->owner.reset();
			ReleaseUdpIoOp();
			return false;
		}
	}
	return true;
}

bool SocksHelper::UDP::PostUdpRecv()
{
	lock_guard<mutex> lg(ioLock_);
	const SOCKET udp = udpSocket.load();
	if (udp == INVALID_SOCKET || stopping.load() || !recvOp_)
		return false;

	ZeroMemory(&recvOp_->overlapped, sizeof(OVERLAPPED));
	recvOp_->complete = &UDP::RecvComplete;
	recvOp_->flags = 0;
	recvOp_->fromLen = sizeof(SOCKADDR_IN6);
	memset(&recvOp_->fromAddr, 0, sizeof(recvOp_->fromAddr));
	recvOp_->owner = shared_from_this();

	AcquireUdpIoOp();
	WSABUF wbuf{ static_cast<ULONG>(recvOp_->buf.size()), recvOp_->buf.data() };
	const int rc = WSARecvFrom(udp, &wbuf, 1, NULL, &recvOp_->flags,
		reinterpret_cast<PSOCKADDR>(&recvOp_->fromAddr), &recvOp_->fromLen, &recvOp_->overlapped, NULL);
	if (rc == SOCKET_ERROR)
	{
		const int err = WSAGetLastError();
		if (err != WSA_IO_PENDING)
		{
			recvOp_->owner.reset();
			ReleaseUdpIoOp();
			return false;
		}
	}
	return true;
}

void SocksHelper::UDP::CtrlComplete(IocpOperation* op, DWORD error, DWORD bytes)
{
	auto* ctrl = static_cast<CtrlRecvOp*>(op);
	auto owner = std::move(ctrl->owner);
	if (!owner)
	{
		ReleaseUdpIoOp();
		return;
	}

	if (error == NO_ERROR && bytes > 0 && !owner->stopping.load())
	{
		const bool reposted = owner->PostCtrlRecv();
		if (!reposted)
		{
			owner->Stop();
			owner->NotifyClosed();
		}
		owner.reset();
		ReleaseUdpIoOp();
		return;
	}

	owner->Stop();
	owner->NotifyClosed();
	owner.reset();
	ReleaseUdpIoOp();
}

void SocksHelper::UDP::RecvComplete(IocpOperation* op, DWORD error, DWORD bytes)
{
	auto* recv = static_cast<UdpRecvOp*>(op);
	auto owner = std::move(recv->owner);
	if (!owner)
	{
		ReleaseUdpIoOp();
		return;
	}

	const bool truncated = (error == ERROR_MORE_DATA || error == WSAEMSGSIZE);
	if ((error == NO_ERROR || truncated) && !owner->stopping.load())
	{
		if (error == NO_ERROR && owner->onPacket_ && IsSameUdpEndpoint(recv->fromAddr, owner->address))
		{
			SOCKADDR_IN6 remoteAddr{};
			int payloadOffset = 0;
			int payloadLength = 0;
			if (UnpackSocks5UdpPacket(recv->buf.data(), static_cast<int>(bytes), &remoteAddr, &payloadOffset, &payloadLength))
				owner->onPacket_(remoteAddr, recv->buf.data() + payloadOffset, payloadLength);
		}

		const bool reposted = owner->PostUdpRecv();
		if (!reposted)
		{
			owner->Stop();
			owner->NotifyClosed();
		}
		owner.reset();
		ReleaseUdpIoOp();
		return;
	}

	owner->Stop();
	owner->NotifyClosed();
	owner.reset();
	ReleaseUdpIoOp();
}

void SocksHelper::UDP::WaitAllIoDrained()
{
	unique_lock<mutex> ul(g_udpIoLock);
	g_udpIoCv.wait(ul, []() { return g_activeUdpIoOps == 0; });
}

int SocksHelper::UDP::Send(PSOCKADDR_IN6 target, const char* buffer, int length)
{
	if (target == nullptr)
		return SOCKET_ERROR;

	if (target->sin6_family != AF_INET && target->sin6_family != AF_INET6)
		return SOCKET_ERROR;

	const int headerLength = target->sin6_family == AF_INET ? 10 : 22;
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

	// Scatter-gather send avoiding an extra payload copy per packet.
	WSABUF buffers[2] = {
		{ static_cast<ULONG>(headerLength), header },
		{ static_cast<ULONG>(length), const_cast<char*>(buffer) }
	};
	DWORD bytesSent = 0;
	const DWORD bufferCount = length > 0 ? 2 : 1;
	const int dataLength = headerLength + length;

	lock_guard<mutex> lg(ioLock_);
	const SOCKET udp = this->udpSocket.load();
	if (udp == INVALID_SOCKET || stopping.load())
		return SOCKET_ERROR;

	const int sendResult = WSASendTo(udp, buffers, bufferCount, &bytesSent, 0,
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
	const SOCKET udp = this->udpSocket.load();
	if (udp == INVALID_SOCKET || length < 4)
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
			FD_SET(udp, &fds);

			const int readyStatus = select(NULL, &fds, NULL, NULL, &wait);
			if (readyStatus == 0 || readyStatus == SOCKET_ERROR)
				return readyStatus;
		}

		const int size = recvfrom(udp, buffer, length, 0, NULL, NULL);
		if (size == SOCKET_ERROR)
		{
			if (WSAGetLastError() == WSAEMSGSIZE)
				continue;
			return SOCKET_ERROR;
		}

		int offset = 0;
		int payloadLen = 0;
		if (!UnpackSocks5UdpPacket(buffer, size, target, &offset, &payloadLen))
			continue;

		if (payloadOffset != NULL)
			*payloadOffset = offset;
		return payloadLen;
	}
}

bool SocksHelper::UDP::CanRetry() const
{
	const auto now = chrono::duration_cast<chrono::milliseconds>(
		chrono::steady_clock::now().time_since_epoch()).count();
	return now >= retryAfterMs.load();
}

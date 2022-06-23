#include "SocksHelper.h"

#include "Utils.h"

extern wstring tgtHost;
extern wstring tgtPort;
extern string tgtUsername;
extern string tgtPassword;

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
	if (send(client, buffer, 4, 0) != 4)
	{
		printf("[Redirector][SocksHelper::Handshake] Send client hello failed: %d\n", WSAGetLastError());
		return false;
	}

	/* Server Choice */
	if (recv(client, buffer, 2, 0) != 2)
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
		if (send(client, buffer, length, 0) != length)
		{
			printf("[Redirector][SocksHelper::Handshake] Send authentication request failed: %d\n", WSAGetLastError());
			return false;
		}

		/* Server Response */
		if (recv(client, buffer, 2, 0) != 2)
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
	if (recv(client, (char*)&addrType, 1, 0) != 1)
	{
		printf("[Redirector][SocksHelper::SplitAddr] Read address type failed: %d\n", WSAGetLastError());
		return false;
	}

	if (addrType == 0x01)
	{
		auto ipv4 = (PSOCKADDR_IN)addr;
		ipv4->sin_family = AF_INET;

		if (recv(client, (char*)&ipv4->sin_addr, 4, 0) != 4)
		{
			printf("[Redirector][SocksHelper::SplitAddr] Read IPv4 address failed: %d\n", WSAGetLastError());
			return false;
		}

		if (recv(client, (char*)&ipv4->sin_port, 2, 0) != 2)
		{
			printf("[Redirector][SocksHelper::SplitAddr] Read IPv4 port failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else if (addrType == 0x04)
	{
		addr->sin6_family = AF_INET6;

		if (recv(client, (char*)&addr->sin6_addr, 16, 0) != 16)
		{
			printf("[Redirector][SocksHelper::SplitAddr] Read IPv6 address failed: %d\n", WSAGetLastError());
			return false;
		}

		if (recv(client, (char*)&addr->sin6_port, 2, 0) != 2)
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

		if (send(this->tcpSocket, buffer, 10, 0) != 10)
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

		if (send(this->tcpSocket, buffer, sizeof(buffer), 0) != sizeof(buffer))
		{
			printf("[Redirector][SocksHelper::TCP::Connect] Send connect request failed: %d\n", WSAGetLastError());
			return false;
		}
	}

	/* Server Response */
	char buffer[3];
	if (recv(this->tcpSocket, buffer, 3, 0) != 3)
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
		return send(this->tcpSocket, buffer, length, 0);

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
	if (this->tcpSocket != INVALID_SOCKET)
	{
		closesocket(this->tcpSocket);

		this->tcpSocket = INVALID_SOCKET;
	}

	if (this->udpSocket != INVALID_SOCKET)
	{
		closesocket(this->udpSocket);

		this->udpSocket = INVALID_SOCKET;
	}
}

void SocksHelper::UDP::Run(SOCKET tcpSocket, SOCKET udpSocket)
{
	char buffer[1];

	while (tcpSocket != INVALID_SOCKET)
	{
		if (recv(tcpSocket, buffer, sizeof(buffer), 0) != sizeof(buffer))
			break;

		if (send(tcpSocket, buffer, sizeof(buffer), 0) != sizeof(buffer))
			break;
	}

	if (tcpSocket != INVALID_SOCKET) closesocket(tcpSocket);
	if (udpSocket != INVALID_SOCKET) closesocket(udpSocket);
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

	if (send(this->tcpSocket, buffer, 10, 0) != 10)
	{
		printf("[Redirector][SocksHelper::UDP::Associate] Send udp associate request failed: %d\n", WSAGetLastError());
		return false;
	}

	if (recv(this->tcpSocket, buffer, 3, 0) != 3)
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

	thread(SocksHelper::UDP::Run, this->tcpSocket, this->udpSocket).detach();
	return true;
}

int SocksHelper::UDP::Send(PSOCKADDR_IN6 target, const char* buffer, int length)
{
	if (this->udpSocket == INVALID_SOCKET)
		return SOCKET_ERROR;

	if (target->sin6_family != AF_INET && target->sin6_family != AF_INET6)
		return SOCKET_ERROR;

	auto data = new char[3 + 1 + 16 + 2 + (ULONG64)length]();
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

	memcpy(data + 3 + 1 + (target->sin6_family == AF_INET ? 4 : 16) + 2, buffer, length);
	auto dataLength = 3 + 1 + (target->sin6_family == AF_INET ? 4 : 16) + 2 + length;

	if (sendto(this->udpSocket, data, dataLength, 0, (PSOCKADDR)&this->address, (this->address.sin6_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6))) != dataLength)
	{
		delete[] data;

		printf("[Redirector][SocksHelper::UDP::Send] Send packet failed: %d\n", WSAGetLastError());
		return SOCKET_ERROR;
	}

	delete[] data;
	return length;
}

int SocksHelper::UDP::Read(PSOCKADDR_IN6 target, char* buffer, int length, PTIMEVAL timeout)
{
	if (!this->udpSocket)
		return SOCKET_ERROR;

	if (timeout != NULL)
	{
		fd_set fds;
		FD_ZERO(&fds);
		FD_SET(this->udpSocket, &fds);

		int size = select(NULL, &fds, NULL, NULL, timeout);
		if (size == 0 || size == SOCKET_ERROR)
			return size;
	}

	int size = recvfrom(this->udpSocket, buffer, length, 0, NULL, NULL);
	if (size == 0 || size == SOCKET_ERROR)
		return size;

	SOCKADDR_IN6 addr;
	if (buffer[3] == 0x01)
	{
		auto ipv4 = (PSOCKADDR_IN)&addr;
		ipv4->sin_family = AF_INET;

		memcpy(&ipv4->sin_addr, buffer + 4, 4);
		memcpy(&ipv4->sin_port, buffer + 8, 2);

		memcpy(buffer, buffer + 10, (ULONG64)size - 10);
	}
	else if (buffer[3] == 0x04)
	{
		addr.sin6_family = AF_INET6;

		memcpy(&addr.sin6_addr, buffer + 4, 16);
		memcpy(&addr.sin6_port, buffer + 20, 2);

		memcpy(buffer, buffer + 22, (ULONG64)size - 22);
	}
	else
	{
		return SOCKET_ERROR;
	}

	if (target != NULL)
		memcpy(target, &addr, sizeof(SOCKADDR_IN6));

	return size - (addr.sin6_family == AF_INET ? 10 : 22);
}

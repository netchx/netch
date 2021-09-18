#include "SocksHelper.h"

#include "Utils.h"

extern wstring tgtHost;
extern wstring tgtPort;
extern string tgtUsername;
extern string tgtPassword;

SOCKET SocksHelper::Utils::Connect()
{
	auto client = socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
	if (!client)
	{
		printf("[Redirector][SocksHelper::Utils::Connect] Create socket failed: %d\n", WSAGetLastError());
		return NULL;
	}

	{
		int v6only = 0;
		if (setsockopt(client, IPPROTO_IPV6, IPV6_V6ONLY, (char*)&v6only, sizeof(v6only)) == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::Utils::Connect] Set socket option failed: %d\n", WSAGetLastError());

			closesocket(client);
			return NULL;
		}
	}

	timeval timeout;
	timeout.tv_sec = 4;

	if (!WSAConnectByNameW(client, (LPWSTR)tgtHost.c_str(), (LPWSTR)tgtPort.c_str(), NULL, NULL, NULL, NULL, &timeout, NULL))
	{
		printf("[Redirector][SocksHelper::Utils::Connect] Connect to remote server failed: %d\n", WSAGetLastError());

		closesocket(client);
		return NULL;
	}

	return client;
}

bool SocksHelper::Utils::Handshake(SOCKET client)
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
		printf("[Redirector][SocksHelper::Utils::Handshake] Send client hello failed: %d\n", WSAGetLastError());
		return false;
	}

	/* Server Choice */
	if (recv(client, buffer, 2, 0) != 2)
	{
		printf("[Redirector][SocksHelper::Utils::Handshake] Receive server choice failed: %d\n", WSAGetLastError());
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
		buffer[1 + plength] = 0x00;
		if (plength != 0)
		{
			buffer[1 + ulength] = plength;
			memcpy(buffer + 1 + 1 + ulength + 1, tgtPassword.c_str(), plength);
		}

		auto length = 1 + 1 + ulength + 1 + plength;
		if (send(client, buffer, length, 0) != length)
		{
			printf("[Redirector][SocksHelper::Utils::Handshake] Send authentication request failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else if (buffer[1] != 0x00)
	{
		return false;
	}

	/* Server Response */
	if (recv(client, buffer, 2, 0) != 2)
	{
		printf("[Redirector][SocksHelper::Utils::Handshake] Receive server response failed: %d\n", WSAGetLastError());
		return false;
	}

	if (buffer[1] != 0x00)
	{
		puts("[Redirector][SocksHelper::Utils::Handshake] Authentication failed");
		return false;
	}

	return true;
}

bool SocksHelper::Utils::ReadAddr(SOCKET client, char type, PSOCKADDR addr)
{
	if (type == 0x01)
	{
		auto address = (PSOCKADDR_IN)addr;
		address->sin_family = AF_INET;

		if (recv(client, (char*)&address->sin_addr, 4, 0) != 4)
		{
			printf("[Redirector][SocksHelper::Utils::ReadAddr] Read IPv4 address failed: %d\n", WSAGetLastError());
			return false;
		}

		if (recv(client, (char*)&address->sin_port, 2, 0) != 2)
		{
			printf("[Redirector][SocksHelper::Utils::ReadAddr] Read IPv4 port failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else if (type == 0x04)
	{
		auto address = (PSOCKADDR_IN6)addr;
		address->sin6_family = AF_INET6;

		if (recv(client, (char*)&address->sin6_addr, 16, 0) != 16)
		{
			printf("[Redirector][SocksHelper::Utils::ReadAddr] Read IPv6 address failed: %d\n", WSAGetLastError());
			return false;
		}

		if (recv(client, (char*)&address->sin6_port, 2, 0) != 2)
		{
			printf("[Redirector][SocksHelper::Utils::ReadAddr] Read IPv6 port failed: %d\n", WSAGetLastError());
			return false;
		}
	}
	else
	{
		puts("[Redirector][SocksHelper::Utils::ReadAddr] Unsupported address family");
		return false;
	}

	return true;
}

SocksHelper::TCP::TCP()
{

}

SocksHelper::TCP::TCP(SOCKET tcpSocket)
{
	this->tcpSocket = tcpSocket;
}

SocksHelper::TCP::~TCP()
{
	if (this->tcpSocket)
	{
		closesocket(this->tcpSocket);
		this->tcpSocket = NULL;
	}
}

bool SocksHelper::TCP::Connect(PSOCKADDR target)
{
	/* Connect Request */
	if (target->sa_family == AF_INET)
	{
		char buffer[10];
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
		char buffer[22];
		buffer[0] = 0x05;
		buffer[1] = 0x01;
		buffer[2] = 0x00;
		buffer[3] = 0x04;

		auto addr = (PSOCKADDR_IN6)target;
		memcpy(buffer + 4, &addr->sin6_addr, 16);
		memcpy(buffer + 20, &addr->sin6_port, 2);

		if (send(this->tcpSocket, buffer, 22, 0) != 22)
		{
			printf("[Redirector][SocksHelper::TCP::Connect] Send connect request failed: %d\n", WSAGetLastError());
			return false;
		}
	}

	/* Server Response */
	char buffer[4];
	if (recv(this->tcpSocket, buffer, 4, 0) != 4)
	{
		printf("[Redirector][SocksHelper::TCP::Connect] Receive server response failed: %d\n", WSAGetLastError());
		return false;
	}

	if (buffer[1] != 0x00)
	{
		return false;
	}

	SOCKADDR_IN6 addr;
	return Utils::ReadAddr(this->tcpSocket, buffer[3], (PSOCKADDR)&addr);
}

int SocksHelper::TCP::Send(char* buffer, int length)
{
	if (this->tcpSocket)
	{
		return send(this->tcpSocket, buffer, length, 0);
	}

	return -1;
}

int SocksHelper::TCP::Read(char* buffer, int length)
{
	if (this->tcpSocket)
	{
		return recv(this->tcpSocket, buffer, length, 0);
	}

	return -1;
}

SocksHelper::UDP::UDP()
{
}

SocksHelper::UDP::UDP(SOCKET tcpSocket)
{
	this->tcpSocket = tcpSocket;
}

SocksHelper::UDP::~UDP()
{
	if (this->tcpSocket)
	{
		closesocket(this->tcpSocket);
		this->tcpSocket = NULL;
	}

	if (this->udpSocket)
	{
		closesocket(this->udpSocket);
		this->udpSocket = NULL;
	}

	if (this->tcpThread.joinable())
	{
		this->tcpThread.join();
	}
}

bool SocksHelper::UDP::Associate()
{
	if (!this->tcpSocket)
	{
		return false;
	}

	char buffer[10];
	buffer[0] = 0x05;
	buffer[1] = 0x03;
	buffer[2] = 0x00;
	buffer[3] = 0x01;
	buffer[4] = 0x00;
	buffer[5] = 0x00;
	buffer[6] = 0x00;
	buffer[7] = 0x00;
	buffer[8] = 0x00;
	buffer[9] = 0x00;

	if (send(this->tcpSocket, buffer, 10, 0) != 10)
	{
		printf("[Redirector][SocksHelper::UDP::Associate] Send udp associate request failed: %d\n", WSAGetLastError());
		return false;
	}

	if (recv(this->tcpSocket, buffer, 4, 0) != 4)
	{
		printf("[Redirector][SocksHelper::UDP::Associate] Receive udp associate response failed: %d\n", WSAGetLastError());
		return false;
	}

	if (buffer[1] != 0x00)
	{
		puts("[Redirector][SocksHelper::UDP::Associate] UDP associate failed");
		return false;
	}

	return Utils::ReadAddr(this->tcpSocket, buffer[3], (PSOCKADDR)&this->address);
}

bool SocksHelper::UDP::CreateUDP()
{
	if (this->address.sin6_family == AF_INET6)
	{
		this->udpSocket = socket(AF_INET6, SOCK_DGRAM, IPPROTO_UDP);
		if (!this->udpSocket)
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
	else
	{
		this->udpSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (!this->udpSocket)
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

	return true;
}

int SocksHelper::UDP::Send(PSOCKADDR target, char* buffer, int length)
{
	if (!this->udpSocket)
	{
		return -1;
	}

	auto data = new char[3 + 1 + 16 + 2 + (ULONG64)length]();
	data[3] = (target->sa_family == AF_INET6) ? 0x04 : 0x01;

	if (target->sa_family == AF_INET)
	{
		auto ipv4 = (PSOCKADDR_IN)target;

		memcpy(data + 4, &ipv4->sin_addr, 4);
		memcpy(data + 8, &ipv4->sin_port, 2);
	}
	else if (target->sa_family == AF_INET6)
	{
		auto ipv6 = (PSOCKADDR_IN6)target;

		memcpy(data + 4, &ipv6->sin6_addr, 16);
		memcpy(data + 20, &ipv6->sin6_port, 2);
	}
	else
	{
		delete[] data;

		puts("[Redirector][SocksHelper::UDP::Send] Unsupported address family");
		return length;
	}

	memcpy(data + 3 + 1 + (target->sa_family == AF_INET6 ? 16 : 4) + 2, buffer, length);
	auto dataLength = 3 + 1 + (target->sa_family == AF_INET6 ? 16 : 4) + 2 + length;

	if (sendto(this->udpSocket, data, dataLength, 0, (PSOCKADDR)&this->address, (this->address.sin6_family == AF_INET6 ? sizeof(SOCKADDR_IN6) : sizeof(SOCKADDR_IN))) != dataLength)
	{
		delete[] data;

		printf("[Redirector][SocksHelper::UDP::Send] Send packet failed: %d\n", WSAGetLastError());
		return 0;
	}

	delete[] data;
	return dataLength;
}

int SocksHelper::UDP::Read(PSOCKADDR target, char* buffer, int length)
{
	if (!this->udpSocket)
	{
		return -1;
	}

	auto targetLength = 0;
	auto bufferLength = recvfrom(this->udpSocket, buffer, length, 0, target, &targetLength);
	if (!bufferLength)
	{
		if (bufferLength == SOCKET_ERROR)
		{
			printf("[Redirector][SocksHelper::UDP::Read] Receive packet failed: %d\n", WSAGetLastError());
			return 0;
		}

		return 0;
	}

	memset(target, 0, sizeof(SOCKADDR_IN6));
	if (buffer[3] == 0x04)
	{
		auto ipv6 = (PSOCKADDR_IN6)target;
		ipv6->sin6_family = AF_INET6;
		
		memcpy(&ipv6->sin6_addr, buffer + 4, 16);
		memcpy(&ipv6->sin6_port, buffer + 20, 2);

		memcpy(buffer, buffer + 22, (ULONG64)bufferLength - 22);
	}
	else
	{
		auto ipv4 = (PSOCKADDR_IN)target;
		ipv4->sin_family = AF_INET;

		memcpy(&ipv4->sin_addr, buffer + 4, 4);
		memcpy(&ipv4->sin_port, buffer + 8, 2);

		memcpy(buffer, buffer + 10, (ULONG64)bufferLength - 10);
	}

	return bufferLength - (target->sa_family == AF_INET6 ? 22 : 10);
}

void SocksHelper::UDP::run()
{
	char buffer[1];

	while (true)
	{
		if (recv(this->tcpSocket, buffer, 1, 0) != 1)
		{
			return;
		}

		if (send(this->tcpSocket, buffer, 1, 0) != 1)
		{
			return;
		}
	}
}

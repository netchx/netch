#include "DNSHandler.h"

// Noob code
// Waiting rewrite

extern string dnsHost;
extern USHORT dnsPort;

void ProcessPacket(ENDPOINT_ID id, SOCKADDR_IN6 target, const char* packet, int length, PNF_UDP_OPTIONS options)
{
	auto buffer = new char[1024]();

	auto tcpSocket = SocksHelper::Utils::Connect();
	if (tcpSocket != INVALID_SOCKET)
	{
		if (SocksHelper::Utils::Handshake(tcpSocket))
		{
			SocksHelper::UDP udpConn;
			udpConn.tcpSocket = tcpSocket;

			if (udpConn.Associate())
			{
				if (udpConn.CreateUDP())
				{
					SOCKADDR_IN6 addr;
					if (inet_pton(AF_INET, dnsHost.c_str(), &addr.sin6_addr) == 1)
					{
						addr.sin6_family = AF_INET;
					}
					else if (inet_pton(AF_INET6, dnsHost.c_str(), &((PSOCKADDR_IN)&addr)->sin_addr) == 1)
					{
						addr.sin6_family = AF_INET6;
					}

					if (addr.sin6_family == AF_INET)
					{
						((PSOCKADDR_IN)&addr)->sin_port = htons(dnsPort);
					}
					else
					{
						addr.sin6_port = htons(dnsPort);
					}

					if (udpConn.Send(&addr, packet, length) == length)
					{
						int size = udpConn.Read(NULL, buffer, sizeof(buffer));
						if (size != 0 && size != SOCKET_ERROR)
						{
							nf_udpPostReceive(id, (unsigned char*)&target, buffer, size, options);
						}
					}
				}
			}
		}
	}

	delete options;
	delete[] buffer;
	delete[] packet;
}

bool DNSHandler::IsDNS(PSOCKADDR_IN6 target)
{
	if (target->sin6_family == AF_INET)
	{
		return ((PSOCKADDR_IN)target)->sin_port == htons(53);
	}

	return target->sin6_port == htons(53);
}

void DNSHandler::CreateHandler(ENDPOINT_ID id, PSOCKADDR_IN6 target, const char* packet, int length, PNF_UDP_OPTIONS options)
{
	SOCKADDR_IN6 remote;
	auto buffer = new char[length]();
	auto option = (PNF_UDP_OPTIONS)new char[sizeof(NF_UDP_OPTIONS) + options->optionsLength];

	memcpy(&remote, target, sizeof(SOCKADDR_IN6));
	memcpy(buffer, packet, length);
	memcpy(option, options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);

	thread(ProcessPacket, id, remote, buffer, length, option).detach();
}

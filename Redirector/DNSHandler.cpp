#include "DNSHandler.h"

#include "SocksHelper.h"

extern bool dnsProx;
extern string dnsHost;
extern USHORT dnsPort;

SOCKADDR_IN6 dnsAddr;

void HandleClientDNS(ENDPOINT_ID id, PSOCKADDR_IN6 target, char* packet, int length, PNF_UDP_OPTIONS option)
{
	auto remote = socket(AF_INET6, SOCK_DGRAM, IPPROTO_UDP);
	if (remote != INVALID_SOCKET)
	{
		int v6only = 0;

		if (setsockopt(remote, IPPROTO_IPV6, IPV6_V6ONLY, (char*)&v6only, sizeof(v6only)) != SOCKET_ERROR)
		{
			SOCKADDR_IN6 addr;
			IN6ADDR_SETANY(&addr);

			if (bind(remote, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN6)) != SOCKET_ERROR)
			{
				if (sendto(remote, packet, length, 0, (PSOCKADDR)&dnsAddr, (dnsAddr.sin6_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6))) == length)
				{
					timeval timeout{};
					timeout.tv_sec = 4;

					fd_set fds;
					FD_ZERO(&fds);
					FD_SET(remote, &fds);

					int size = select(NULL, &fds, NULL, NULL, &timeout);
					if (size != 0 && size != SOCKET_ERROR)
					{
						char buffer[1024];

						size = recvfrom(remote, buffer, sizeof(buffer), 0, NULL, NULL);
						if (size != 0 && size != SOCKET_ERROR)
							nf_udpPostReceive(id, (PBYTE)target, buffer, size, option);
					}
				}
			}
		}
	}

	if (remote != INVALID_SOCKET)
		closesocket(remote);

	delete target;
	delete[] packet;
	delete[] option;
}

void HandleRemoteDNS(ENDPOINT_ID id, PSOCKADDR_IN6 target, char* packet, int length, PNF_UDP_OPTIONS option)
{
	auto remote = new SocksHelper::UDP();
	if (remote->Associate())
	{
		if (remote->CreateUDP())
		{
			if (remote->Send(&dnsAddr, packet, length) == length)
			{
				char buffer[1024];

				timeval timeout{};
				timeout.tv_sec = 4;

				int size = remote->Read(NULL, buffer, sizeof(buffer), &timeout);
				if (size != 0 && size != SOCKET_ERROR)
					nf_udpPostReceive(id, (PBYTE)target, buffer, size, option);
			}
		}
	}

	delete remote;
	delete target;
	delete[] packet;
	delete[] option;
}

bool DNSHandler::INIT()
{
	memset(&dnsAddr, 0, sizeof(dnsAddr));

	auto ipv4 = (PSOCKADDR_IN)&dnsAddr;
	if (inet_pton(AF_INET, dnsHost.c_str(), &ipv4->sin_addr) == 1)
	{
		ipv4->sin_family = AF_INET;
		ipv4->sin_port = htons(dnsPort);
		return true;
	}

	auto ipv6 = (PSOCKADDR_IN6)&dnsAddr;
	if (inet_pton(AF_INET6, dnsHost.c_str(), &ipv6->sin6_addr) == 1)
	{
		ipv6->sin6_family = AF_INET6;
		ipv6->sin6_port = htons(dnsPort);
		return true;
	}

	return false;
}

bool DNSHandler::IsDNS(PSOCKADDR_IN6 target)
{
	if (target->sin6_family == AF_INET)
		return ((PSOCKADDR_IN)target)->sin_port == htons(53);
	else
		return target->sin6_port == htons(53);
}

void DNSHandler::CreateHandler(ENDPOINT_ID id, PSOCKADDR_IN6 target, const char* packet, int length, PNF_UDP_OPTIONS options)
{
	auto remote = new SOCKADDR_IN6();
	auto buffer = new char[length]();
	auto option = (PNF_UDP_OPTIONS)new char[sizeof(NF_UDP_OPTIONS) + options->optionsLength];

	memcpy(remote, target, sizeof(SOCKADDR_IN6));
	memcpy(buffer, packet, length);
	memcpy(option, options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);

	if (!dnsProx)
		thread(HandleClientDNS, id, remote, buffer, length, option).detach();
	else
		thread(HandleRemoteDNS, id, remote, buffer, length, option).detach();
}

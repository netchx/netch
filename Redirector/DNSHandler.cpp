#include "DNSHandler.h"

#include "SocksHelper.h"

extern bool dnsProx;
extern string dnsHost;
extern USHORT dnsPort;

SOCKADDR_IN6 dnsAddr;
constexpr size_t DnsResponseBufferSize = 65535;

void HandleClientDNS(ENDPOINT_ID id, PSOCKADDR_IN6 target, char* packet, int length, PNF_UDP_OPTIONS option)
{
	// Windows does not reliably accept an AF_INET destination on an AF_INET6
	// sendto socket, even when IPV6_V6ONLY is disabled.
	const int family = dnsAddr.sin6_family;
	auto remote = socket(family, SOCK_DGRAM, IPPROTO_UDP);
	if (remote != INVALID_SOCKET)
	{
		SOCKADDR_IN6 addr{};
		if (family == AF_INET)
		{
			((PSOCKADDR_IN)&addr)->sin_family = AF_INET;
		}
		else
		{
			addr.sin6_family = AF_INET6;
		}
		const int addressLength = family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
		if (bind(remote, (PSOCKADDR)&addr, addressLength) != SOCKET_ERROR)
		{
			if (sendto(remote, packet, length, 0, (PSOCKADDR)&dnsAddr, addressLength) == length)
			{
				timeval timeout{};
				timeout.tv_sec = 4;

				fd_set fds;
				FD_ZERO(&fds);
				FD_SET(remote, &fds);

				int size = select(NULL, &fds, NULL, NULL, &timeout);
				if (size != 0 && size != SOCKET_ERROR)
				{
					char buffer[DnsResponseBufferSize];

					size = recvfrom(remote, buffer, sizeof(buffer), 0, NULL, NULL);
					if (size != 0 && size != SOCKET_ERROR)
						nf_udpPostReceive(id, (PBYTE)target, buffer, size, option);
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
	auto remote = std::make_shared<SocksHelper::UDP>();
	if (remote->Associate())
	{
		if (remote->CreateUDP())
		{
			if (remote->Send(&dnsAddr, packet, length) == length)
			{
				char buffer[DnsResponseBufferSize];
				int payloadOffset = 0;

				timeval timeout{};
				timeout.tv_sec = 4;

				int size = remote->Read(NULL, buffer, sizeof(buffer), &payloadOffset, &timeout);
				if (size != 0 && size != SOCKET_ERROR)
					nf_udpPostReceive(id, (PBYTE)target, buffer + payloadOffset, size, option);
			}
		}
	}

	remote->Stop();
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

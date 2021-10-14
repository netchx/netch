#include "DNSHandler.h"

extern string dnsHost;
extern USHORT dnsPort;

SOCKADDR_IN6 dnsAddr;

void HandleDNS(ENDPOINT_ID id, PSOCKADDR_IN6 target, char* packet, int length, PNF_UDP_OPTIONS option)
{
	char buffer[1024];

	auto remote = new SocksHelper::UDP();
	if (remote->Associate())
	{
		if (remote->CreateUDP())
		{
			if (remote->Send(&dnsAddr, packet, length) == length)
			{
				timeval timeout{};
				timeout.tv_sec = 4;

				int size = remote->Read(NULL, buffer, sizeof(buffer), &timeout);
				if (size != 0 && size != SOCKET_ERROR)
				{
					nf_udpPostReceive(id, (unsigned char*)target, buffer, size, option);
				}
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

	cout << "[Redirector][DNSHandler::INIT] Convert address failed: " << WSAGetLastError() << " [" << dnsHost << ":" << dnsPort << "]" << endl;
	return false;
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
	auto remote = new SOCKADDR_IN6();
	auto buffer = new char[length]();
	auto option = (PNF_UDP_OPTIONS)new char[sizeof(NF_UDP_OPTIONS) + options->optionsLength];

	memcpy(remote, target, sizeof(SOCKADDR_IN6));
	memcpy(buffer, packet, length);
	memcpy(option, options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);

	thread(HandleDNS, id, remote, buffer, length, option).detach();
}

#include "DNSHandler.h"

#include "Utils.h"

void dns_freePacket(PDNSPKT i)
{
	if (i)
	{
		if (i->Target)
		{
			delete[] i->Target;
		}

		if (i->Buffer)
		{
			delete[] i->Buffer;
		}

		if (i->Option)
		{
			delete i->Option;
		}

		delete i;
	}

	i = NULL;
}

DNSHandler::DNSHandler(string dnsHost, USHORT dnsPort)
{
	this->dnsHost = dnsHost;
	this->dnsPort = dnsPort;
}

BOOL DNSHandler::init()
{
	this->Started = TRUE;

	WaitForSingleObject(this->dnsLock, INFINITE);
	for (size_t i = 0; i < 4; i++)
	{
		this->dnsLoop[i] = thread(&DNSHandler::Thread, this);
	}
	ReleaseMutex(this->dnsLock);
}

void DNSHandler::free()
{
	if (this->dnsLock)
	{
		WaitForSingleObject(this->dnsLock, INFINITE);
	}

	this->Started = FALSE;
	for (size_t i = 0; i < this->dnsLoop.size(); i++)
	{
		if (this->dnsLoop[i].joinable())
		{
			this->dnsLoop[i].join();
		}
	}
	this->dnsLoop.clear();

	auto size = this->dnsList.size();
	for (size_t i = 0; i < size; i++)
	{
		dns_freePacket(this->dnsList.front());
	}

	if (this->dnsLock)
	{
		CloseHandle(this->dnsLock);
		this->dnsLock = NULL;
	}
}

void DNSHandler::Create(ENDPOINT_ID id, PBYTE target, ULONG targetLength, PCHAR buffer, ULONG bufferLength, PNF_UDP_OPTIONS option)
{
	if (!this->Started)
	{
		return;
	}

	auto data = new DNSPKT();
	data->ID = id;
	data->Target = new BYTE[targetLength]();
	data->TargetLength = targetLength;
	data->Buffer = new CHAR[bufferLength]();
	data->BufferLength = bufferLength;
	data->Option = new NF_UDP_OPTIONS();

	memcpy(data->Target, target, targetLength);
	memcpy(data->Buffer, buffer, bufferLength);
	memcpy(data->Option, option, sizeof(NF_UDP_OPTIONS));

	WaitForSingleObject(this->dnsLock, INFINITE);
	this->dnsList.emplace(data);
	ReleaseMutex(this->dnsLock);
}

void DNSHandler::Thread()
{
	while (this->Started && this->dnsLock)
	{
		switch (WaitForSingleObject(this->dnsLock, 10))
		{
		case WAIT_OBJECT_0:
			this->Handle();
			break;
		default:
			continue;
		}
	}
}

void DNSHandler::Handle()
{
	SOCKADDR_IN addr;
	addr.sin_family = AF_INET;
	addr.sin_port = htons(this->dnsPort);
	if (!inet_pton(AF_INET, this->dnsHost.c_str(), &addr.sin_addr))
	{
		ReleaseMutex(this->dnsLock);
		return;
	}

	auto data = this->dnsList.front();
	this->dnsList.pop();
	ReleaseMutex(this->dnsLock);

	auto client = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
	if (!client)
	{
		dns_freePacket(data);

		printf("[Redirector][DNSHandler::Handle] Create socket failed: %d\n", WSAGetLastError());
		return;
	}

	TIMEVAL tv;
	tv.tv_sec = 4;
	tv.tv_usec = 0;

	FD_SET fds;
	FD_ZERO(&fds);
	FD_SET(client, &fds);

	if (sendto(client, data->Buffer, data->BufferLength, 0, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN)))
	{
		if (select(0, &fds, 0, 0, &tv))
		{
			CHAR buffer[1024];
			auto length = recvfrom(client, buffer, sizeof(buffer), 0, NULL, NULL);

			if (length)
			{
				nf_udpPostReceive(data->ID, data->Target, buffer, length, data->Option);
			}
			else
			{
				printf("[Redirector][DNSHandler::Handle] Receive failed: %d\n", WSAGetLastError());
			}
		}
		else
		{
			puts("[Redirector][DNSHandler::Handle] Receive timed out");
		}
	}
	else
	{
		printf("[Redirector][DNSHandler::Handle] Send failed: %d\n", WSAGetLastError());
	}
	
	closesocket(client);
	dns_freePacket(data);
}

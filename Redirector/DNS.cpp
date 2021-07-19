#include "DNS.h"

#include "Data.h"
#include "Utils.h"

#include <stdio.h>

#include <list>
#include <string>
#include <thread>

using namespace std;

extern string dnsHost;
extern USHORT dnsPort;

typedef struct _DNSPKT {
	ENDPOINT_ID     ID;
	PBYTE           Target;
	ULONG           TargetLength;
	PCHAR           Buffer;
	ULONG           BufferLength;
	PNF_UDP_OPTIONS Option;
} DNSPKT, * PDNSPKT;

BOOL dnsInited = FALSE;
HANDLE dnsLock = NULL;
list<PDNSPKT> dnsList;

SOCKET dns_createSocket()
{
	sockaddr_in addr;
	addr.sin_family = AF_INET;
	addr.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
	addr.sin_port = 0;

	auto client = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
	if (client == INVALID_SOCKET)
	{
		printf("[Redirector][DNS][CreateSocket] Unable to create socket: %d\n", WSAGetLastError());
		return NULL;
	}

	if (bind(client, (PSOCKADDR)&addr, sizeof(sockaddr_in)) == SOCKET_ERROR)
	{
		printf("[Redirector][DNS][CreateSocket] Unable to bind socket: %d\n", WSAGetLastError());
		return NULL;
	}

	return client;
}

void dns_freePacket(PDNSPKT i)
{
	if (i)
	{
		if (i->Target)
		{
			free(i->Target);
		}

		if (i->Buffer)
		{
			free(i->Buffer);
		}

		if (i->Option)
		{
			free(i->Option);
		}

		free(i);
	}

	i = NULL;
}

void dns_work()
{
	sockaddr_in addr;
	memset(&addr, 0, sizeof(sockaddr_in));
	addr.sin_addr.S_un.S_addr = inet_addr(dnsHost.c_str());
	addr.sin_port = htons(dnsPort);

	while (dnsInited)
	{
		auto client = dns_createSocket();
		if (NULL == client)
		{
			Sleep(100);
			continue;
		}

		WaitForSingleObject(dnsLock, INFINITE);
		if (!dnsList.size())
		{
			closesocket(client);
			ReleaseMutex(dnsLock);

			Sleep(1);
			continue;
		}

		auto data = dnsList.front();
		dnsList.remove(data);
		ReleaseMutex(dnsLock);

		if (data->BufferLength != (ULONG)sendto(client, (PCHAR)data->Buffer, data->BufferLength, NULL, (PSOCKADDR)&addr, sizeof(sockaddr_in)))
		{
			closesocket(client);
			dns_freePacket(data);

			printf("[Redirector][DNS][dnsWorker] Unable to send packet: %d\n", WSAGetLastError());
			continue;
		}

		char buffer[1500];
		auto length = recvfrom(client, buffer, sizeof(buffer), NULL, NULL, NULL);
		if (!length)
		{
			closesocket(client);
			dns_freePacket(data);

			printf("[Redirector][DNS][dnsWorker] Unable to receive packet: %d\n", WSAGetLastError());
			continue;
		}

		nf_udpPostReceive(data->ID, data->Target, buffer, length, data->Option);
		closesocket(client);
		dns_freePacket(data);
	}
}

void dns_init()
{
	if (!dnsLock)
	{
		dnsLock = CreateMutex(NULL, FALSE, NULL);
	}

	dnsInited = TRUE;
	dnsDelete();

	for (DWORD i = 0; i < 4; i++)
	{
		thread(dns_work).detach();
	}
}

void dns_free()
{
	dnsInited = FALSE;
	Sleep(10);

	if (dnsLock)
	{
		dnsDelete();

		CloseHandle(dnsLock);
		dnsLock = NULL;
	}
}

void dnsCreate(ENDPOINT_ID id, PBYTE target, ULONG targetLength, PCHAR buffer, ULONG bufferLength, PNF_UDP_OPTIONS option)
{
	if (!dnsInited)
	{
		return;
	}

	auto data = (PDNSPKT)malloc(sizeof(DNSPKT));
	if (!data)
	{
		puts("[Redirector][DNS][dnsCreate] Unable to allocate memory");
		return;
	}
	data->ID = id;

	data->Target = (PBYTE)malloc(targetLength);
	data->TargetLength = targetLength;
	if (!data->Target)
	{
		free(data);

		puts("[Redirector][DNS][dnsCreate] Unable to allocate memory");
		return;
	}

	data->Buffer = (PCHAR)malloc(bufferLength);
	data->BufferLength = bufferLength;
	if (!data->Buffer)
	{
		free(data->Target);
		free(data);

		puts("[Redirector][DNS][dnsCreate] Unable to allocate memory");
		return;
	}

	data->Option = (PNF_UDP_OPTIONS)malloc(sizeof(NF_UDP_OPTIONS));
	if (!data->Option)
	{
		free(data->Target);
		free(data->Buffer);
		free(data);

		puts("[Redirector][DNS][dnsCreate] Unable to allocate memory");
		return;
	}

	memcpy(data->Target, target, targetLength);
	memcpy(data->Buffer, buffer, bufferLength);
	memcpy(data->Option, option, sizeof(NF_UDP_OPTIONS));

	WaitForSingleObject(dnsLock, INFINITE);
	dnsList.emplace_back(data);
	ReleaseMutex(dnsLock);
}

void dnsDelete()
{
	WaitForSingleObject(dnsLock, INFINITE);

	for (auto i : dnsList)
	{
		dns_freePacket(i);
	}
	dnsList.clear();

	ReleaseMutex(dnsLock);
}

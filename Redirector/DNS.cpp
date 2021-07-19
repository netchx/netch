#include "DNS.h"

#include "API.h"
#include "Data.h"
#include "Utils.h"

#include <stdio.h>

#include <list>
#include <string>

using namespace std;

extern string dnsHost;
extern USHORT dnsPort;
extern USHORT dnsLisn;

typedef struct _DNSPKT {
	ENDPOINT_ID     ID;
	PBYTE           Target;
	ULONG           TargetLength;
	PCHAR           Buffer;
	ULONG           BufferLength;
	PNF_UDP_OPTIONS Option;
} DNSPKT, * PDNSPKT;

BOOL          dnsInit = FALSE;
HANDLE        dnsLock = NULL;
list<PDNSPKT> dnsList;

SOCKET CreateSocket()
{
	sockaddr_in addr;
	addr.sin_family = AF_INET;
	addr.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
	addr.sin_port = 0;

	auto client = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
	if (INVALID_SOCKET == client)
	{
		printf("[Redirector][DNS][CreateSocket] Unable to create socket: %d\n", WSAGetLastError());
		return NULL;
	}

	if (SOCKET_ERROR == bind(client, (PSOCKADDR)&addr, sizeof(sockaddr_in)))
	{
		printf("[Redirector][DNS][CreateSocket] Unable to bind socket: %d\n", WSAGetLastError());
		return NULL;
	}

	return client;
}

void DnsFreePacket(PDNSPKT i)
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

void dns_init()
{
	dnsInit = TRUE;

	if (!dnsLock)
	{
		dnsLock = CreateMutex(NULL, FALSE, NULL);
	}

	dnsDelete();
}

void dns_free()
{
	dnsInit = FALSE;
	Sleep(10);

	if (dnsLock)
	{
		dnsDelete();

		CloseHandle(dnsLock);
		dnsLock = NULL;
	}
}

void dnsWorker()
{
	sockaddr_in addr;
	memset(&addr, 0, sizeof(sockaddr_in));
	addr.sin_addr.S_un.S_addr = inet_addr(dnsHost.c_str());
	addr.sin_port = htons(dnsPort);

	while (dnsInit)
	{
		auto client = CreateSocket();
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
			DnsFreePacket(data);

			printf("[Redirector][DNS][dnsWorker] Unable to send packet: %d\n", WSAGetLastError());
			continue;
		}

		char buffer[1500];
		auto length = recvfrom(client, buffer, sizeof(buffer), NULL, NULL, NULL);
		if (!length)
		{
			closesocket(client);
			DnsFreePacket(data);

			printf("[Redirector][DNS][dnsWorker] Unable to receive packet: %d\n", WSAGetLastError());
			continue;
		}

		nf_udpPostReceive(data->ID, data->Target, buffer, length, data->Option);
		closesocket(client);
		DnsFreePacket(data);
	}
}

void dnsCreate(ENDPOINT_ID id, PBYTE target, ULONG targetLength, PCHAR buffer, ULONG bufferLength, PNF_UDP_OPTIONS option)
{
	if (!dnsInit)
	{
		return;
	}

	auto data = (PDNSPKT)malloc(sizeof(DNSPKT));
	if (!data)
	{
		puts("[Redirector][DNS][dnsCreate] Unable to allocate memory");
		return;
	}

	memset(data, 0, sizeof(DNSPKT));
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
		DnsFreePacket(i);
	}
	dnsList.clear();

	ReleaseMutex(dnsLock);
}

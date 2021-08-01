#pragma once
#ifndef DNSHANDLER_H
#define DNSHANDLER_H
#include "Based.h"

typedef struct _DNSPKT {
	ENDPOINT_ID     ID;
	PBYTE           Target;
	ULONG           TargetLength;
	PCHAR           Buffer;
	ULONG           BufferLength;
	PNF_UDP_OPTIONS Option;
} DNSPKT, * PDNSPKT;

typedef class DNSHandler
{
public:
	DNSHandler(string dnsHost, USHORT dnsPort);

	BOOL init();
	void free();

	void Create(ENDPOINT_ID id, PBYTE target, ULONG targetLength, PCHAR buffer, ULONG bufferLength, PNF_UDP_OPTIONS options);
private:
	void Thread();
	void Handle();

	string dnsHost;
	USHORT dnsPort = 0;

	HANDLE         dnsLock;
	queue<PDNSPKT> dnsList;
	vector<thread> dnsLoop;

	BOOL Started = FALSE;
} *PDNSHandler;

#endif

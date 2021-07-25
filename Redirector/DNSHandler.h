#pragma once
#ifndef DNSHANDLER_H
#define DNSHANDLER_H
#include <Windows.h>

#include <mutex>
#include <string>
#include <vector>

#include <nfapi.h>

using namespace std;

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
	~DNSHandler();

	void Create(ENDPOINT_ID id, PBYTE target, ULONG targetLength, PCHAR buffer, ULONG bufferLength, PNF_UDP_OPTIONS options);
private:
	void Delete(PDNSPKT i);
	void Worker();

	BOOL Started = FALSE;

	string DNSHost;
	USHORT DNSPort = 0;

	mutex DNSLock;
	vector<PDNSPKT> DNSList;
} *PDNSHandler;

#endif

#pragma once
#ifndef DNSHANDLER_H
#define DNSHANDLER_H
#include "Based.h"
#include "SocksHelper.h"

namespace DNSHandler
{
	bool INIT();

	bool IsDNS(PSOCKADDR_IN6 target);

	void CreateHandler(ENDPOINT_ID id, PSOCKADDR_IN6 target, const char* packet, int length, PNF_UDP_OPTIONS options);
}

#endif

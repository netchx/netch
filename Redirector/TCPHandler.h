#pragma once
#ifndef TCPHANDLER_H
#define TCPHANDLER_H
#include "Based.h"
#include "SocksHelper.h"

#include <cstdint>

namespace TCPHandler
{
	bool INIT();
	void FREE();

	void CreateHandler(ENDPOINT_ID id, SOCKADDR_IN6 client, SOCKADDR_IN6 remote, uint64_t connId = 0);
	void DeleteHandler(ENDPOINT_ID id, SOCKADDR_IN6 client);

	bool CloseSession(uint64_t connId);
	void CloseAllSessions();
}

#endif

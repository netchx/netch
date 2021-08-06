#pragma once
#ifndef TCPHANDLER_H
#define TCPHANDLER_H
#include "Based.h"

typedef class TCPHandler
{
public:
	BOOL init();
	void free();

	void Create(ENDPOINT_ID id, PNF_TCP_CONN_INFO info);
	void Delete(ENDPOINT_ID id);

	USHORT ListenIPv4 = 0;
	USHORT ListenIPv6 = 0;
private:
	void IPv4();
	void IPv6();
	void Handle(SOCKET client, USHORT side);

	mutex Lock;
	map<USHORT, ENDPOINT_ID> Context;

	SOCKET SocketIPv4 = NULL;
	SOCKET SocketIPv6 = NULL;
} *PTCPHandler;

#endif

#pragma once
#ifndef TCPHANDLER_H
#define TCPHANDLER_H
#include "Based.h"

typedef struct _TCPINFO {
	DWORD    PID;
	SOCKADDR Target;
} TCPINFO, * PTCPINFO;

typedef class TCPHandler
{
public:
	TCPHandler(USHORT tcpPort);

	BOOL init();
	void free();

	HANDLE tcpLock = NULL;
	map<USHORT, PTCPINFO> tcpContext;
private:
	void IPv4();
	void IPv6();
	void Handle(SOCKET client, USHORT side);

	USHORT tcpPort = 0;

	BOOL Started = FALSE;
	SOCKET IPv4Socket = NULL;
	SOCKET IPv6Socket = NULL;
} *PTCPHandler;

#endif

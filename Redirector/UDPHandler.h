#pragma once
#ifndef UDPHANDLER_H
#define UDPHANDLER_H
#include "Based.h"

typedef struct _UDPINFO {
	SOCKET Socket;
} UDPINFO, * PUDPINFO;

typedef class UDPHandler
{
public:
	BOOL init();
	void free();
} *PUDPHandler;

#endif

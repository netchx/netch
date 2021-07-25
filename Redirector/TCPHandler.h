#pragma once
#ifndef TCPHANDLER_H
#define TCPHANDLER_H
#include <Windows.h>

typedef class TCPHandler
{
public:
	TCPHandler();
	~TCPHandler();

	BOOL init();
	void free();
} *PTCPHandler;

#endif

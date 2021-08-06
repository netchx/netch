#pragma once
#ifndef BASED_H
#define BASED_H
#include <stdio.h>

#include <map>
#include <list>
#include <queue>
#include <regex>
#include <mutex>
#include <string>
#include <vector>
#include <thread>
#include <iostream>

#include <WinSock2.h>
#include <ws2ipdef.h>
#include <WS2tcpip.h>
#include <Windows.h>

#include <nfapi.h>

using namespace std;

typedef enum _AIO_TYPE {
	AIO_FILTERLOOPBACK,
	AIO_FILTERICMP,
	AIO_FILTERTCP,
	AIO_FILTERUDP,

	AIO_CLRNAME,
	AIO_ADDNAME,
	AIO_BYPNAME,

	AIO_TCPPORT,
	AIO_UDPPORT
} AIO_TYPE;

typedef struct _TCPINFO {
	DWORD PID;
	PBYTE Client[NF_MAX_ADDRESS_LENGTH];
	PBYTE Target[NF_MAX_ADDRESS_LENGTH];
} TCPINFO, * PTCPINFO;

typedef struct _UDPINFO {
	DWORD  PID;
	SOCKET Socket;
} UDPINFO, * PUDPINFO;

#endif

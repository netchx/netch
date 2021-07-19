#pragma once
#ifndef DATA_H
#define DATA_H
#include <Windows.h>

#include <nfapi.h>

typedef enum _AIO_TYPE {
	AIO_FILTERLOOP,
	AIO_FILTERICMP,
	AIO_FILTERTCP,
	AIO_FILTERUDP,

	AIO_CLRNAME,
	AIO_ADDNAME,
	AIO_BYPNAME,

	AIO_DNSHOOK,
	AIO_DNSHOST,
	AIO_DNSPORT,

	AIO_APIPORT,
	AIO_TCPPORT,
	AIO_UDPPORT
} AIO_TYPE;

typedef struct _TCPINFO {
	BYTE   Target[NF_MAX_ADDRESS_LENGTH];
} TCPINFO, * PTCPINFO;

typedef struct _UDPINFO {
	SOCKET Socket;
} UDPINFO, * PUDPINFO;

#endif

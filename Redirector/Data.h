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

	AIO_TCPPORT,
	AIO_UDPPORT
} AIO_TYPE;

#endif

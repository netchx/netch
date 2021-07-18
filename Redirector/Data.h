#pragma once
#ifndef DATA_H
#define DATA_H
#include <WinSock2.h>
#include <Windows.h>

enum {
	AIO_FILTERLOOPBACK,
	AIO_FILTERICMP,
	AIO_FILTERTCP,
	AIO_FILTERUDP,
	AIO_TCPLISN,
	AIO_UDPLISN
};

#endif

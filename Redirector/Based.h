#pragma once
#ifndef BASED_H
#define BASED_H
#include <stdio.h>

#include <map>
#include <list>
#include <queue>
#include <regex>
#include <mutex>
#include <chrono>
#include <string>
#include <vector>
#include <thread>
#include <iostream>
#include <memory>
#include <atomic>
#include <unordered_map>

#include <WinSock2.h>
#include <MSWSock.h>
#include <ws2ipdef.h>
#include <WS2tcpip.h>
#include <tlhelp32.h>
#include <mstcpip.h>
#include <Windows.h>

#include <nfapi.h>

using namespace std;

// Per-connection and per-packet diagnostics. Release builds run with a hidden
// console, so formatting and writing these lines (and the process lookups some
// of them need) only cost time on the Netfilter callback threads.
#ifdef _DEBUG
#define REDIRECTOR_TRACE_W(expr) do { wcout << expr << L'\n'; } while (0)
#define REDIRECTOR_TRACE(...) do { printf(__VA_ARGS__); } while (0)
#else
#define REDIRECTOR_TRACE_W(expr) do { } while (0)
#define REDIRECTOR_TRACE(...) do { } while (0)
#endif

typedef enum _AIO_TYPE {
	AIO_FILTERLOOPBACK,
	AIO_FILTERINTRANET,
	AIO_FILTERPARENT,
	AIO_FILTERICMP,
	AIO_FILTERTCP,
	AIO_FILTERUDP,
	AIO_FILTERDNS,

	AIO_ICMPING,

	AIO_DNSONLY,
	AIO_DNSPROX,
	AIO_DNSHOST,
	AIO_DNSPORT,

	AIO_TGTHOST,
	AIO_TGTPORT,
	AIO_TGTUSER,
	AIO_TGTPASS,

	AIO_CLRNAME,
	AIO_ADDNAME,
	AIO_BYPNAME
} AIO_TYPE;

#endif

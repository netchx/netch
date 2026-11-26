#pragma once
#ifndef BASED_H
#define BASED_H
#include <stdio.h>

#include <algorithm>
#include <array>
#include <atomic>
#include <cctype>
#include <cerrno>
#include <chrono>
#include <condition_variable>
#include <cstddef>
#include <cstdint>
#include <deque>
#include <functional>
#include <iostream>
#include <iterator>
#include <list>
#include <map>
#include <memory>
#include <mutex>
#include <queue>
#include <regex>
#include <shared_mutex>
#include <string>
#include <thread>
#include <unordered_map>
#include <unordered_set>
#include <utility>
#include <vector>

#include <WinSock2.h>
#include <MSWSock.h>
#include <ws2ipdef.h>
#include <WS2tcpip.h>
#include <tlhelp32.h>
#include <mstcpip.h>
#include <Windows.h>

#define _C_API
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
	AIO_BYPNAME,

	AIO_CLRIP,
	AIO_ADDIP,
	AIO_BYPIP,

	AIO_TRACKCONNECTIONS,
	AIO_TRACKUNMATCHED,

	AIO_BEGINRULES,
	AIO_COMMITRULES
} AIO_TYPE;

extern atomic<bool> filterLoopback;
extern atomic<bool> filterIntranet;
extern atomic<bool> filterParent;
extern atomic<bool> filterICMP;
extern atomic<bool> filterTCP;
extern atomic<bool> filterUDP;
extern atomic<bool> filterDNS;

extern atomic<DWORD> icmping;

extern atomic<bool> dnsOnly;
extern atomic<bool> dnsProx;
extern string dnsHost;
extern atomic<USHORT> dnsPort;

extern wstring tgtHost;
extern wstring tgtPort;
extern string tgtUsername;
extern string tgtPassword;
extern shared_mutex configLock;

#include "IpFilter.h"

struct ProcessRuleEntry
{
	wregex regex;
	string patternUtf8;
};

enum class RuleOpResult
{
	Invalid = 0,
	Staged = 1,
	Applied = 2
};

bool RuleBeginTransaction();
bool RuleCommitTransaction();
void RuleResetTransaction();
RuleOpResult RuleClearNames();
RuleOpResult RuleAddBypassName(const wchar_t* value);
RuleOpResult RuleAddHandleName(const wchar_t* value);
RuleOpResult RuleClearIps();
RuleOpResult RuleAddBypassIp(const wchar_t* value);
RuleOpResult RuleAddHandleIp(const wchar_t* value);

extern atomic<bool> trackConnections;
extern atomic<bool> trackUnmatched;

extern vector<ProcessRuleEntry> bypassList;
extern vector<ProcessRuleEntry> handleList;
extern vector<IpCidrRule> bypassIpList;
extern vector<IpCidrRule> handleIpList;
extern shared_mutex ruleLock;

#endif

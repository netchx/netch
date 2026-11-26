#include "Based.h"
#include "EventHandler.h"
#include "IPEventHandler.h"
#include "Utils.h"

extern atomic_ullong UP;
extern atomic_ullong DL;

namespace
{
	mutex g_lifecycleLock;
	bool g_aioInitialized = false;

	bool ParseBoundedDecimal(LPWSTR value, long minVal, long maxVal, long& parsedOut)
	{
		if (value == nullptr || *value == L'\0')
			return false;

		const string text = ws2s(value);
		if (text.empty())
			return false;

		for (unsigned char ch : text)
		{
			if (!isdigit(ch))
				return false;
		}

		char* end = nullptr;
		errno = 0;
		const long parsed = strtol(text.c_str(), &end, 10);
		if (errno != 0 || end == text.c_str() || *end != '\0' || parsed < minVal || parsed > maxVal)
			return false;

		parsedOut = parsed;
		return true;
	}

	bool ParseIcmpDelay(LPWSTR value, DWORD& parsedOut)
	{
		long parsed = 0;
		if (!ParseBoundedDecimal(value, 0, 60000, parsed))
			return false;
		parsedOut = static_cast<DWORD>(parsed);
		return true;
	}

	bool ParsePort(LPWSTR value, USHORT& parsedOut)
	{
		long parsed = 0;
		if (!ParseBoundedDecimal(value, 1, 65535, parsed))
			return false;
		parsedOut = static_cast<USHORT>(parsed);
		return true;
	}
}

NF_EventHandler EventHandler = {
	threadStart,
	threadEnd,
	tcpConnectRequest,
	tcpConnected,
	tcpClosed,
	tcpReceive,
	tcpSend,
	tcpCanReceive,
	tcpCanSend,
	udpCreated,
	udpConnectRequest,
	udpClosed,
	udpReceive,
	udpSend,
	udpCanReceive,
	udpCanSend
};

NF_IPEventHandler g_ipEventHandler = {
	ipReceive,
	ipSend
};

BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved)
{
	UNREFERENCED_PARAMETER(hModule);
	UNREFERENCED_PARAMETER(dwReason);
	UNREFERENCED_PARAMETER(lpReserved);

	return TRUE;
}

/* IPv4-mapped form (::ffff:0:0/96 + network) of an IPv4 network, for dual-stack IPv6 sockets */
static void AllowIPv4Mapped(const char* address, const char* mask)
{
	NF_RULE rule;
	memset(&rule, 0, sizeof(NF_RULE));
	rule.ip_family = AF_INET6;

	rule.remoteIpAddress[10] = 0xff;
	rule.remoteIpAddress[11] = 0xff;
	inet_pton(AF_INET, address, &rule.remoteIpAddress[12]);

	memset(rule.remoteIpAddressMask, 0xff, 12);
	inet_pton(AF_INET, mask, &rule.remoteIpAddressMask[12]);

	rule.filteringFlag = NF_ALLOW;
	nf_addRule(&rule, FALSE);
}

extern "C" {
	__declspec(dllexport) BOOL __cdecl aio_register(LPWSTR value)
	{
		if (value == nullptr)
			return FALSE;

		auto status = nf_registerDriver(ws2s(value).c_str());
		if (status != NF_STATUS_SUCCESS)
		{
			printf("[Redirector][aio_register] nf_registerDriver: %d\n", status);
			return FALSE;
		}

		return TRUE;
	}

	__declspec(dllexport) BOOL __cdecl aio_unregister(LPWSTR value)
	{
		if (value == nullptr)
			return FALSE;

		auto status = nf_unRegisterDriver(ws2s(value).c_str());
		if (status != NF_STATUS_SUCCESS)
		{
			printf("[Redirector][aio_unregister] nf_unRegisterDriver: %d\n", status);
			return FALSE;
		}

		return TRUE;
	}

	__declspec(dllexport) BOOL __cdecl aio_dial(int name, LPWSTR value)
	{
		switch (name)
		{
		case AIO_FILTERLOOPBACK:
			if (value == nullptr) return FALSE;
			filterLoopback = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_FILTERINTRANET:
			if (value == nullptr) return FALSE;
			filterIntranet = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_FILTERPARENT:
			if (value == nullptr) return FALSE;
			filterParent = (wstring(value).find(L"false") == wstring::npos);
			eh_clearProcessCache();
			break;
		case AIO_FILTERICMP:
			if (value == nullptr) return FALSE;
			filterICMP = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_FILTERTCP:
			if (value == nullptr) return FALSE;
			filterTCP = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_FILTERUDP:
			if (value == nullptr) return FALSE;
			filterUDP = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_FILTERDNS:
			if (value == nullptr) return FALSE;
			filterDNS = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_ICMPING:
		{
			DWORD parsed = 0;
			if (!ParseIcmpDelay(value, parsed))
				return FALSE;
			icmping = parsed;
			break;
		}
		case AIO_DNSONLY:
			if (value == nullptr) return FALSE;
			dnsOnly = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_DNSPROX:
			if (value == nullptr) return FALSE;
			dnsProx = (wstring(value).find(L"false") == wstring::npos);
			break;
		case AIO_DNSHOST:
		{
			if (value == nullptr) return FALSE;
			unique_lock<shared_mutex> ul(configLock);
			dnsHost = ws2s(value);
			break;
		}
		case AIO_DNSPORT:
		{
			USHORT parsedPort = 0;
			if (!ParsePort(value, parsedPort))
				return FALSE;
			dnsPort = parsedPort;
			break;
		}
		case AIO_TGTHOST:
		{
			if (value == nullptr) return FALSE;
			unique_lock<shared_mutex> ul(configLock);
			tgtHost = wstring(value);
			break;
		}
		case AIO_TGTPORT:
		{
			USHORT parsedPort = 0;
			if (!ParsePort(value, parsedPort))
				return FALSE;
			unique_lock<shared_mutex> ul(configLock);
			tgtPort = to_wstring(parsedPort);
			break;
		}
		case AIO_TGTUSER:
		{
			unique_lock<shared_mutex> ul(configLock);
			tgtUsername = value != nullptr ? ws2s(value) : string();
			break;
		}
		case AIO_TGTPASS:
		{
			unique_lock<shared_mutex> ul(configLock);
			tgtPassword = value != nullptr ? ws2s(value) : string();
			break;
		}
		case AIO_CLRNAME:
			if (RuleClearNames() == RuleOpResult::Applied)
				eh_clearProcessCache();
			break;
		case AIO_BYPNAME:
			{
				const auto op = RuleAddBypassName(value);
				if (op == RuleOpResult::Invalid)
					return FALSE;
				if (op == RuleOpResult::Applied)
					eh_clearProcessCache();
			}
			break;
		case AIO_ADDNAME:
			{
				const auto op = RuleAddHandleName(value);
				if (op == RuleOpResult::Invalid)
					return FALSE;
				if (op == RuleOpResult::Applied)
					eh_clearProcessCache();
			}
			break;
		case AIO_CLRIP:
			RuleClearIps();
			break;
		case AIO_BYPIP:
			if (RuleAddBypassIp(value) == RuleOpResult::Invalid)
				return FALSE;
			break;
		case AIO_ADDIP:
			if (RuleAddHandleIp(value) == RuleOpResult::Invalid)
				return FALSE;
			break;
		case AIO_TRACKCONNECTIONS:
			if (value == nullptr)
				return FALSE;
			if (_wcsicmp(value, L"true") == 0 || wcscmp(value, L"1") == 0)
				trackConnections.store(true);
			else if (_wcsicmp(value, L"false") == 0 || wcscmp(value, L"0") == 0)
				trackConnections.store(false);
			else
				return FALSE;
			break;
		case AIO_TRACKUNMATCHED:
			if (value == nullptr)
				return FALSE;
			if (_wcsicmp(value, L"true") == 0 || wcscmp(value, L"1") == 0)
				trackUnmatched.store(true);
			else if (_wcsicmp(value, L"false") == 0 || wcscmp(value, L"0") == 0)
				trackUnmatched.store(false);
			else
				return FALSE;
			break;
		case AIO_BEGINRULES:
			RuleBeginTransaction();
			break;
		case AIO_COMMITRULES:
			if (!RuleCommitTransaction())
				return FALSE;
			eh_clearProcessCache();
			break;
		default:
			return FALSE;
		}

		return TRUE;
	}

	__declspec(dllexport) BOOL __cdecl aio_init()
	{
		lock_guard<mutex> lg(g_lifecycleLock);
		if (g_aioInitialized)
			return FALSE;

		WSADATA data;
		if (WSAStartup(MAKEWORD(2, 2), &data) != NO_ERROR)
		{
			puts("[Redirector][aio_init] WSAStartup != NO_ERROR");
			return FALSE;
		}

		nf_adjustProcessPriviledges();
		if (!eh_init())
		{
			puts("[Redirector][aio_init] !eh_init");
			WSACleanup();
			return FALSE;
		}

		if (nf_init("netfilter2", &EventHandler) != NF_STATUS_SUCCESS)
		{
			puts("[Redirector][aio_init] nf_init != NF_STATUS_SUCCESS");
			eh_free();
			WSACleanup();
			return FALSE;
		}

		NF_RULE rule;
		if (!filterLoopback.load())
		{
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "127.0.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.0.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.remoteIpAddress[15] = 1;
			memset(rule.remoteIpAddressMask, 0xff, sizeof(rule.remoteIpAddressMask));
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* ::ffff:127.0.0.0/104 */
			AllowIPv4Mapped("127.0.0.0", "255.0.0.0");
		}

		if (!filterIntranet.load())
		{
			/* 10.0.0.0/8 */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "10.0.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.0.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* 100.64.0.0/10 */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "100.64.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.192.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* 169.254.0.0/16 */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "169.254.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.255.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* 172.16.0.0/12 */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "172.16.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.240.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* 192.0.0.0/24 */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "192.0.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.255.255.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* 192.168.0.0/16 */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "192.168.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.255.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* 198.18.0.0/15 */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			inet_pton(AF_INET, "198.18.0.0", rule.remoteIpAddress);
			inet_pton(AF_INET, "255.254.0.0", rule.remoteIpAddressMask);
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* The same networks through IPv4-mapped addresses */
			AllowIPv4Mapped("10.0.0.0", "255.0.0.0");
			AllowIPv4Mapped("100.64.0.0", "255.192.0.0");
			AllowIPv4Mapped("169.254.0.0", "255.255.0.0");
			AllowIPv4Mapped("172.16.0.0", "255.240.0.0");
			AllowIPv4Mapped("192.0.0.0", "255.255.255.0");
			AllowIPv4Mapped("192.168.0.0", "255.255.0.0");
			AllowIPv4Mapped("198.18.0.0", "255.254.0.0");

			/* fe80::/10 (IPv6 link-local) */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.remoteIpAddress[0] = 0xfe;
			rule.remoteIpAddress[1] = 0x80;
			rule.remoteIpAddressMask[0] = 0xff;
			rule.remoteIpAddressMask[1] = 0xc0;
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);

			/* fc00::/7 (IPv6 ULA) */
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.remoteIpAddress[0] = 0xfc;
			rule.remoteIpAddressMask[0] = 0xfe;
			rule.filteringFlag = NF_ALLOW;
			nf_addRule(&rule, FALSE);
		}

		if (filterICMP.load())
		{
			nf_setIPEventHandler(&g_ipEventHandler);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			rule.protocol = IPPROTO_ICMP;
			rule.direction = NF_D_OUT;
			rule.filteringFlag = NF_FILTER_AS_IP_PACKETS;
			nf_addRule(&rule, FALSE);
		}

		if (filterTCP.load())
		{
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			rule.protocol = IPPROTO_TCP;
			rule.direction = NF_D_OUT;
			rule.filteringFlag = NF_INDICATE_CONNECT_REQUESTS;
			nf_addRule(&rule, FALSE);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.protocol = IPPROTO_TCP;
			rule.direction = NF_D_OUT;
			rule.filteringFlag = NF_INDICATE_CONNECT_REQUESTS;
			nf_addRule(&rule, FALSE);
		}

		if (filterUDP.load() || filterDNS.load())
		{
			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET;
			rule.protocol = IPPROTO_UDP;
			rule.filteringFlag = NF_FILTER;
			nf_addRule(&rule, FALSE);

			memset(&rule, 0, sizeof(NF_RULE));
			rule.ip_family = AF_INET6;
			rule.protocol = IPPROTO_UDP;
			rule.filteringFlag = NF_FILTER;
			nf_addRule(&rule, FALSE);
		}

		g_aioInitialized = true;
		return TRUE;
	}

	__declspec(dllexport) void __cdecl aio_free()
	{
		lock_guard<mutex> lg(g_lifecycleLock);
		if (!g_aioInitialized)
			return;
		g_aioInitialized = false;

		nf_deleteRules();
		nf_free();
		eh_free();

		WSACleanup();
	}

	__declspec(dllexport) ULONG64 __cdecl aio_getUP()
	{
		return UP;
	}

	__declspec(dllexport) ULONG64 __cdecl aio_getDL()
	{
		return DL;
	}

	__declspec(dllexport) int __cdecl aio_getConnectionsJson(char* buffer, int capacity)
	{
		if (capacity < 0)
			return -1;
		return static_cast<int>(ConnectionTracker::GetConnectionsJson(buffer, static_cast<size_t>(capacity)));
	}

	__declspec(dllexport) int __cdecl aio_drainConnectionEventsJson(ULONG64 afterSeq, char* buffer, int capacity)
	{
		if (capacity < 0)
			return -1;
		return static_cast<int>(ConnectionTracker::DrainConnectionEventsJson(afterSeq, buffer, static_cast<size_t>(capacity)));
	}

	__declspec(dllexport) BOOL __cdecl aio_closeConnection(ULONG64 connId)
	{
		return ConnectionTracker::CloseConnection(connId) ? TRUE : FALSE;
	}

	__declspec(dllexport) void __cdecl aio_closeAllConnections()
	{
		ConnectionTracker::CloseAllConnections();
	}

	__declspec(dllexport) void __cdecl aio_clearClosedConnections()
	{
		ConnectionTracker::ClearClosedConnections();
	}
}

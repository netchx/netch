#include "EventHandler.h"

#include "DNSHandler.h"
#include "TCPHandler.h"

extern bool filterParent;
extern bool filterTCP;
extern bool filterUDP;
extern bool filterDNS;
extern mutex ruleLock;

extern bool dnsOnly;

extern vector<wregex> bypassList;
extern vector<wregex> handleList;

extern atomic<USHORT> tcpListen;

DWORD CurrentID = 0;

mutex udpContextLock;
map<ENDPOINT_ID, shared_ptr<SocksHelper::UDP>> udpContext;

atomic_ullong UP = { 0 };
atomic_ullong DL = { 0 };

struct ProcessInfo
{
	wstring name;
	DWORD parentId = 0;
};

struct ProcessCacheEntry
{
	ProcessInfo info;
	bool bypass = false;
	bool handle = false;
	bool classified = false;
	HANDLE processHandle = NULL;
};

struct ProcessDecision
{
	ProcessInfo info;
	bool bypass = false;
	bool handle = false;
};

// Process callbacks are frequent, while process names and parent IDs are
// stable for the lifetime of a process. Keep the expensive Win32 lookups and
// regex matching out of the hot path. The synchronization handle also lets us
// discard an entry when Windows reuses a PID.
mutex processCacheLock;
unordered_map<DWORD, ProcessCacheEntry> processCache;

wstring ConvertIP(PSOCKADDR addr)
{
	WCHAR buffer[MAX_PATH] = L"";
	DWORD bufferLength = MAX_PATH;

	if (addr->sa_family == AF_INET)
	{
		WSAAddressToStringW(addr, sizeof(SOCKADDR_IN), NULL, buffer, &bufferLength);
	}
	else
	{
		WSAAddressToStringW(addr, sizeof(SOCKADDR_IN6), NULL, buffer, &bufferLength);
	}

	return buffer;
}

wstring GetProcessNameUncached(DWORD id)
{
	if (id == 0)
	{
		return L"Idle";
	}

	if (id == 4)
	{
		return L"System";
	}

	wchar_t name[MAX_PATH];
	if (!nf_getProcessNameFromKernel(id, name, MAX_PATH))
	{
		if (!nf_getProcessNameW(id, name, MAX_PATH))
		{
			return L"Unknown";
		}
	}

	wchar_t data[MAX_PATH];
	if (GetLongPathNameW(name, data, MAX_PATH))
	{
		return data;
	}

	return name;
}

DWORD GetParentProcessId(DWORD id)
{
	PROCESSENTRY32W process;
	memset(&process, 0, sizeof(process));
	process.dwSize = sizeof(process);

	const auto snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
	if (snapshot == INVALID_HANDLE_VALUE)
		return 0;

	DWORD parentId = 0;
	if (Process32FirstW(snapshot, &process))
	{
		do
		{
			if (process.th32ProcessID == id)
			{
				parentId = process.th32ParentProcessID;
				break;
			}
		} while (Process32NextW(snapshot, &process));
	}

	CloseHandle(snapshot);
	return parentId;
}

bool IsProcessAlive(HANDLE processHandle)
{
	return processHandle != NULL && WaitForSingleObject(processHandle, 0) == WAIT_TIMEOUT;
}

ProcessInfo LoadProcessInfo(DWORD id, bool includeParent)
{
	if (id == 0)
		return { L"Idle", 0 };

	if (id == 4)
		return { L"System", 0 };

	{
		lock_guard<mutex> lg(processCacheLock);
		auto it = processCache.find(id);
		if (it != processCache.end())
		{
			if (IsProcessAlive(it->second.processHandle))
				return it->second.info;

			CloseHandle(it->second.processHandle);
			processCache.erase(it);
		}
	}

	ProcessInfo info;
	info.name = GetProcessNameUncached(id);
	if (includeParent)
		info.parentId = GetParentProcessId(id);

	// A process that cannot be opened is still returned to the caller, but is
	// intentionally not cached because its lifetime cannot be tracked safely.
	const auto processHandle = OpenProcess(SYNCHRONIZE, FALSE, id);
	if (processHandle == NULL)
		return info;

	lock_guard<mutex> lg(processCacheLock);
	auto it = processCache.find(id);
	if (it != processCache.end())
	{
		if (IsProcessAlive(it->second.processHandle))
		{
			CloseHandle(processHandle);
			return it->second.info;
		}

		CloseHandle(it->second.processHandle);
		processCache.erase(it);
	}

	ProcessCacheEntry entry;
	entry.info = info;
	entry.processHandle = processHandle;
	processCache.emplace(id, move(entry));
	return info;
}

bool MatchesAny(const vector<wregex>& rules, const wstring& name)
{
	for (const auto& rule : rules)
	{
		if (regex_search(name, rule))
			return true;
	}

	return false;
}

ProcessDecision ClassifyProcess(DWORD id)
{
	const auto info = LoadProcessInfo(id, filterParent);

	{
		lock_guard<mutex> lg(processCacheLock);
		auto it = processCache.find(id);
		if (it != processCache.end() && IsProcessAlive(it->second.processHandle) && it->second.classified)
			return { it->second.info, it->second.bypass, it->second.handle };
	}

	bool bypass;
	bool handle;
	{
		lock_guard<mutex> lg(ruleLock);
		bypass = MatchesAny(bypassList, info.name);
		handle = MatchesAny(handleList, info.name);

		if (!handle && filterParent && info.parentId != 0)
		{
			const auto parent = LoadProcessInfo(info.parentId, false);
			handle = MatchesAny(handleList, parent.name);
		}
	}

	lock_guard<mutex> lg(processCacheLock);
	auto it = processCache.find(id);
	if (it != processCache.end() && IsProcessAlive(it->second.processHandle))
	{
		it->second.bypass = bypass;
		it->second.handle = handle;
		it->second.classified = true;
		return { it->second.info, bypass, handle };
	}

	return { info, bypass, handle };
}

void eh_clearProcessCache()
{
	lock_guard<mutex> lg(processCacheLock);
	for (auto& [id, entry] : processCache)
	{
		UNREFERENCED_PARAMETER(id);
		if (entry.processHandle != NULL)
			CloseHandle(entry.processHandle);
	}
	processCache.clear();
}

bool eh_init()
{
	eh_clearProcessCache();
	CurrentID = GetCurrentProcessId();

	if (!DNSHandler::INIT())
		return false;

	if (!TCPHandler::INIT())
		return false;

	return true;
}

void eh_free()
{
	TCPHandler::FREE();

	map<ENDPOINT_ID, shared_ptr<SocksHelper::UDP>> connections;
	{
		lock_guard<mutex> lg(udpContextLock);
		connections.swap(udpContext);
	}
	for (const auto& connection : connections)
		connection.second->Stop();
	eh_clearProcessCache();

	UP = 0;
	DL = 0;
}

void threadStart()
{

}

void threadEnd()
{

}

void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	if (CurrentID == info->processId)
	{
		nf_tcpDisableFiltering(id);
		return;
	}

	if (!filterTCP)
	{
		const auto process = LoadProcessInfo(info->processId, false);
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!filterTCP] " << process.name << endl;
		return;
	}

	const auto decision = ClassifyProcess(info->processId);

	if (decision.bypass)
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][checkBypassName] " << decision.info.name << endl;
		return;
	}

	if (!decision.handle)
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!checkHandleName] " << decision.info.name << endl;
		return;
	}

	if (info->ip_family != AF_INET && info->ip_family != AF_INET6)
	{
		nf_tcpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!IPv4 && !IPv6] " << decision.info.name << endl;
		return;
	}

	SOCKADDR_IN6 client;
	memcpy(&client, info->localAddress, sizeof(SOCKADDR_IN6));

	SOCKADDR_IN6 remote;
	memcpy(&remote, info->remoteAddress, sizeof(SOCKADDR_IN6));

	if (info->ip_family == AF_INET)
	{
		auto addr = (PSOCKADDR_IN)info->remoteAddress;
		addr->sin_family = AF_INET;
		addr->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		addr->sin_port = tcpListen;
	}

	if (info->ip_family == AF_INET6)
	{
		auto addr = (PSOCKADDR_IN6)info->remoteAddress;
		IN6ADDR_SETLOOPBACK(addr);
		addr->sin6_port = tcpListen;
	}

	TCPHandler::CreateHandler(client, remote);
	wcout << "[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "] " << ConvertIP((PSOCKADDR)&client) << " -> " << ConvertIP((PSOCKADDR)&remote) << endl;
}

void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	const auto process = LoadProcessInfo(info->processId, false);
	wcout << "[Redirector][EventHandler][tcpConnected][" << id << "][" << info->processId << "][" << ConvertIP((PSOCKADDR)info->remoteAddress) << "] " << process.name << endl;
}

void tcpCanSend(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void tcpSend(ENDPOINT_ID id, const char* buffer, int length)
{
	UP += length;

	nf_tcpPostSend(id, buffer, length);
}

void tcpCanReceive(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void tcpReceive(ENDPOINT_ID id, const char* buffer, int length)
{
	DL += length;

	nf_tcpPostReceive(id, buffer, length);
}

void tcpClosed(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	SOCKADDR_IN6 client;
	memcpy(&client, info->localAddress, sizeof(SOCKADDR_IN6));

	TCPHandler::DeleteHandler(client);

	printf("[Redirector][EventHandler][tcpClosed][%llu][%lu]\n", id, info->processId);
}

void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	if (CurrentID == info->processId)
	{
		nf_udpDisableFiltering(id);
		return;
	}

	if (!filterUDP)
	{
		const auto process = LoadProcessInfo(info->processId, false);
		if (!filterDNS) nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!filterUDP] " << process.name << endl;
		return;
	}

	const auto decision = ClassifyProcess(info->processId);

	if (decision.bypass)
	{
		if (dnsOnly) nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][checkBypassName] " << decision.info.name << endl;
		return;
	}

	if (!decision.handle)
	{
		if (dnsOnly) nf_udpDisableFiltering(id);

		wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!checkHandleName] " << decision.info.name << endl;
		return;
	}

	wcout << "[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "] " << decision.info.name << endl;

	lock_guard<mutex> lg(udpContextLock);
	udpContext[id] = make_shared<SocksHelper::UDP>();
}

void udpConnectRequest(ENDPOINT_ID id, PNF_UDP_CONN_REQUEST info)
{
	UNREFERENCED_PARAMETER(id);
	UNREFERENCED_PARAMETER(info);
}

void udpCanSend(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void udpSend(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options)
{
	if (DNSHandler::IsDNS((PSOCKADDR_IN6)target))
	{
		if (!filterDNS)
		{
			nf_udpPostSend(id, target, buffer, length, options);

			wcout << "[Redirector][EventHandler][udpSend][" << id << "] B DNS to " << ConvertIP((PSOCKADDR)target) << endl;
			return;
		}
		else
		{
			UP += length;
			DNSHandler::CreateHandler(id, (PSOCKADDR_IN6)target, buffer, length, options);

			wcout << "[Redirector][EventHandler][udpSend][" << id << "] H DNS to " << ConvertIP((PSOCKADDR)target) << endl;
			return;
		}
	}

	shared_ptr<SocksHelper::UDP> remote;
	{
		lock_guard<mutex> lg(udpContextLock);
		auto it = udpContext.find(id);
		if (it != udpContext.end())
		{
			// If the SOCKS control connection died, retry with a fresh
			// association, but avoid reconnecting once per packet while the
			// proxy is unavailable.
			if (it->second->stopping && it->second->CanRetry())
				it->second = make_shared<SocksHelper::UDP>();
			else if (it->second->stopping)
				return;
			remote = it->second;
		}
	}
	if (!remote)
	{
		nf_udpPostSend(id, target, buffer, length, options);
		return;
	}
	// Serialize session creation and sends for the same UDP endpoint.
	lock_guard<mutex> sendGuard(remote->sendLock);
	if (remote->stopping)
		return;

	if (remote->tcpSocket == INVALID_SOCKET && !remote->Associate())
	{
		remote->Stop();
		return;
	}

	if (remote->udpSocket == INVALID_SOCKET)
	{
		if (!remote->CreateUDP())
		{
			remote->Stop();
			return;
		}

		auto option = (PNF_UDP_OPTIONS)new char[sizeof(NF_UDP_OPTIONS) + options->optionsLength]();
		memcpy(option, options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);

		thread(udpReceiveHandler, id, remote, option).detach();
	}

	if (remote->Send((PSOCKADDR_IN6)target, buffer, length) == length)
		UP += length;
}

void udpCanReceive(ENDPOINT_ID id)
{
	UNREFERENCED_PARAMETER(id);
}

void udpReceive(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options)
{
	nf_udpPostReceive(id, target, buffer, length, options);
}

void udpClosed(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	UNREFERENCED_PARAMETER(info);

	printf("[Redirector][EventHandler][udpClosed][%llu]\n", id);

	shared_ptr<SocksHelper::UDP> remote;
	{
		lock_guard<mutex> lg(udpContextLock);
		auto it = udpContext.find(id);
		if (it != udpContext.end())
		{
			remote = it->second;
			udpContext.erase(it);
		}
	}
	if (remote)
	{
		remote->Stop();
	}
}

void udpReceiveHandler(ENDPOINT_ID id, shared_ptr<SocksHelper::UDP> remote, PNF_UDP_OPTIONS options)
{
	// SOCKS5 adds up to 22 bytes before the game payload. Keep a full
	// datagram per association so a large packet cannot tear down the relay.
	vector<char> buffer(65535);

	while (!remote->stopping && remote->tcpSocket != INVALID_SOCKET && remote->udpSocket != INVALID_SOCKET)
	{
		SOCKADDR_IN6 target;
		int payloadOffset = 0;

		int length = remote->Read(&target, buffer.data(), static_cast<int>(buffer.size()), &payloadOffset, NULL);
		if (length == SOCKET_ERROR)
			break;

		DL += length;

		nf_udpPostReceive(id, (unsigned char*)&target, buffer.data() + payloadOffset, length, options);
	}

	remote->Stop();
	delete[] options;
}

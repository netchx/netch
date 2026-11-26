#include "EventHandler.h"

#include "DNSHandler.h"
#include "IocpPool.h"
#include "IPEventHandler.h"
#include "TCPHandler.h"
#include "Utils.h"

#include <optional>

extern atomic<USHORT> tcpListen;

DWORD CurrentID = 0;

constexpr size_t MaxUdpContexts = 1024;
constexpr size_t MaxUdpPacketsPerEndpoint = 64;
constexpr size_t MaxUdpTotalPackets = 4096;
constexpr size_t MaxUdpTotalBytes = 16 * 1024 * 1024;
constexpr size_t MaxProcessCacheEntries = 256;

atomic_ullong UP = { 0 };
atomic_ullong DL = { 0 };

namespace
{
	struct PendingUdpPacket
	{
		SOCKADDR_IN6 target{};
		vector<char> payload;
		string matchedRule;
		string ruleSource;
	};

	struct UdpContext : enable_shared_from_this<UdpContext>
	{
		ENDPOINT_ID id = 0;
		DWORD pid = 0;
		string processName;
		string processPath;
		string parentName;
		string matchedRule;
		string ruleSource;
		mutex lock;
		shared_ptr<SocksHelper::UDP> remote;
		vector<char> options;
		bool optionsCaptured = false;
		atomic<bool> closing{ false };
		bool scheduled = false;
		deque<PendingUdpPacket> queue;
		size_t queuedBytes = 0;
	};

	struct UdpEndpointMeta
	{
		DWORD pid = 0;
		string processName;
		string processPath;
		string parentName;
		bool isHandledProcess = false;
	};

	mutex udpContextLock;
	map<ENDPOINT_ID, shared_ptr<UdpContext>> udpContext;
	mutex g_udpMetaLock;
	unordered_map<ENDPOINT_ID, UdpEndpointMeta> g_udpMeta;

	mutex g_udpDispatchLock;
	condition_variable g_udpDispatchCv;
	deque<shared_ptr<UdpContext>> g_udpDispatchQueue;
	vector<thread> g_udpWorkers;
	atomic<bool> g_udpStopping{ true };
	size_t g_udpTotalPackets = 0;
	size_t g_udpTotalBytes = 0;
	vector<char> CopyUdpOptions(PNF_UDP_OPTIONS options)
	{
		if (options == nullptr)
			return {};
		const size_t optLen = options->optionsLength > 0 ? static_cast<size_t>(options->optionsLength) : 0;
		const size_t total = offsetof(NF_UDP_OPTIONS, options) + optLen;
		vector<char> out(total);
		memcpy(out.data(), options, total);
		return out;
	}

	void DropQueueLocked(UdpContext& ctx)
	{
		if (ctx.queue.empty())
			return;

		lock_guard<mutex> dl(g_udpDispatchLock);
		g_udpTotalPackets = g_udpTotalPackets >= ctx.queue.size() ? (g_udpTotalPackets - ctx.queue.size()) : 0;
		g_udpTotalBytes = g_udpTotalBytes >= ctx.queuedBytes ? (g_udpTotalBytes - ctx.queuedBytes) : 0;
		ctx.queue.clear();
		ctx.queuedBytes = 0;
	}

	void ProcessUdpContext(const shared_ptr<UdpContext>& ctx)
	{
		for (;;)
		{
			if (g_udpStopping.load() || ctx->closing.load())
			{
				lock_guard<mutex> cl(ctx->lock);
				DropQueueLocked(*ctx);
				ctx->scheduled = false;
				return;
			}

			shared_ptr<SocksHelper::UDP> remote;
			vector<char> optionsSnapshot;
			bool needSetup = false;

			{
				lock_guard<mutex> cl(ctx->lock);
				if (ctx->closing.load() || g_udpStopping.load())
				{
					DropQueueLocked(*ctx);
					ctx->scheduled = false;
					return;
				}

				if (ctx->remote && ctx->remote->stopping.load())
				{
					if (!ctx->remote->CanRetry())
					{
						DropQueueLocked(*ctx);
						ctx->scheduled = false;
						return;
					}
					ctx->remote = make_shared<SocksHelper::UDP>();
				}

				if (!ctx->remote)
					ctx->remote = make_shared<SocksHelper::UDP>();

				remote = ctx->remote;
				if (!remote->ready.load())
				{
					needSetup = true;
					optionsSnapshot = ctx->options;
				}
			}

			if (needSetup)
			{
				if (!remote->Associate() || !remote->CreateUDP())
				{
					remote->Stop();
					{
						lock_guard<mutex> cl(ctx->lock);
						DropQueueLocked(*ctx);
						ctx->scheduled = false;
					}
					if (trackConnections.load())
					{
						ConnectionTracker::ConnectionEventInput ev{};
						ev.action = ConnectionTracker::EventAction::Error;
						ev.protocol = ConnectionTracker::Protocol::Udp;
						ev.pid = ctx->pid;
						ev.processName = ctx->processName;
						ev.processPath = ctx->processPath;
						ev.parentName = ctx->parentName;
						ev.matchedRule = ctx->matchedRule;
						ev.ruleSource = "UDP";
						ev.detail = "SOCKS5 UDP associate failed";
						ev.deduplicate = true;
						ConnectionTracker::RecordEvent(ev);
						ConnectionTracker::CloseUdpEndpoint(ctx->id, "UDP relay setup failed");
					}
					return;
				}

				weak_ptr<UdpContext> weakCtx = ctx;
				const ENDPOINT_ID endpointId = ctx->id;
				auto onPacket = [weakCtx, endpointId, opts = std::move(optionsSnapshot)](
					const SOCKADDR_IN6& sender, const char* data, int len) mutable
				{
					if (g_udpStopping.load() || len < 0)
						return;
					auto locked = weakCtx.lock();
					if (!locked || locked->closing.load())
						return;

					DL += static_cast<unsigned long long>(len);
					if (trackConnections.load())
					{
						string senderIp;
						uint16_t senderPort = 0;
						if (FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(&sender), senderIp, senderPort))
							ConnectionTracker::AddUdpDownloadBytes(endpointId, senderIp, senderPort, static_cast<uint64_t>(len));
					}
					PNF_UDP_OPTIONS optPtr = opts.empty() ? nullptr : reinterpret_cast<PNF_UDP_OPTIONS>(opts.data());
					SOCKADDR_IN6 senderCopy = sender;
					nf_udpPostReceive(endpointId, reinterpret_cast<unsigned char*>(&senderCopy), data, len, optPtr);
				};

				if (!remote->StartAsync(std::move(onPacket)))
				{
					remote->Stop();
					{
						lock_guard<mutex> cl(ctx->lock);
						DropQueueLocked(*ctx);
						ctx->scheduled = false;
					}
					if (trackConnections.load())
						ConnectionTracker::CloseUdpEndpoint(ctx->id, "UDP listener start failed");
					return;
				}
			}

			PendingUdpPacket packet;
			{
				lock_guard<mutex> cl(ctx->lock);
				if (ctx->queue.empty() || ctx->closing.load() || g_udpStopping.load())
				{
					if (ctx->closing.load() || g_udpStopping.load())
						DropQueueLocked(*ctx);
					ctx->scheduled = false;
					return;
				}
				packet = std::move(ctx->queue.front());
				ctx->queue.pop_front();
				const size_t sz = packet.payload.size();
				ctx->queuedBytes = ctx->queuedBytes >= sz ? (ctx->queuedBytes - sz) : 0;

				lock_guard<mutex> dl(g_udpDispatchLock);
				if (g_udpTotalPackets > 0)
					--g_udpTotalPackets;
				g_udpTotalBytes = g_udpTotalBytes >= sz ? (g_udpTotalBytes - sz) : 0;
			}

			int sent = SOCKET_ERROR;
			{
				lock_guard<mutex> sl(remote->sendLock);
				if (!remote->stopping.load())
				{
					sent = remote->Send(&packet.target, packet.payload.data(), static_cast<int>(packet.payload.size()));
				}
			}
			if (sent == static_cast<int>(packet.payload.size()))
			{
				UP += static_cast<unsigned long long>(sent);
				if (trackConnections.load())
				{
					ConnectionTracker::ConnectionOpenInfo openInfo{};
					openInfo.protocol = ConnectionTracker::Protocol::Udp;
					openInfo.endpointId = ctx->id;
					openInfo.pid = ctx->pid;
					openInfo.processName = ctx->processName;
					openInfo.processPath = ctx->processPath;
					openInfo.parentName = ctx->parentName;
					if (FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(&packet.target), openInfo.remoteIp, openInfo.remotePort))
					{
						openInfo.matchedRule = !packet.matchedRule.empty() ? packet.matchedRule : ctx->matchedRule;
						openInfo.ruleSource = !packet.ruleSource.empty() ? packet.ruleSource : ctx->ruleSource;
						ConnectionTracker::TouchOrOpenUdpFlow(openInfo, static_cast<uint64_t>(sent));
					}
				}
			}
			else if (sent == SOCKET_ERROR)
			{
				if (packet.payload.size() <= 65507 - 22 && !remote->stopping.load())
					remote->Stop();
				if (remote->stopping.load())
				{
					lock_guard<mutex> cl(ctx->lock);
					DropQueueLocked(*ctx);
					ctx->scheduled = false;
					return;
				}
			}
		}
	}

	void UdpWorkerLoop()
	{
		BoostRelayThread();

		for (;;)
		{
			shared_ptr<UdpContext> ctx;
			{
				unique_lock<mutex> ul(g_udpDispatchLock);
				g_udpDispatchCv.wait(ul, []() { return g_udpStopping.load() || !g_udpDispatchQueue.empty(); });
				if (g_udpStopping.load() && g_udpDispatchQueue.empty())
					break;
				ctx = std::move(g_udpDispatchQueue.front());
				g_udpDispatchQueue.pop_front();
			}

			if (ctx)
				ProcessUdpContext(ctx);
		}
	}

	struct ProcessInfo
	{
		wstring name;
		DWORD parentId = 0;
		// Creation time (FILETIME ticks) detects a parent PID that was recycled by a
		// newer, unrelated process. 0 when unknown.
		ULONGLONG createTime = 0;
		bool parentLoaded = false;
	};

	constexpr int MaxParentDepth = 6;

	struct ProcessCacheEntry
	{
		ProcessInfo info;
		bool bypass = false;
		bool handle = false;
		string matchedRule;
		string ruleSource;
		string parentName;
		bool classified = false;
		HANDLE processHandle = NULL;
	};

	struct ProcessDecision
	{
		ProcessInfo info;
		bool bypass = false;
		bool handle = false;
		string matchedRule;
		string ruleSource;
		string parentName;
	};

	mutex processCacheLock;
	unordered_map<DWORD, ProcessCacheEntry> processCache;
	uint64_t g_cacheGeneration = 0;

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

		wchar_t name[1024]{};
		if (!nf_getProcessNameFromKernel(id, name, static_cast<DWORD>(std::size(name))))
		{
			if (!nf_getProcessNameW(id, name, static_cast<DWORD>(std::size(name))))
			{
				return L"Unknown";
			}
		}

		wchar_t data[1024]{};
		const DWORD len = GetLongPathNameW(name, data, static_cast<DWORD>(std::size(data)));
		if (len > 0 && len < std::size(data))
		{
			return wstring(data, len);
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

	struct ProcessBasicInformationRaw
	{
		LONG_PTR ExitStatus;
		PVOID PebBaseAddress;
		ULONG_PTR AffinityMask;
		LONG_PTR BasePriority;
		ULONG_PTR UniqueProcessId;
		ULONG_PTR InheritedFromUniqueProcessId;
	};

	using NtQueryInformationProcessFn = LONG(NTAPI*)(HANDLE, ULONG, PVOID, ULONG, PULONG);

	bool QueryProcessLineage(DWORD id, DWORD& parentId, ULONGLONG& createTime)
	{
		static const auto query = reinterpret_cast<NtQueryInformationProcessFn>(
			GetProcAddress(GetModuleHandleW(L"ntdll.dll"), "NtQueryInformationProcess"));

		const auto handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, id);
		if (handle == NULL)
			return false;

		bool ok = false;
		FILETIME creation{}, exitTime{}, kernelTime{}, userTime{};
		if (GetProcessTimes(handle, &creation, &exitTime, &kernelTime, &userTime))
		{
			createTime = (static_cast<ULONGLONG>(creation.dwHighDateTime) << 32) | creation.dwLowDateTime;

			ProcessBasicInformationRaw basic{};
			ULONG returned = 0;
			if (query != NULL && query(handle, 0 /* ProcessBasicInformation */, &basic, sizeof(basic), &returned) >= 0)
			{
				parentId = static_cast<DWORD>(basic.InheritedFromUniqueProcessId);
				ok = true;
			}
		}

		CloseHandle(handle);
		return ok;
	}

	bool IsProcessAlive(HANDLE processHandle)
	{
		return processHandle != NULL && WaitForSingleObject(processHandle, 0) == WAIT_TIMEOUT;
	}

	void SweepDeadProcessCacheLocked()
	{
		for (auto it = processCache.begin(); it != processCache.end();)
		{
			if (!IsProcessAlive(it->second.processHandle))
			{
				if (it->second.processHandle != NULL)
					CloseHandle(it->second.processHandle);
				it = processCache.erase(it);
			}
			else
			{
				++it;
			}
		}
	}

	ProcessInfo LoadProcessInfo(DWORD id, bool includeParent, uint64_t& outGeneration)
	{
		if (id == 0)
		{
			lock_guard<mutex> lg(processCacheLock);
			outGeneration = g_cacheGeneration;
			return { L"Idle", 0 };
		}

		if (id == 4)
		{
			lock_guard<mutex> lg(processCacheLock);
			outGeneration = g_cacheGeneration;
			return { L"System", 0 };
		}

		uint64_t generation = 0;
		optional<ProcessInfo> cached;
		{
			lock_guard<mutex> lg(processCacheLock);
			generation = g_cacheGeneration;
			auto it = processCache.find(id);
			if (it != processCache.end())
			{
				if (IsProcessAlive(it->second.processHandle))
				{
					if (!includeParent || it->second.info.parentLoaded)
					{
						outGeneration = generation;
						return it->second.info;
					}

					cached = it->second.info;
				}
				else
				{
					CloseHandle(it->second.processHandle);
					processCache.erase(it);
				}
			}
		}

		ProcessInfo info;
		info.name = cached ? cached->name : GetProcessNameUncached(id);
		if (includeParent)
		{
			if (!QueryProcessLineage(id, info.parentId, info.createTime))
			{
				info.parentId = GetParentProcessId(id);
				info.createTime = 0;
			}
			info.parentLoaded = true;
		}

		const auto processHandle = OpenProcess(SYNCHRONIZE, FALSE, id);
		if (processHandle == NULL)
		{
			outGeneration = generation;
			return info;
		}

		lock_guard<mutex> lg(processCacheLock);
		if (generation != g_cacheGeneration)
		{
			CloseHandle(processHandle);
			outGeneration = g_cacheGeneration;
			return info;
		}

		outGeneration = generation;
		auto it = processCache.find(id);
		if (it != processCache.end())
		{
			if (IsProcessAlive(it->second.processHandle))
			{
				CloseHandle(processHandle);
				if (info.parentLoaded && !it->second.info.parentLoaded)
					it->second.info = info;
				return it->second.info;
			}

			CloseHandle(it->second.processHandle);
			processCache.erase(it);
		}

		if (processCache.size() >= MaxProcessCacheEntries)
			SweepDeadProcessCacheLocked();

		ProcessCacheEntry entry;
		entry.info = info;
		entry.processHandle = processHandle;
		processCache.emplace(id, move(entry));
		return info;
	}

	bool MatchesAnyRule(const vector<ProcessRuleEntry>& rules, const wstring& name, string& outPattern)
	{
		for (const auto& rule : rules)
		{
			if (regex_search(name, rule.regex))
			{
				outPattern = rule.patternUtf8;
				return true;
			}
		}

		outPattern.clear();
		return false;
	}

	ProcessDecision ClassifyProcess(DWORD id)
	{
		uint64_t generation = 0;
		const bool parentEnabled = filterParent.load();
		const auto info = LoadProcessInfo(id, parentEnabled, generation);

		{
			lock_guard<mutex> lg(processCacheLock);
			if (generation == g_cacheGeneration)
			{
				auto it = processCache.find(id);
				if (it != processCache.end() && IsProcessAlive(it->second.processHandle) && it->second.classified)
					return { it->second.info, it->second.bypass, it->second.handle, it->second.matchedRule, it->second.ruleSource, it->second.parentName };
			}
		}

		bool bypass = false;
		bool handle = false;
		string bypassPattern;
		string handlePattern;
		{
			shared_lock<shared_mutex> sl(ruleLock);
			bypass = MatchesAnyRule(bypassList, info.name, bypassPattern);
			handle = MatchesAnyRule(handleList, info.name, handlePattern);
		}

		string matchedRule;
		string ruleSource;
		string parentName;

		if (bypass)
		{
			matchedRule = std::move(bypassPattern);
			ruleSource = "Process.Bypass";
		}
		else if (handle)
		{
			matchedRule = std::move(handlePattern);
			ruleSource = "Process.Handle";
		}
		else if (parentEnabled)
		{
			auto child = info;
			for (int depth = 0; depth < MaxParentDepth && child.parentId != 0 && child.parentId != 4; depth++)
			{
				uint64_t parentGen = 0;
				const auto parent = LoadProcessInfo(child.parentId, true, parentGen);
				UNREFERENCED_PARAMETER(parentGen);

				const bool lineageKnown = child.createTime != 0 && parent.createTime != 0;
				if (lineageKnown && parent.createTime > child.createTime)
					break;

				bool parentBypassed = false;
				bool parentHandled = false;
				string parentPattern;
				{
					shared_lock<shared_mutex> sl(ruleLock);
					parentBypassed = MatchesAnyRule(bypassList, parent.name, parentPattern);
					if (!parentBypassed)
						parentHandled = MatchesAnyRule(handleList, parent.name, parentPattern);
				}

				if (parentBypassed)
				{
					matchedRule = std::move(parentPattern);
					ruleSource = "Parent.Bypass";
					parentName = ConnectionTracker::ExtractProcessBaseName(parent.name);
					break;
				}

				if (parentHandled)
				{
					handle = true;
					matchedRule = std::move(parentPattern);
					ruleSource = "Parent.Handle";
					parentName = ConnectionTracker::ExtractProcessBaseName(parent.name);
					break;
				}

				if (!lineageKnown)
					break;

				child = parent;
			}
		}

		if (ruleSource.empty())
			ruleSource = "Process.Unmatched";

		lock_guard<mutex> lg(processCacheLock);
		if (generation == g_cacheGeneration)
		{
			auto it = processCache.find(id);
			if (it != processCache.end() && IsProcessAlive(it->second.processHandle))
			{
				it->second.bypass = bypass;
				it->second.handle = handle;
				it->second.matchedRule = matchedRule;
				it->second.ruleSource = ruleSource;
				it->second.parentName = parentName;
				it->second.classified = true;
				return { it->second.info, bypass, handle, matchedRule, ruleSource, parentName };
			}
		}

		return { info, bypass, handle, matchedRule, ruleSource, parentName };
	}
}

void eh_clearProcessCache()
{
	lock_guard<mutex> lg(processCacheLock);
	++g_cacheGeneration;
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
	{
		lock_guard<mutex> lg(g_udpMetaLock);
		g_udpMeta.clear();
	}
	ConnectionTracker::ResetActive();
	CurrentID = GetCurrentProcessId();

	if (!IocpPool::Init())
		return false;

	if (!IPEventHandler::INIT())
	{
		IocpPool::Shutdown();
		return false;
	}

	if (!DNSHandler::INIT())
	{
		IPEventHandler::FREE();
		IocpPool::Shutdown();
		return false;
	}

	if (!TCPHandler::INIT())
	{
		DNSHandler::FREE();
		IPEventHandler::FREE();
		IocpPool::Shutdown();
		return false;
	}

	g_udpStopping = false;
	const unsigned hw = thread::hardware_concurrency();
	const unsigned workerCount = (std::clamp)(hw == 0 ? 2u : hw, 2u, 4u);
	try
	{
		g_udpWorkers.reserve(workerCount);
		for (unsigned i = 0; i < workerCount; ++i)
			g_udpWorkers.emplace_back(UdpWorkerLoop);
	}
	catch (...)
	{
		eh_free();
		return false;
	}

	return true;
}

void eh_free()
{
	g_udpStopping = true;
	g_udpDispatchCv.notify_all();

	map<ENDPOINT_ID, shared_ptr<UdpContext>> connections;
	{
		lock_guard<mutex> lg(udpContextLock);
		connections.swap(udpContext);
	}
	for (const auto& [id, ctx] : connections)
	{
		UNREFERENCED_PARAMETER(id);
		shared_ptr<SocksHelper::UDP> remote;
		{
			lock_guard<mutex> cl(ctx->lock);
			ctx->closing = true;
			remote = ctx->remote;
			DropQueueLocked(*ctx);
		}
		if (remote)
			remote->Stop();
	}

	for (auto& w : g_udpWorkers)
	{
		if (w.joinable())
			w.join();
	}
	g_udpWorkers.clear();

	{
		lock_guard<mutex> dl(g_udpDispatchLock);
		g_udpDispatchQueue.clear();
		g_udpTotalPackets = 0;
		g_udpTotalBytes = 0;
	}

	TCPHandler::FREE();
	DNSHandler::FREE();
	IPEventHandler::FREE();
	SocksHelper::UDP::WaitAllIoDrained();
	IocpPool::Shutdown();
	eh_clearProcessCache();
	{
		lock_guard<mutex> lg(g_udpMetaLock);
		g_udpMeta.clear();
	}
	ConnectionTracker::OnRedirectorStopped();
	RuleResetTransaction();

	UP = 0;
	DL = 0;
}

bool eh_closeUdpFlow(ENDPOINT_ID id, uint64_t connId)
{
	UNREFERENCED_PARAMETER(connId);

	shared_ptr<UdpContext> ctx;
	{
		lock_guard<mutex> lg(udpContextLock);
		auto it = udpContext.find(id);
		if (it != udpContext.end())
			ctx = it->second;
	}

	if (!ctx)
		return false;

	shared_ptr<SocksHelper::UDP> remote;
	{
		lock_guard<mutex> cl(ctx->lock);
		remote = ctx->remote;
	}
	if (remote)
		remote->Stop();

	return true;
}

void eh_closeAllUdpFlows()
{
	vector<shared_ptr<UdpContext>> snapshot;
	{
		lock_guard<mutex> lg(udpContextLock);
		snapshot.reserve(udpContext.size());
		for (const auto& [id, ctx] : udpContext)
		{
			UNREFERENCED_PARAMETER(id);
			if (ctx)
				snapshot.push_back(ctx);
		}
	}

	for (const auto& ctx : snapshot)
	{
		shared_ptr<SocksHelper::UDP> remote;
		{
			lock_guard<mutex> cl(ctx->lock);
			remote = ctx->remote;
		}
		if (remote)
			remote->Stop();
	}
}

void threadStart()
{
}

void threadEnd()
{
}

void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	if (info == nullptr || info->processId == 0 || info->processId == 4 || CurrentID == info->processId)
	{
		nf_tcpDisableFiltering(id);
		return;
	}

	if (!filterTCP.load())
	{
		nf_tcpDisableFiltering(id);

		REDIRECTOR_TRACE_W("[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!filterTCP] " << ClassifyProcess(info->processId).info.name);
		return;
	}

	const USHORT listenPort = tcpListen.load();
	if (listenPort == 0)
	{
		nf_tcpDisableFiltering(id);
		return;
	}

	const auto decision = ClassifyProcess(info->processId);
	const bool validFamily = (info->ip_family == AF_INET || info->ip_family == AF_INET6);

	auto emitBypassProc = [&](const char* fallbackSource)
	{
		if (!trackConnections.load() || !trackUnmatched.load())
			return;
		ConnectionTracker::ConnectionEventInput ev{};
		ev.action = ConnectionTracker::EventAction::BypassProc;
		ev.protocol = ConnectionTracker::Protocol::Tcp;
		ev.pid = info->processId;
		ev.processName = ConnectionTracker::ExtractProcessBaseName(decision.info.name);
		ev.processPath = ConnectionTracker::WideToUtf8(decision.info.name);
		ev.parentName = decision.parentName;
		ev.matchedRule = decision.matchedRule;
		ev.ruleSource = !decision.ruleSource.empty() ? decision.ruleSource : fallbackSource;
		if (validFamily)
		{
			SOCKADDR_IN6 rawRemote{};
			const size_t sz = info->ip_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
			memcpy(&rawRemote, info->remoteAddress, sz);
			FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(&rawRemote), ev.remoteIp, ev.remotePort);
		}
		ev.deduplicate = true;
		ConnectionTracker::RecordEvent(ev);
	};

	if (decision.bypass)
	{
		nf_tcpDisableFiltering(id);
		emitBypassProc("Process.Bypass");

		REDIRECTOR_TRACE_W("[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][checkBypassName] " << decision.info.name);
		return;
	}

	if (!decision.handle)
	{
		nf_tcpDisableFiltering(id);
		emitBypassProc("Process.Unmatched");

		REDIRECTOR_TRACE_W("[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!checkHandleName] " << decision.info.name);
		return;
	}

	if (!validFamily)
	{
		nf_tcpDisableFiltering(id);

		REDIRECTOR_TRACE_W("[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "][!IPv4 && !IPv6] " << decision.info.name);
		return;
	}

	const size_t addrSize = info->ip_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
	SOCKADDR_IN6 client{};
	memcpy(&client, info->localAddress, addrSize);

	SOCKADDR_IN6 remote{};
	memcpy(&remote, info->remoteAddress, addrSize);

	IpFilterDecision ipDecision{};
	{
		shared_lock<shared_mutex> sl(ruleLock);
		ipDecision = EvaluateIpFilter(reinterpret_cast<const sockaddr*>(&remote), bypassIpList, handleIpList);
	}

	if (ipDecision.action != IpFilterAction::Proxy)
	{
		nf_tcpDisableFiltering(id);
		if (trackConnections.load())
		{
			ConnectionTracker::ConnectionEventInput ev{};
			ev.action = ConnectionTracker::EventAction::BypassIp;
			ev.protocol = ConnectionTracker::Protocol::Tcp;
			ev.pid = info->processId;
			ev.processName = ConnectionTracker::ExtractProcessBaseName(decision.info.name);
			ev.processPath = ConnectionTracker::WideToUtf8(decision.info.name);
			ev.parentName = decision.parentName;
			FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(&remote), ev.remoteIp, ev.remotePort);
			if (ipDecision.action == IpFilterAction::BypassByRule)
			{
				ev.matchedRule = ipDecision.matchedRule;
				ev.ruleSource = "IP.Bypass";
			}
			else
			{
				ev.matchedRule = "!HandleIPs";
				ev.ruleSource = "IP.Handle";
			}
			ev.deduplicate = true;
			ConnectionTracker::RecordEvent(ev);
		}
		return;
	}

	if (info->ip_family == AF_INET)
	{
		auto addr = (PSOCKADDR_IN)info->remoteAddress;
		addr->sin_family = AF_INET;
		addr->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		addr->sin_port = listenPort;
	}

	if (info->ip_family == AF_INET6)
	{
		auto addr = (PSOCKADDR_IN6)info->remoteAddress;
		IN6ADDR_SETLOOPBACK(addr);
		addr->sin6_port = listenPort;
	}

	uint64_t connId = 0;
	if (trackConnections.load())
	{
		ConnectionTracker::ConnectionOpenInfo openInfo{};
		openInfo.protocol = ConnectionTracker::Protocol::Tcp;
		openInfo.endpointId = id;
		openInfo.pid = info->processId;
		openInfo.processName = ConnectionTracker::ExtractProcessBaseName(decision.info.name);
		openInfo.processPath = ConnectionTracker::WideToUtf8(decision.info.name);
		openInfo.parentName = decision.parentName;
		FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(&remote), openInfo.remoteIp, openInfo.remotePort);
		if (!ipDecision.matchedRule.empty())
		{
			openInfo.matchedRule = ipDecision.matchedRule;
			openInfo.ruleSource = "IP.Handle";
		}
		else
		{
			openInfo.matchedRule = decision.matchedRule;
			openInfo.ruleSource = decision.ruleSource;
		}
		connId = ConnectionTracker::OpenTcpConnection(openInfo);
	}

	TCPHandler::CreateHandler(id, client, remote, connId);
	REDIRECTOR_TRACE_W("[Redirector][EventHandler][tcpConnectRequest][" << id << "][" << info->processId << "] " << ConvertIP((PSOCKADDR)&client) << " -> " << ConvertIP((PSOCKADDR)&remote));
}

void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	UNREFERENCED_PARAMETER(id);
	UNREFERENCED_PARAMETER(info);

	REDIRECTOR_TRACE_W("[Redirector][EventHandler][tcpConnected][" << id << "][" << info->processId << "][" << ConvertIP((PSOCKADDR)info->remoteAddress) << "] " << ClassifyProcess(info->processId).info.name);
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
	if (info != nullptr)
	{
		const size_t addrSize = info->ip_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
		SOCKADDR_IN6 client{};
		if (info->ip_family == AF_INET || info->ip_family == AF_INET6)
		{
			memcpy(&client, info->localAddress, addrSize);
			TCPHandler::DeleteHandler(id, client);
		}
		REDIRECTOR_TRACE("[Redirector][EventHandler][tcpClosed][%llu][%lu]\n", id, info->processId);
	}
}

void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	if (info == nullptr || info->processId == 0 || info->processId == 4 || CurrentID == info->processId)
	{
		nf_udpDisableFiltering(id);
		return;
	}

	const bool udpEnabled = filterUDP.load();
	const bool dnsEnabled = filterDNS.load();
	if (!udpEnabled && !dnsEnabled)
	{
		nf_udpDisableFiltering(id);
		return;
	}

	const auto decision = ClassifyProcess(info->processId);
	const string procBaseName = ConnectionTracker::ExtractProcessBaseName(decision.info.name);
	const string procFullPath = ConnectionTracker::WideToUtf8(decision.info.name);

	if (decision.bypass)
	{
		nf_udpDisableFiltering(id);
		if (trackConnections.load() && trackUnmatched.load())
		{
			ConnectionTracker::ConnectionEventInput ev{};
			ev.action = ConnectionTracker::EventAction::BypassProc;
			ev.protocol = ConnectionTracker::Protocol::Udp;
			ev.pid = info->processId;
			ev.processName = procBaseName;
			ev.processPath = procFullPath;
			ev.parentName = decision.parentName;
			ev.matchedRule = decision.matchedRule;
			ev.ruleSource = !decision.ruleSource.empty() ? decision.ruleSource : "Process.Bypass";
			ev.deduplicate = true;
			ConnectionTracker::RecordEvent(ev);
		}

		REDIRECTOR_TRACE_W("[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][checkBypassName] " << decision.info.name);
		return;
	}

	if (!decision.handle)
	{
		if (dnsOnly.load() || !dnsEnabled)
		{
			nf_udpDisableFiltering(id);
		}
		else
		{
			lock_guard<mutex> mlg(g_udpMetaLock);
			if (g_udpMeta.size() < MaxUdpContexts)
				g_udpMeta[id] = { info->processId, procBaseName, procFullPath, decision.parentName, false };
		}

		if (trackConnections.load() && trackUnmatched.load())
		{
			ConnectionTracker::ConnectionEventInput ev{};
			ev.action = ConnectionTracker::EventAction::BypassProc;
			ev.protocol = ConnectionTracker::Protocol::Udp;
			ev.pid = info->processId;
			ev.processName = procBaseName;
			ev.processPath = procFullPath;
			ev.parentName = decision.parentName;
			ev.matchedRule = decision.matchedRule;
			ev.ruleSource = "Process.Unmatched";
			ev.deduplicate = true;
			ConnectionTracker::RecordEvent(ev);
		}

		REDIRECTOR_TRACE_W("[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!checkHandleName] " << decision.info.name);
		return;
	}

	if (!udpEnabled)
	{
		if (dnsEnabled && !dnsOnly.load())
		{
			lock_guard<mutex> mlg(g_udpMetaLock);
			if (g_udpMeta.size() < MaxUdpContexts)
				g_udpMeta[id] = { info->processId, procBaseName, procFullPath, decision.parentName, true };
		}
		REDIRECTOR_TRACE_W("[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "][!filterUDP] " << decision.info.name);
		return;
	}

	REDIRECTOR_TRACE_W("[Redirector][EventHandler][udpCreated][" << id << "][" << info->processId << "] " << decision.info.name);

	lock_guard<mutex> lg(udpContextLock);
	if (udpContext.size() >= MaxUdpContexts)
	{
		nf_udpDisableFiltering(id);
		return;
	}

	auto ctx = make_shared<UdpContext>();
	ctx->id = id;
	ctx->pid = info->processId;
	ctx->processName = procBaseName;
	ctx->processPath = procFullPath;
	ctx->parentName = decision.parentName;
	ctx->matchedRule = decision.matchedRule;
	ctx->ruleSource = decision.ruleSource;
	ctx->remote = make_shared<SocksHelper::UDP>();
	udpContext[id] = std::move(ctx);
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
	if (target == nullptr || length < 0)
		return;

	const auto targetFamily = reinterpret_cast<const SOCKADDR*>(target)->sa_family;
	if (targetFamily != AF_INET && targetFamily != AF_INET6)
	{
		nf_udpPostSend(id, target, buffer, length, options);
		return;
	}

	if (DNSHandler::IsDNS((PSOCKADDR_IN6)target))
	{
		const bool proxiedDns = filterDNS.load();
		if (!proxiedDns)
		{
			nf_udpPostSend(id, target, buffer, length, options);
			REDIRECTOR_TRACE_W("[Redirector][EventHandler][udpSend][" << id << "] B DNS to " << ConvertIP((PSOCKADDR)target));
		}
		else
		{
			UP += length;
			DNSHandler::CreateHandler(id, (PSOCKADDR_IN6)target, buffer, length, options);
			REDIRECTOR_TRACE_W("[Redirector][EventHandler][udpSend][" << id << "] H DNS to " << ConvertIP((PSOCKADDR)target));
		}

		if (trackConnections.load())
		{
			shared_ptr<UdpContext> dnsCtx;
			{
				lock_guard<mutex> lg(udpContextLock);
				auto it = udpContext.find(id);
				if (it != udpContext.end())
					dnsCtx = it->second;
			}

			bool hasMeta = false;
			bool handledProc = false;
			ConnectionTracker::ConnectionEventInput ev{};
			ev.action = proxiedDns ? ConnectionTracker::EventAction::DnsProxy : ConnectionTracker::EventAction::DnsDirect;
			ev.protocol = ConnectionTracker::Protocol::Udp;
			ev.matchedRule = "DNS";
			ev.ruleSource = proxiedDns ? "DNS.Proxy" : "DNS.Direct";
			ev.deduplicate = true;

			if (dnsCtx)
			{
				hasMeta = true;
				handledProc = true;
				ev.pid = dnsCtx->pid;
				ev.processName = dnsCtx->processName;
				ev.processPath = dnsCtx->processPath;
				ev.parentName = dnsCtx->parentName;
			}
			else
			{
				lock_guard<mutex> mlg(g_udpMetaLock);
				auto mit = g_udpMeta.find(id);
				if (mit != g_udpMeta.end())
				{
					hasMeta = true;
					handledProc = mit->second.isHandledProcess;
					ev.pid = mit->second.pid;
					ev.processName = mit->second.processName;
					ev.processPath = mit->second.processPath;
					ev.parentName = mit->second.parentName;
				}
			}

			if (handledProc || trackUnmatched.load())
			{
				if (!hasMeta)
					ev.ruleSource = proxiedDns ? "DNS.Proxy (unmatched)" : "DNS.Direct (unmatched)";
				FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(target), ev.remoteIp, ev.remotePort);
				ConnectionTracker::RecordEvent(ev);
			}
		}
		return;
	}

	shared_ptr<UdpContext> ctx;
	{
		lock_guard<mutex> lg(udpContextLock);
		auto it = udpContext.find(id);
		if (it != udpContext.end())
			ctx = it->second;
	}
	if (!ctx)
	{
		nf_udpPostSend(id, target, buffer, length, options);
		return;
	}

	IpFilterDecision ipDecision{};
	{
		shared_lock<shared_mutex> sl(ruleLock);
		ipDecision = EvaluateIpFilter(reinterpret_cast<const sockaddr*>(target), bypassIpList, handleIpList);
	}

	if (ipDecision.action != IpFilterAction::Proxy)
	{
		nf_udpPostSend(id, target, buffer, length, options);
		if (trackConnections.load())
		{
			ConnectionTracker::ConnectionEventInput ev{};
			ev.action = ConnectionTracker::EventAction::BypassIp;
			ev.protocol = ConnectionTracker::Protocol::Udp;
			ev.pid = ctx->pid;
			ev.processName = ctx->processName;
			ev.processPath = ctx->processPath;
			ev.parentName = ctx->parentName;
			FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(target), ev.remoteIp, ev.remotePort);
			if (ipDecision.action == IpFilterAction::BypassByRule)
			{
				ev.matchedRule = ipDecision.matchedRule;
				ev.ruleSource = "IP.Bypass";
			}
			else
			{
				ev.matchedRule = "!HandleIPs";
				ev.ruleSource = "IP.Handle";
			}
			ev.deduplicate = true;
			ConnectionTracker::RecordEvent(ev);
		}
		return;
	}

	const string effectiveRule = !ipDecision.matchedRule.empty() ? ipDecision.matchedRule : ctx->matchedRule;
	const string effectiveSource = !ipDecision.matchedRule.empty() ? string("IP.Handle") : ctx->ruleSource;

	shared_ptr<SocksHelper::UDP> fastRemote;
	bool enqueue = false;
	bool shouldNotify = false;
	{
		lock_guard<mutex> cl(ctx->lock);
		if (ctx->closing.load() || g_udpStopping.load())
			return;

		if (!ctx->optionsCaptured && options != nullptr)
		{
			ctx->options = CopyUdpOptions(options);
			ctx->optionsCaptured = true;
		}

		if (ctx->remote && ctx->remote->ready.load() && !ctx->remote->stopping.load() && ctx->queue.empty() && !ctx->scheduled)
		{
			fastRemote = ctx->remote;
		}
		else
		{
			if (ctx->remote && ctx->remote->stopping.load() && !ctx->remote->CanRetry())
				return;

			const size_t packetBytes = static_cast<size_t>(length);
			if (ctx->queue.size() < MaxUdpPacketsPerEndpoint)
			{
				bool withinGlobal = false;
				{
					lock_guard<mutex> dl(g_udpDispatchLock);
					if (g_udpTotalPackets < MaxUdpTotalPackets &&
						g_udpTotalBytes + packetBytes <= MaxUdpTotalBytes)
					{
						++g_udpTotalPackets;
						g_udpTotalBytes += packetBytes;
						withinGlobal = true;
					}
				}

				if (withinGlobal)
				{
					PendingUdpPacket pkt;
					const size_t targetSize = targetFamily == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
					memcpy(&pkt.target, target, targetSize);
					if (length > 0 && buffer != nullptr)
						pkt.payload.assign(buffer, buffer + length);
					pkt.matchedRule = effectiveRule;
					pkt.ruleSource = effectiveSource;
					ctx->queue.push_back(std::move(pkt));
					ctx->queuedBytes += packetBytes;
					enqueue = true;

					if (!ctx->scheduled)
					{
						ctx->scheduled = true;
						shouldNotify = true;
					}
				}
			}
		}
	}

	if (fastRemote)
	{
		int sent = SOCKET_ERROR;
		{
			lock_guard<mutex> sl(fastRemote->sendLock);
			if (!fastRemote->stopping.load())
				sent = fastRemote->Send((PSOCKADDR_IN6)target, buffer, length);
		}
		if (sent == length)
		{
			UP += length;
			if (trackConnections.load())
			{
				ConnectionTracker::ConnectionOpenInfo openInfo{};
				openInfo.protocol = ConnectionTracker::Protocol::Udp;
				openInfo.endpointId = id;
				openInfo.pid = ctx->pid;
				openInfo.processName = ctx->processName;
				openInfo.processPath = ctx->processPath;
				openInfo.parentName = ctx->parentName;
				if (FormatNormalizedIpPort(reinterpret_cast<const sockaddr*>(target), openInfo.remoteIp, openInfo.remotePort))
				{
					openInfo.matchedRule = effectiveRule;
					openInfo.ruleSource = effectiveSource;
					ConnectionTracker::TouchOrOpenUdpFlow(openInfo, static_cast<uint64_t>(sent));
				}
			}
		}
		else if (sent == SOCKET_ERROR && length <= 65507 - 22 && !fastRemote->stopping.load())
			fastRemote->Stop();
		return;
	}

	if (enqueue && shouldNotify)
	{
		{
			lock_guard<mutex> dl(g_udpDispatchLock);
			if (!g_udpStopping.load())
				g_udpDispatchQueue.push_back(std::move(ctx));
		}
		g_udpDispatchCv.notify_one();
	}
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

	REDIRECTOR_TRACE("[Redirector][EventHandler][udpClosed][%llu]\n", id);

	{
		lock_guard<mutex> mlg(g_udpMetaLock);
		g_udpMeta.erase(id);
	}

	shared_ptr<UdpContext> ctx;
	{
		lock_guard<mutex> lg(udpContextLock);
		auto it = udpContext.find(id);
		if (it != udpContext.end())
		{
			ctx = std::move(it->second);
			udpContext.erase(it);
		}
	}
	if (ctx)
	{
		shared_ptr<SocksHelper::UDP> remote;
		{
			lock_guard<mutex> cl(ctx->lock);
			ctx->closing = true;
			remote = ctx->remote;
			DropQueueLocked(*ctx);
		}
		if (remote)
			remote->Stop();
	}

	ConnectionTracker::CloseUdpEndpoint(id, "Socket closed");
}

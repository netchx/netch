#include "DNSHandler.h"

#include "IocpPool.h"
#include "SocksHelper.h"
#include "Utils.h"

SOCKADDR_IN6 dnsAddr;

namespace
{
	constexpr auto DnsQueryTimeout = chrono::seconds(5);
	constexpr auto DirectDnsQueryTimeout = chrono::seconds(4);
	constexpr size_t MaxPendingQueries = 4096;
	constexpr size_t MaxPendingSetupQueue = 512;
	constexpr size_t RecycleAfterUnansweredQueries = 3;
	constexpr long long RelayRetryCooldownMs = 1000;
	constexpr size_t DnsResponseBufferSize = 65535;

	struct DnsQuery
	{
		ENDPOINT_ID id = 0;
		SOCKADDR_IN6 target{};
		vector<char> options;
		vector<char> packet;
	};

	struct PendingQuery
	{
		ENDPOINT_ID id = 0;
		SOCKADDR_IN6 target{};
		vector<char> options;
		USHORT originalId = 0;
		chrono::steady_clock::time_point expires;
	};

	struct DnsRelay : enable_shared_from_this<DnsRelay>
	{
		shared_ptr<SocksHelper::UDP> udp;
		mutex lock;
		unordered_map<USHORT, PendingQuery> pending;
		USHORT nextId = 0;
		size_t unansweredExpired = 0;
	};

	struct DirectDnsState;

	struct DirectRecvOp : IocpOperation
	{
		shared_ptr<DirectDnsState> owner;
		vector<char> buf = vector<char>(DnsResponseBufferSize);
		SOCKADDR_IN6 fromAddr{};
		INT fromLen = sizeof(SOCKADDR_IN6);
		DWORD flags = 0;
	};

	struct DirectDnsState : enable_shared_from_this<DirectDnsState>
	{
		atomic<SOCKET> sock{ INVALID_SOCKET };
		mutex sockLock;
		atomic<bool> stopping{ false };
		int activeOps = 0;
		mutex ioLock;
		condition_variable ioCv;

		mutex lock;
		unordered_map<USHORT, PendingQuery> pending;
		USHORT nextId = 0;
		unique_ptr<DirectRecvOp> recvOp;

		void Close()
		{
			lock_guard<mutex> sl(sockLock);
			stopping = true;
			const SOCKET s = sock.exchange(INVALID_SOCKET);
			if (s != INVALID_SOCKET)
				closesocket(s);
		}

		void AcquireIoOp()
		{
			lock_guard<mutex> lg(ioLock);
			++activeOps;
		}

		void ReleaseIoOp()
		{
			lock_guard<mutex> lg(ioLock);
			if (activeOps > 0)
				--activeOps;
			if (activeOps == 0)
				ioCv.notify_all();
		}

		void WaitDrained()
		{
			unique_lock<mutex> ul(ioLock);
			ioCv.wait(ul, [this]() { return activeOps == 0; });
		}
	};

	mutex g_relayLock;
	shared_ptr<DnsRelay> g_currentRelay;
	mutex g_relaySetupLock;
	atomic<long long> g_relayRetryAfterMs{ 0 };

	mutex g_directLock;
	shared_ptr<DirectDnsState> g_directState;

	mutex g_workerLock;
	condition_variable g_workerCv;
	deque<DnsQuery> g_setupQueue;
	vector<thread> g_setupWorkers;
	atomic<bool> g_stopping{ true };
	bool g_dnsInitialized = false;

	long long NowMs()
	{
		return chrono::duration_cast<chrono::milliseconds>(
			chrono::steady_clock::now().time_since_epoch()).count();
	}

	vector<char> CopyOptions(PNF_UDP_OPTIONS options)
	{
		if (options == nullptr)
			return {};
		const size_t optLen = options->optionsLength > 0 ? static_cast<size_t>(options->optionsLength) : 0;
		const size_t total = offsetof(NF_UDP_OPTIONS, options) + optLen;
		vector<char> copy(total);
		memcpy(copy.data(), options, total);
		return copy;
	}

	bool IsSameDnsEndpoint(const SOCKADDR_IN6& from, const SOCKADDR_IN6& expected)
	{
		if (expected.sin6_family == AF_INET)
		{
			const auto* exp4 = reinterpret_cast<const SOCKADDR_IN*>(&expected);
			if (from.sin6_family == AF_INET)
			{
				const auto* from4 = reinterpret_cast<const SOCKADDR_IN*>(&from);
				return from4->sin_port == exp4->sin_port &&
					from4->sin_addr.S_un.S_addr == exp4->sin_addr.S_un.S_addr;
			}
			if (from.sin6_family == AF_INET6 && IN6_IS_ADDR_V4MAPPED(&from.sin6_addr))
			{
				return from.sin6_port == exp4->sin_port &&
					memcmp(from.sin6_addr.u.Byte + 12, &exp4->sin_addr, sizeof(IN_ADDR)) == 0;
			}
			return false;
		}

		if (expected.sin6_family == AF_INET6 && from.sin6_family == AF_INET6)
		{
			return from.sin6_port == expected.sin6_port &&
				memcmp(&from.sin6_addr, &expected.sin6_addr, sizeof(IN6_ADDR)) == 0;
		}

		return false;
	}

	void StopRelay(const shared_ptr<DnsRelay>& relay)
	{
		if (!relay)
			return;

		relay->udp->Stop();
		{
			lock_guard<mutex> lg(g_relayLock);
			if (g_currentRelay == relay)
				g_currentRelay.reset();
		}
		lock_guard<mutex> pl(relay->lock);
		relay->pending.clear();
	}

	void HandleRelayPacket(const weak_ptr<DnsRelay>& weakRelay, const SOCKADDR_IN6& remote, const char* data, int size)
	{
		if (g_stopping.load() || data == nullptr || size < 12 || !IsSameDnsEndpoint(remote, dnsAddr))
			return;

		auto relay = weakRelay.lock();
		if (!relay || relay->udp->stopping.load())
			return;

		USHORT wireId = 0;
		memcpy(&wireId, data, sizeof(wireId));

		PendingQuery query;
		{
			lock_guard<mutex> lg(relay->lock);
			auto it = relay->pending.find(wireId);
			if (it == relay->pending.end())
				return;

			query = std::move(it->second);
			relay->pending.erase(it);
			relay->unansweredExpired = 0;
		}

		vector<char> mutablePacket(data, data + size);
		memcpy(mutablePacket.data(), &query.originalId, sizeof(query.originalId));
		PNF_UDP_OPTIONS opts = query.options.empty() ? nullptr : reinterpret_cast<PNF_UDP_OPTIONS>(query.options.data());
		nf_udpPostReceive(query.id, reinterpret_cast<PBYTE>(&query.target), mutablePacket.data(), size, opts);
	}

	shared_ptr<DnsRelay> GetRelay(bool allowConnect)
	{
		{
			lock_guard<mutex> lg(g_relayLock);
			if (g_currentRelay && !g_currentRelay->udp->stopping.load())
				return g_currentRelay;
		}

		if (!allowConnect || g_stopping.load())
			return nullptr;

		lock_guard<mutex> setup(g_relaySetupLock);
		{
			lock_guard<mutex> lg(g_relayLock);
			if (g_currentRelay && !g_currentRelay->udp->stopping.load())
				return g_currentRelay;
		}

		if (g_stopping.load() || NowMs() < g_relayRetryAfterMs.load())
			return nullptr;

		auto udp = make_shared<SocksHelper::UDP>();
		if (!udp->Associate() || !udp->CreateUDP())
		{
			udp->Stop();
			g_relayRetryAfterMs = NowMs() + RelayRetryCooldownMs;
			return nullptr;
		}

		auto relay = make_shared<DnsRelay>();
		relay->udp = udp;
		relay->nextId = static_cast<USHORT>(GetTickCount64());

		weak_ptr<DnsRelay> weakRelay = relay;
		if (!udp->StartAsync(
			[weakRelay](const SOCKADDR_IN6& remote, const char* data, int size)
			{
				HandleRelayPacket(weakRelay, remote, data, size);
			},
			[weakRelay]()
			{
				if (auto locked = weakRelay.lock())
					StopRelay(locked);
			}))
		{
			udp->Stop();
			g_relayRetryAfterMs = NowMs() + RelayRetryCooldownMs;
			return nullptr;
		}

		lock_guard<mutex> lg(g_relayLock);
		if (g_stopping.load())
		{
			udp->Stop();
			return nullptr;
		}
		g_currentRelay = relay;
		return relay;
	}

	bool SendViaRelay(const DnsQuery& query, bool allowConnect)
	{
		auto relay = GetRelay(allowConnect);
		if (!relay)
			return false;

		PendingQuery pending;
		pending.id = query.id;
		pending.target = query.target;
		pending.options = query.options;
		memcpy(&pending.originalId, query.packet.data(), sizeof(pending.originalId));

		const auto now = chrono::steady_clock::now();
		pending.expires = now + DnsQueryTimeout;

		USHORT wireId = 0;
		bool recycle = false;
		{
			lock_guard<mutex> lg(relay->lock);

			for (auto it = relay->pending.begin(); it != relay->pending.end();)
			{
				if (it->second.expires <= now)
				{
					it = relay->pending.erase(it);
					relay->unansweredExpired++;
				}
				else
				{
					++it;
				}
			}

			if (relay->unansweredExpired >= RecycleAfterUnansweredQueries)
			{
				recycle = true;
			}
			else
			{
				if (relay->pending.size() >= MaxPendingQueries)
					return false;

				do
				{
					wireId = relay->nextId++;
				} while (relay->pending.find(wireId) != relay->pending.end());

				relay->pending.emplace(wireId, std::move(pending));
			}
		}

		if (recycle)
		{
			StopRelay(relay);
			return false;
		}

		vector<char> packet = query.packet;
		memcpy(packet.data(), &wireId, sizeof(wireId));

		const int length = static_cast<int>(packet.size());
		int sent = SOCKET_ERROR;
		{
			lock_guard<mutex> sl(relay->udp->sendLock);
			if (!relay->udp->stopping.load())
				sent = relay->udp->Send(&dnsAddr, packet.data(), length);
		}
		if (sent != length)
		{
			{
				lock_guard<mutex> lg(relay->lock);
				relay->pending.erase(wireId);
			}
			StopRelay(relay);
			return false;
		}

		return true;
	}

	bool PostDirectRecv(const shared_ptr<DirectDnsState>& state);

	void OnDirectRecvComplete(IocpOperation* op, DWORD error, DWORD bytes)
	{
		auto* recvOp = static_cast<DirectRecvOp*>(op);
		auto state = std::move(recvOp->owner);
		if (!state)
			return;

		const bool truncated = (error == ERROR_MORE_DATA || error == WSAEMSGSIZE);
		if ((error == NO_ERROR || truncated) && !state->stopping.load() && !g_stopping.load())
		{
			if (error == NO_ERROR && bytes >= 12 && IsSameDnsEndpoint(recvOp->fromAddr, dnsAddr))
			{
				USHORT wireId = 0;
				memcpy(&wireId, recvOp->buf.data(), sizeof(wireId));

				PendingQuery query;
				bool found = false;
				{
					lock_guard<mutex> lg(state->lock);
					auto it = state->pending.find(wireId);
					if (it != state->pending.end())
					{
						query = std::move(it->second);
						state->pending.erase(it);
						found = true;
					}
				}

				if (found)
				{
					memcpy(recvOp->buf.data(), &query.originalId, sizeof(query.originalId));
					PNF_UDP_OPTIONS opts = query.options.empty() ? nullptr : reinterpret_cast<PNF_UDP_OPTIONS>(query.options.data());
					nf_udpPostReceive(
						query.id,
						reinterpret_cast<PBYTE>(&query.target),
						recvOp->buf.data(),
						static_cast<int>(bytes),
						opts);
				}
			}

			if (!PostDirectRecv(state))
				state->Close();
		}
		else
		{
			state->Close();
		}

		auto* statePtr = state.get();
		statePtr->ReleaseIoOp();
	}

	bool PostDirectRecv(const shared_ptr<DirectDnsState>& state)
	{
		lock_guard<mutex> sl(state->sockLock);
		const SOCKET s = state->sock.load();
		if (s == INVALID_SOCKET || state->stopping.load() || g_stopping.load() || !state->recvOp)
			return false;

		ZeroMemory(&state->recvOp->overlapped, sizeof(OVERLAPPED));
		state->recvOp->complete = &OnDirectRecvComplete;
		state->recvOp->flags = 0;
		state->recvOp->fromLen = sizeof(SOCKADDR_IN6);
		memset(&state->recvOp->fromAddr, 0, sizeof(state->recvOp->fromAddr));
		state->recvOp->owner = state;

		state->AcquireIoOp();

		WSABUF wbuf{ static_cast<ULONG>(state->recvOp->buf.size()), state->recvOp->buf.data() };
		const int rc = WSARecvFrom(s, &wbuf, 1, NULL, &state->recvOp->flags,
			reinterpret_cast<PSOCKADDR>(&state->recvOp->fromAddr), &state->recvOp->fromLen,
			&state->recvOp->overlapped, NULL);
		if (rc == SOCKET_ERROR)
		{
			const int err = WSAGetLastError();
			if (err != WSA_IO_PENDING)
			{
				state->recvOp->owner.reset();
				state->ReleaseIoOp();
				return false;
			}
		}
		return true;
	}

	shared_ptr<DirectDnsState> EnsureDirectState()
	{
		{
			lock_guard<mutex> lg(g_directLock);
			if (g_directState && !g_directState->stopping.load() && g_directState->sock.load() != INVALID_SOCKET)
				return g_directState;
		}

		lock_guard<mutex> lg(g_directLock);
		if (g_stopping.load())
			return nullptr;
		if (g_directState && !g_directState->stopping.load() && g_directState->sock.load() != INVALID_SOCKET)
			return g_directState;

		const int family = dnsAddr.sin6_family == AF_INET ? AF_INET : AF_INET6;
		const SOCKET s = WSASocketW(family, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, WSA_FLAG_OVERLAPPED);
		if (s == INVALID_SOCKET)
			return nullptr;

		bool bound = false;
		if (family == AF_INET)
		{
			SOCKADDR_IN addr{};
			addr.sin_family = AF_INET;
			bound = ::bind(s, reinterpret_cast<PSOCKADDR>(&addr), sizeof(addr)) != SOCKET_ERROR;
		}
		else
		{
			int v6only = 0;
			setsockopt(s, IPPROTO_IPV6, IPV6_V6ONLY, reinterpret_cast<const char*>(&v6only), sizeof(v6only));
			SOCKADDR_IN6 addr;
			IN6ADDR_SETANY(&addr);
			bound = ::bind(s, reinterpret_cast<PSOCKADDR>(&addr), sizeof(addr)) != SOCKET_ERROR;
		}
		if (!bound)
		{
			closesocket(s);
			return nullptr;
		}

		const int udpBufferSize = 256 * 1024;
		setsockopt(s, SOL_SOCKET, SO_RCVBUF, reinterpret_cast<const char*>(&udpBufferSize), sizeof(udpBufferSize));
		setsockopt(s, SOL_SOCKET, SO_SNDBUF, reinterpret_cast<const char*>(&udpBufferSize), sizeof(udpBufferSize));
		BOOL reportConnReset = FALSE;
		DWORD returned = 0;
		WSAIoctl(s, SIO_UDP_CONNRESET, &reportConnReset, sizeof(reportConnReset), NULL, 0, &returned, NULL, NULL);

		if (!IocpPool::Associate(reinterpret_cast<HANDLE>(s)))
		{
			closesocket(s);
			return nullptr;
		}

		auto state = make_shared<DirectDnsState>();
		state->sock.store(s);
		state->nextId = static_cast<USHORT>(GetTickCount64());
		state->recvOp = make_unique<DirectRecvOp>();
		if (!PostDirectRecv(state))
		{
			state->Close();
			state->WaitDrained();
			return nullptr;
		}

		g_directState = state;
		return state;
	}

	bool SendViaDirect(const DnsQuery& query)
	{
		auto state = EnsureDirectState();
		if (!state)
			return false;

		PendingQuery pending;
		pending.id = query.id;
		pending.target = query.target;
		pending.options = query.options;
		memcpy(&pending.originalId, query.packet.data(), sizeof(pending.originalId));

		const auto now = chrono::steady_clock::now();
		pending.expires = now + DirectDnsQueryTimeout;

		USHORT wireId = 0;
		{
			lock_guard<mutex> lg(state->lock);
			for (auto it = state->pending.begin(); it != state->pending.end();)
			{
				if (it->second.expires <= now)
					it = state->pending.erase(it);
				else
					++it;
			}
			if (state->pending.size() >= MaxPendingQueries)
				return false;

			do
			{
				wireId = state->nextId++;
			} while (state->pending.find(wireId) != state->pending.end());
			state->pending.emplace(wireId, std::move(pending));
		}

		vector<char> packet = query.packet;
		memcpy(packet.data(), &wireId, sizeof(wireId));

		const int targetLength = dnsAddr.sin6_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
		const int len = static_cast<int>(packet.size());
		int sentBytes = SOCKET_ERROR;
		{
			lock_guard<mutex> sl(state->sockLock);
			const SOCKET s = state->sock.load();
			if (s != INVALID_SOCKET && !state->stopping.load())
				sentBytes = sendto(s, packet.data(), len, 0, reinterpret_cast<PSOCKADDR>(&dnsAddr), targetLength);
		}
		if (sentBytes != len)
		{
			lock_guard<mutex> lg(state->lock);
			state->pending.erase(wireId);
			return false;
		}

		return true;
	}

	void DnsWorkerLoop()
	{
		BoostRelayThread();

		for (;;)
		{
			DnsQuery query;
			{
				unique_lock<mutex> ul(g_workerLock);
				g_workerCv.wait(ul, []() { return g_stopping.load() || !g_setupQueue.empty(); });
				if (g_stopping.load() && g_setupQueue.empty())
					break;
				query = std::move(g_setupQueue.front());
				g_setupQueue.pop_front();
			}

			if (g_stopping.load())
				continue;

			SendViaRelay(query, true);
		}
	}
}

bool DNSHandler::INIT()
{
	DNSHandler::FREE();

	memset(&dnsAddr, 0, sizeof(dnsAddr));

	string hostCopy;
	{
		shared_lock<shared_mutex> sl(configLock);
		hostCopy = dnsHost;
	}
	const USHORT port = dnsPort.load();
	if (port == 0)
		return false;

	bool parsed = false;
	auto ipv4 = (PSOCKADDR_IN)&dnsAddr;
	if (inet_pton(AF_INET, hostCopy.c_str(), &ipv4->sin_addr) == 1)
	{
		ipv4->sin_family = AF_INET;
		ipv4->sin_port = htons(port);
		parsed = true;
	}
	else
	{
		auto ipv6 = (PSOCKADDR_IN6)&dnsAddr;
		if (inet_pton(AF_INET6, hostCopy.c_str(), &ipv6->sin6_addr) == 1)
		{
			ipv6->sin6_family = AF_INET6;
			ipv6->sin6_port = htons(port);
			parsed = true;
		}
	}

	if (!parsed)
		return false;

	if (!IocpPool::Init())
		return false;
	g_dnsInitialized = true;

	g_stopping = false;
	try
	{
		for (int i = 0; i < 2; ++i)
			g_setupWorkers.emplace_back(DnsWorkerLoop);
	}
	catch (...)
	{
		DNSHandler::FREE();
		return false;
	}

	return true;
}

void DNSHandler::FREE()
{
	g_stopping = true;
	g_workerCv.notify_all();

	for (auto& w : g_setupWorkers)
	{
		if (w.joinable())
			w.join();
	}
	g_setupWorkers.clear();

	{
		lock_guard<mutex> lg(g_workerLock);
		g_setupQueue.clear();
	}

	shared_ptr<DnsRelay> relay;
	{
		lock_guard<mutex> lg(g_relayLock);
		relay.swap(g_currentRelay);
	}
	if (relay)
		StopRelay(relay);

	shared_ptr<DirectDnsState> direct;
	{
		lock_guard<mutex> lg(g_directLock);
		direct.swap(g_directState);
	}
	if (direct)
	{
		direct->Close();
		direct->WaitDrained();
		lock_guard<mutex> lg(direct->lock);
		direct->pending.clear();
	}

	g_relayRetryAfterMs = 0;
	SocksHelper::UDP::WaitAllIoDrained();
	if (std::exchange(g_dnsInitialized, false))
		IocpPool::Shutdown();
}

bool DNSHandler::IsDNS(PSOCKADDR_IN6 target)
{
	if (target == nullptr)
		return false;
	if (target->sin6_family == AF_INET)
		return ((PSOCKADDR_IN)target)->sin_port == htons(53);
	if (target->sin6_family == AF_INET6)
		return target->sin6_port == htons(53);
	return false;
}

void DNSHandler::CreateHandler(ENDPOINT_ID id, PSOCKADDR_IN6 target, const char* packet, int length, PNF_UDP_OPTIONS options)
{
	// DNS header is 12 bytes; reject short/invalid datagrams.
	if (target == nullptr || packet == nullptr || length < 12 || g_stopping.load())
		return;
	if (target->sin6_family != AF_INET && target->sin6_family != AF_INET6)
		return;

	DnsQuery query;
	query.id = id;
	const size_t addrLen = target->sin6_family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
	memcpy(&query.target, target, addrLen);
	query.options = CopyOptions(options);
	query.packet.assign(packet, packet + length);

	if (dnsProx.load())
	{
		if (SendViaRelay(query, false))
			return;

		{
			lock_guard<mutex> lg(g_workerLock);
			if (g_stopping.load() || g_setupQueue.size() >= MaxPendingSetupQueue)
				return;
			g_setupQueue.push_back(std::move(query));
		}
		g_workerCv.notify_one();
		return;
	}

	SendViaDirect(query);
}

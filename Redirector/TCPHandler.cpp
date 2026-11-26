#include "TCPHandler.h"

#include "ConnectionTracker.h"
#include "IocpPool.h"
#include "Utils.h"

extern atomic_ullong UP;
extern atomic_ullong DL;

constexpr size_t TcpRelayBufferSize = 64 * 1024;
constexpr size_t MaxSetupQueue = 1024;
constexpr auto TcpContextTtl = chrono::seconds(30);

atomic<USHORT> tcpListen{ 0 };

namespace
{
	struct ContextEntry
	{
		ENDPOINT_ID endpointId = 0;
		SOCKADDR_IN6 remote{};
		uint64_t connId = 0;
		chrono::steady_clock::time_point createdAt;
	};

	using ContextKey = pair<bool, USHORT>; // (isNativeIPv6, network-order port)

	ContextKey MakeContextKey(const SOCKADDR_IN6& addr)
	{
		if (addr.sin6_family == AF_INET)
			return { false, reinterpret_cast<const SOCKADDR_IN*>(&addr)->sin_port };
		const bool isNativeV6 = !IN6_IS_ADDR_V4MAPPED(&addr.sin6_addr);
		return { isNativeV6, addr.sin6_port };
	}

	bool IsLoopbackPeer(const SOCKADDR_IN6& addr)
	{
		if (addr.sin6_family == AF_INET)
		{
			const auto* a4 = reinterpret_cast<const SOCKADDR_IN*>(&addr);
			return (ntohl(a4->sin_addr.S_un.S_addr) & 0xFF000000u) == 0x7F000000u;
		}
		if (addr.sin6_family == AF_INET6)
		{
			if (IN6_IS_ADDR_LOOPBACK(&addr.sin6_addr))
				return true;
			if (IN6_IS_ADDR_V4MAPPED(&addr.sin6_addr))
				return addr.sin6_addr.u.Byte[12] == 127;
		}
		return false;
	}

	struct TcpSession;

	struct TcpPumpOp : IocpOperation
	{
		struct TcpPump* pump = nullptr;
		shared_ptr<TcpSession> session;
	};

	struct TcpPump
	{
		TcpPumpOp readOp;
		TcpPumpOp writeOp;
		SOCKET src = INVALID_SOCKET;
		SOCKET dst = INVALID_SOCKET;
		atomic_ullong* counter = nullptr;
		bool isUpload = false;
		array<char, TcpRelayBufferSize> buf{};
		DWORD writeOffset = 0;
		DWORD writeTotal = 0;
		DWORD readFlags = 0;
	};

	struct TcpSession : enable_shared_from_this<TcpSession>
	{
		mutex ioLock;
		uint64_t connId = 0;
		atomic<SOCKET> client{ INVALID_SOCKET };
		shared_ptr<SocksHelper::TCP> remote;
		TcpPump c2r;
		TcpPump r2c;
		atomic<int> openDirections{ 2 };
		// Starts with 1 setup reference held by HandleClient until initial PostReads finish.
		atomic<int> inFlightOps{ 1 };
		atomic<bool> closed{ false };

		~TcpSession()
		{
			ForceClose();
		}

		void ForceClose();
	};

	mutex g_tcpLock;
	atomic<SOCKET> g_tcpSocket{ INVALID_SOCKET };
	map<ContextKey, ContextEntry> g_tcpContext;

	mutex g_setupLock;
	condition_variable g_setupCv;
	deque<SOCKET> g_setupQueue;
	atomic<bool> g_stopping{ true };
	thread g_acceptThread;
	vector<thread> g_setupWorkers;

	mutex g_sessionLock;
	condition_variable g_sessionsCv;
	unordered_set<shared_ptr<TcpSession>> g_activeSessions;

	void TcpSession_CloseOnly(TcpSession* session, const char* reason = "Closed", bool isError = false, const char* detail = "")
	{
		uint64_t connIdToClose = 0;
		{
			lock_guard<mutex> io(session->ioLock);
			if (!session->closed.exchange(true))
			{
				connIdToClose = session->connId;
				session->c2r.src = INVALID_SOCKET;
				session->c2r.dst = INVALID_SOCKET;
				session->r2c.src = INVALID_SOCKET;
				session->r2c.dst = INVALID_SOCKET;

				const SOCKET c = session->client.exchange(INVALID_SOCKET);
				if (c != INVALID_SOCKET)
				{
					shutdown(c, SD_BOTH);
					closesocket(c);
				}
				if (session->remote)
					session->remote->Close();
			}
		}

		if (connIdToClose != 0)
			ConnectionTracker::MarkClosed(connIdToClose, reason, isError, detail);
	}

	void OnOpFinished(const shared_ptr<TcpSession>& session)
	{
		if (session->inFlightOps.fetch_sub(1) == 1)
		{
			TcpSession_CloseOnly(session.get(), g_stopping.load() ? "Redirector stopped" : "Closed");
			lock_guard<mutex> lg(g_sessionLock);
			g_activeSessions.erase(session);
			if (g_activeSessions.empty())
				g_sessionsCv.notify_all();
		}
	}

	bool PostRead(TcpPump* pump, const shared_ptr<TcpSession>& session);
	bool PostWrite(TcpPump* pump, const shared_ptr<TcpSession>& session);

	void OnReadComplete(IocpOperation* op, DWORD error, DWORD bytes)
	{
		auto* pumpOp = static_cast<TcpPumpOp*>(op);
		auto session = std::move(pumpOp->session);
		auto* pump = pumpOp->pump;
		if (!session || pump == nullptr)
			return;

		if (error != NO_ERROR || session->closed.load() || g_stopping.load())
		{
			if (g_stopping.load())
				TcpSession_CloseOnly(session.get(), "Redirector stopped");
			else if (error != NO_ERROR && error != ERROR_OPERATION_ABORTED && error != WSAECONNABORTED)
			{
				const string errDetail = "Win32 error " + to_string(error);
				TcpSession_CloseOnly(session.get(), "TCP read error", true, errDetail.c_str());
			}
			else
			{
				session->ForceClose();
			}
			OnOpFinished(session);
			return;
		}

		if (bytes == 0)
		{
			{
				lock_guard<mutex> io(session->ioLock);
				if (!session->closed.load() && pump->dst != INVALID_SOCKET)
					shutdown(pump->dst, SD_SEND);
			}
			if (session->openDirections.fetch_sub(1) == 1)
				session->ForceClose();
			OnOpFinished(session);
			return;
		}

		if (pump->counter != nullptr)
			*pump->counter += bytes;
		if (session->connId != 0)
		{
			if (pump->isUpload)
				ConnectionTracker::AddBytes(session->connId, bytes, 0);
			else
				ConnectionTracker::AddBytes(session->connId, 0, bytes);
		}

		pump->writeOffset = 0;
		pump->writeTotal = bytes;
		if (!PostWrite(pump, session))
			session->ForceClose();

		OnOpFinished(session);
	}

	void OnWriteComplete(IocpOperation* op, DWORD error, DWORD bytes)
	{
		auto* pumpOp = static_cast<TcpPumpOp*>(op);
		auto session = std::move(pumpOp->session);
		auto* pump = pumpOp->pump;
		if (!session || pump == nullptr)
			return;

		if (error != NO_ERROR || bytes == 0 || session->closed.load() || g_stopping.load())
		{
			session->ForceClose();
			OnOpFinished(session);
			return;
		}

		pump->writeOffset += bytes;
		if (pump->writeOffset < pump->writeTotal)
		{
			if (!PostWrite(pump, session))
				session->ForceClose();
		}
		else
		{
			if (!PostRead(pump, session))
				session->ForceClose();
		}

		OnOpFinished(session);
	}

	bool PostRead(TcpPump* pump, const shared_ptr<TcpSession>& session)
	{
		lock_guard<mutex> io(session->ioLock);
		if (session->closed.load() || g_stopping.load() || pump->src == INVALID_SOCKET)
			return false;

		ZeroMemory(&pump->readOp.overlapped, sizeof(OVERLAPPED));
		pump->readOp.complete = &OnReadComplete;
		pump->readOp.pump = pump;
		pump->readOp.session = session;
		pump->readFlags = 0;

		session->inFlightOps.fetch_add(1);
		WSABUF wbuf{ static_cast<ULONG>(pump->buf.size()), pump->buf.data() };
		const int rc = WSARecv(pump->src, &wbuf, 1, NULL, &pump->readFlags, &pump->readOp.overlapped, NULL);
		if (rc == SOCKET_ERROR)
		{
			const int err = WSAGetLastError();
			if (err != WSA_IO_PENDING)
			{
				pump->readOp.session.reset();
				session->inFlightOps.fetch_sub(1);
				return false;
			}
		}
		return true;
	}

	bool PostWrite(TcpPump* pump, const shared_ptr<TcpSession>& session)
	{
		lock_guard<mutex> io(session->ioLock);
		if (session->closed.load() || g_stopping.load() || pump->dst == INVALID_SOCKET)
			return false;

		ZeroMemory(&pump->writeOp.overlapped, sizeof(OVERLAPPED));
		pump->writeOp.complete = &OnWriteComplete;
		pump->writeOp.pump = pump;
		pump->writeOp.session = session;

		session->inFlightOps.fetch_add(1);
		WSABUF wbuf{
			pump->writeTotal - pump->writeOffset,
			pump->buf.data() + pump->writeOffset
		};
		const int rc = WSASend(pump->dst, &wbuf, 1, NULL, 0, &pump->writeOp.overlapped, NULL);
		if (rc == SOCKET_ERROR)
		{
			const int err = WSAGetLastError();
			if (err != WSA_IO_PENDING)
			{
				pump->writeOp.session.reset();
				session->inFlightOps.fetch_sub(1);
				return false;
			}
		}
		return true;
	}

	void PruneExpiredContextsLocked(chrono::steady_clock::time_point now, vector<uint64_t>& expiredConnIds)
	{
		for (auto it = g_tcpContext.begin(); it != g_tcpContext.end();)
		{
			if (now - it->second.createdAt > TcpContextTtl)
			{
				if (it->second.connId != 0)
					expiredConnIds.push_back(it->second.connId);
				it = g_tcpContext.erase(it);
			}
			else
			{
				++it;
			}
		}
	}

	void HandleClient(SOCKET client)
	{
		if (g_stopping.load())
		{
			closesocket(client);
			return;
		}

		SOCKADDR_IN6 peerAddr{};
		{
			int addrLength = sizeof(SOCKADDR_IN6);
			if (getpeername(client, (PSOCKADDR)&peerAddr, &addrLength) == SOCKET_ERROR || !IsLoopbackPeer(peerAddr))
			{
				closesocket(client);
				return;
			}
		}
		const ContextKey id = MakeContextKey(peerAddr);

		SOCKADDR_IN6 target{};
		uint64_t connId = 0;
		vector<uint64_t> expiredConnIds;
		bool found = false;
		{
			lock_guard<mutex> lg(g_tcpLock);
			const auto now = chrono::steady_clock::now();
			PruneExpiredContextsLocked(now, expiredConnIds);

			auto it = g_tcpContext.find(id);
			if (it != g_tcpContext.end())
			{
				target = it->second.remote;
				connId = it->second.connId;
				g_tcpContext.erase(it);
				found = true;
			}
		}

		for (uint64_t expiredId : expiredConnIds)
			ConnectionTracker::MarkClosed(expiredId, "Accept timeout", true);

		if (!found)
		{
			closesocket(client);
			return;
		}

		auto remote = make_shared<SocksHelper::TCP>();
		if (!remote->Connect(&target))
		{
			closesocket(client);
			ConnectionTracker::MarkClosed(connId, "SOCKS5 connect failed", true);
			return;
		}

		if (g_stopping.load() || g_tcpSocket.load() == INVALID_SOCKET)
		{
			closesocket(client);
			remote->Close();
			ConnectionTracker::MarkClosed(connId, "Redirector stopped");
			return;
		}

		const SOCKET remoteSocket = remote->tcpSocket.load();
		if (remoteSocket == INVALID_SOCKET ||
			!IocpPool::Associate(reinterpret_cast<HANDLE>(client)) ||
			!IocpPool::Associate(reinterpret_cast<HANDLE>(remoteSocket)))
		{
			closesocket(client);
			remote->Close();
			ConnectionTracker::MarkClosed(connId, "IOCP associate failed", true);
			return;
		}

		auto session = make_shared<TcpSession>();
		session->connId = connId;
		session->client.store(client);
		session->remote = remote;

		session->c2r.src = client;
		session->c2r.dst = remoteSocket;
		session->c2r.counter = &UP;
		session->c2r.isUpload = true;

		session->r2c.src = remoteSocket;
		session->r2c.dst = client;
		session->r2c.counter = &DL;
		session->r2c.isUpload = false;

		{
			lock_guard<mutex> lg(g_sessionLock);
			if (g_stopping.load())
			{
				TcpSession_CloseOnly(session.get(), "Redirector stopped");
				return;
			}
			g_activeSessions.insert(session);
		}

		const bool okC2r = PostRead(&session->c2r, session);
		const bool okR2c = okC2r && PostRead(&session->r2c, session);
		if (!okC2r || !okR2c)
			session->ForceClose();

		OnOpFinished(session);
	}

	void AcceptLoop()
	{
		BoostRelayThread();

		const SOCKET listener = g_tcpSocket.load();
		while (!g_stopping.load() && listener != INVALID_SOCKET && g_tcpSocket.load() == listener)
		{
			auto client = accept(listener, NULL, NULL);
			if (client == INVALID_SOCKET)
			{
				const int lasterr = WSAGetLastError();
				if (g_stopping.load() || g_tcpSocket.load() != listener)
					return;

				if (lasterr != WSAECONNRESET && lasterr != WSAECONNABORTED && lasterr != WSAEINTR)
					printf("[Redirector][TCPHandler::Accept] Accept client failed: %d\n", lasterr);
				Sleep(1);
				continue;
			}

			BOOL noDelay = TRUE;
			if (setsockopt(client, IPPROTO_TCP, TCP_NODELAY, (char*)&noDelay, sizeof(noDelay)) == SOCKET_ERROR)
				printf("[Redirector][TCPHandler::Accept] Disable Nagle failed: %d\n", WSAGetLastError());

			bool queued = false;
			{
				lock_guard<mutex> lg(g_setupLock);
				if (!g_stopping.load() && g_setupQueue.size() < MaxSetupQueue)
				{
					g_setupQueue.push_back(client);
					queued = true;
				}
			}

			if (queued)
				g_setupCv.notify_one();
			else
				closesocket(client);
		}
	}

	void SetupWorkerLoop()
	{
		BoostRelayThread();

		for (;;)
		{
			SOCKET client = INVALID_SOCKET;
			{
				unique_lock<mutex> ul(g_setupLock);
				g_setupCv.wait(ul, []() { return g_stopping.load() || !g_setupQueue.empty(); });
				if (g_stopping.load() && g_setupQueue.empty())
					break;
				client = g_setupQueue.front();
				g_setupQueue.pop_front();
			}

			if (g_stopping.load())
			{
				closesocket(client);
				continue;
			}

			HandleClient(client);
		}
	}
}

void TcpSession::ForceClose()
{
	TcpSession_CloseOnly(this);
}

bool TCPHandler::INIT()
{
	TCPHandler::FREE();

	SOCKET client = WSASocketW(AF_INET6, SOCK_STREAM, IPPROTO_TCP, NULL, 0, WSA_FLAG_OVERLAPPED);
	if (client == INVALID_SOCKET)
	{
		printf("[Redirector][TCPHandler::INIT] Create socket failed: %d\n", WSAGetLastError());
		return false;
	}

	{
		int v6only = 0;
		if (setsockopt(client, IPPROTO_IPV6, IPV6_V6ONLY, (char*)&v6only, sizeof(v6only)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::INIT] Set socket option failed: %d\n", WSAGetLastError());
			closesocket(client);
			return false;
		}
	}

	{
		SOCKADDR_IN6 addr;
		IN6ADDR_SETANY(&addr);

		if (::bind(client, (PSOCKADDR)&addr, sizeof(SOCKADDR_IN6)) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::INIT] Bind socket failed: %d\n", WSAGetLastError());
			closesocket(client);
			return false;
		}
	}
	if (listen(client, 1024) == SOCKET_ERROR)
	{
		printf("[Redirector][TCPHandler::INIT] Listen socket failed: %d\n", WSAGetLastError());
		closesocket(client);
		return false;
	}

	{
		SOCKADDR_IN6 addr;
		int addrLength = sizeof(SOCKADDR_IN6);
		if (getsockname(client, (PSOCKADDR)&addr, &addrLength) == SOCKET_ERROR)
		{
			printf("[Redirector][TCPHandler::INIT] Get listen address failed: %d\n", WSAGetLastError());
			closesocket(client);
			return false;
		}

		tcpListen = (addr.sin6_family == AF_INET6) ? addr.sin6_port : ((PSOCKADDR_IN)&addr)->sin_port;
	}

	{
		lock_guard<mutex> lg(g_tcpLock);
		g_tcpContext.clear();
		g_tcpSocket.store(client);
	}

	g_stopping = false;
	const unsigned hw = thread::hardware_concurrency();
	const unsigned workerCount = (std::clamp)(hw == 0 ? 4u : hw, 2u, 16u);
	try
	{
		g_setupWorkers.reserve(workerCount);
		for (unsigned i = 0; i < workerCount; ++i)
			g_setupWorkers.emplace_back(SetupWorkerLoop);
		g_acceptThread = thread(AcceptLoop);
	}
	catch (...)
	{
		TCPHandler::FREE();
		return false;
	}

	return true;
}

void TCPHandler::FREE()
{
	g_stopping = true;

	{
		lock_guard<mutex> lg(g_tcpLock);
		const SOCKET listener = g_tcpSocket.exchange(INVALID_SOCKET);
		if (listener != INVALID_SOCKET)
			closesocket(listener);
		tcpListen = 0;
	}

	g_setupCv.notify_all();

	if (g_acceptThread.joinable())
		g_acceptThread.join();

	for (auto& worker : g_setupWorkers)
	{
		if (worker.joinable())
			worker.join();
	}
	g_setupWorkers.clear();

	{
		lock_guard<mutex> lg(g_setupLock);
		while (!g_setupQueue.empty())
		{
			closesocket(g_setupQueue.front());
			g_setupQueue.pop_front();
		}
	}

	vector<shared_ptr<TcpSession>> sessions;
	{
		lock_guard<mutex> lg(g_sessionLock);
		sessions.assign(g_activeSessions.begin(), g_activeSessions.end());
	}
	for (const auto& session : sessions)
		TcpSession_CloseOnly(session.get(), "Redirector stopped");

	{
		unique_lock<mutex> ul(g_sessionLock);
		g_sessionsCv.wait(ul, []() { return g_activeSessions.empty(); });
	}

	vector<uint64_t> pendingConnIds;
	{
		lock_guard<mutex> lg(g_tcpLock);
		for (const auto& [k, entry] : g_tcpContext)
		{
			UNREFERENCED_PARAMETER(k);
			if (entry.connId != 0)
				pendingConnIds.push_back(entry.connId);
		}
		g_tcpContext.clear();
	}
	for (uint64_t pendingId : pendingConnIds)
		ConnectionTracker::MarkClosed(pendingId, "Redirector stopped");
}

void TCPHandler::CreateHandler(ENDPOINT_ID id, SOCKADDR_IN6 client, SOCKADDR_IN6 remote, uint64_t connId)
{
	vector<uint64_t> expiredConnIds;
	{
		lock_guard<mutex> lg(g_tcpLock);

		const auto now = chrono::steady_clock::now();
		PruneExpiredContextsLocked(now, expiredConnIds);

		const ContextKey key = MakeContextKey(client);
		auto existingIt = g_tcpContext.find(key);
		if (existingIt != g_tcpContext.end() && existingIt->second.connId != 0)
			expiredConnIds.push_back(existingIt->second.connId);

		g_tcpContext[key] = { id, remote, connId, now };
	}

	for (uint64_t expiredId : expiredConnIds)
		ConnectionTracker::MarkClosed(expiredId, "Replaced or expired", true);
}

void TCPHandler::DeleteHandler(ENDPOINT_ID id, SOCKADDR_IN6 client)
{
	uint64_t removedConnId = 0;
	{
		lock_guard<mutex> lg(g_tcpLock);

		const ContextKey key = MakeContextKey(client);
		auto it = g_tcpContext.find(key);
		if (it != g_tcpContext.end() && it->second.endpointId == id)
		{
			removedConnId = it->second.connId;
			g_tcpContext.erase(it);
		}
	}

	if (removedConnId != 0)
		ConnectionTracker::MarkClosed(removedConnId, "Closed before accept");
}

bool TCPHandler::CloseSession(uint64_t connId)
{
	if (connId == 0)
		return false;

	bool erasedPending = false;
	{
		lock_guard<mutex> lg(g_tcpLock);
		for (auto it = g_tcpContext.begin(); it != g_tcpContext.end();)
		{
			if (it->second.connId == connId)
			{
				it = g_tcpContext.erase(it);
				erasedPending = true;
			}
			else
			{
				++it;
			}
		}
	}

	shared_ptr<TcpSession> targetSession;
	{
		lock_guard<mutex> lg(g_sessionLock);
		for (const auto& session : g_activeSessions)
		{
			if (session->connId == connId)
			{
				targetSession = session;
				break;
			}
		}
	}

	if (targetSession)
	{
		TcpSession_CloseOnly(targetSession.get(), "Closed by user");
		return true;
	}

	if (erasedPending)
	{
		ConnectionTracker::MarkClosed(connId, "Closed by user");
		return true;
	}

	return false;
}

void TCPHandler::CloseAllSessions()
{
	vector<uint64_t> pendingConnIds;
	{
		lock_guard<mutex> lg(g_tcpLock);
		for (const auto& [k, entry] : g_tcpContext)
		{
			UNREFERENCED_PARAMETER(k);
			if (entry.connId != 0)
				pendingConnIds.push_back(entry.connId);
		}
		g_tcpContext.clear();
	}
	for (uint64_t pendingId : pendingConnIds)
		ConnectionTracker::MarkClosed(pendingId, "Closed all by user");

	vector<shared_ptr<TcpSession>> sessions;
	{
		lock_guard<mutex> lg(g_sessionLock);
		sessions.assign(g_activeSessions.begin(), g_activeSessions.end());
	}
	for (const auto& session : sessions)
		TcpSession_CloseOnly(session.get(), "Closed all by user");
}

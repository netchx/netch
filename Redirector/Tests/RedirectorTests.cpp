#include "../Based.h"
#include "../DNSHandler.h"
#include "../EventHandler.h"
#include "../IocpPool.h"
#include "../IPEventHandler.h"
#include "../SocksHelper.h"
#include "../TCPHandler.h"
#include "../Utils.h"

#include <cassert>
#include <cstring>

extern "C" {
	BOOL __cdecl aio_dial(int name, LPWSTR value);
	BOOL __cdecl aio_init();
	void __cdecl aio_free();
	ULONG64 __cdecl aio_getUP();
	ULONG64 __cdecl aio_getDL();
	int __cdecl aio_getConnectionsJson(char* buffer, int capacity);
	int __cdecl aio_drainConnectionEventsJson(ULONG64 afterSeq, char* buffer, int capacity);
	BOOL __cdecl aio_closeConnection(ULONG64 connId);
	void __cdecl aio_closeAllConnections();
	void __cdecl aio_clearClosedConnections();
}

extern atomic<USHORT> tcpListen;

namespace
{
	mutex g_mockLock;
	condition_variable g_mockCv;

	NF_STATUS g_mockNfInitStatus = NF_STATUS_SUCCESS;
	int g_mockRulesAdded = 0;
	int g_mockRulesDeleted = 0;
	bool g_mockNfInitialized = false;
	vector<ENDPOINT_ID> g_disabledTcpEndpoints;
	vector<ENDPOINT_ID> g_disabledUdpEndpoints;
	int g_directUdpSends = 0;

	struct CapturedUdpReceive
	{
		ENDPOINT_ID id = 0;
		SOCKADDR_STORAGE remote{};
		vector<char> payload;
		vector<char> options;
	};
	vector<CapturedUdpReceive> g_udpReceives;

	struct CapturedIpReceive
	{
		vector<char> packet;
		NF_IP_PACKET_OPTIONS options{};
		chrono::steady_clock::time_point receivedAt;
	};
	vector<CapturedIpReceive> g_ipReceives;

	wstring g_mockProcessName = L"C:\\Games\\game.exe";

	void ResetMocks()
	{
		lock_guard<mutex> lg(g_mockLock);
		g_mockNfInitStatus = NF_STATUS_SUCCESS;
		g_mockRulesAdded = 0;
		g_mockRulesDeleted = 0;
		g_mockNfInitialized = false;
		g_disabledTcpEndpoints.clear();
		g_disabledUdpEndpoints.clear();
		g_directUdpSends = 0;
		g_udpReceives.clear();
		g_ipReceives.clear();
		g_mockProcessName = L"C:\\Games\\game.exe";
	}
}

extern "C" {
	NF_STATUS __cdecl nf_init(const char* driverName, NF_EventHandler* pHandler)
	{
		UNREFERENCED_PARAMETER(driverName);
		UNREFERENCED_PARAMETER(pHandler);
		lock_guard<mutex> lg(g_mockLock);
		if (g_mockNfInitStatus != NF_STATUS_SUCCESS)
			return g_mockNfInitStatus;
		g_mockNfInitialized = true;
		return NF_STATUS_SUCCESS;
	}

	void __cdecl nf_free()
	{
		lock_guard<mutex> lg(g_mockLock);
		g_mockNfInitialized = false;
	}

	NF_STATUS __cdecl nf_registerDriver(const char* driverName)
	{
		UNREFERENCED_PARAMETER(driverName);
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_unRegisterDriver(const char* driverName)
	{
		UNREFERENCED_PARAMETER(driverName);
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_tcpPostSend(ENDPOINT_ID id, const char* buf, int len)
	{
		UNREFERENCED_PARAMETER(id);
		UNREFERENCED_PARAMETER(buf);
		UNREFERENCED_PARAMETER(len);
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_tcpPostReceive(ENDPOINT_ID id, const char* buf, int len)
	{
		UNREFERENCED_PARAMETER(id);
		UNREFERENCED_PARAMETER(buf);
		UNREFERENCED_PARAMETER(len);
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_tcpDisableFiltering(ENDPOINT_ID id)
	{
		lock_guard<mutex> lg(g_mockLock);
		g_disabledTcpEndpoints.push_back(id);
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_udpPostSend(ENDPOINT_ID id, const unsigned char* remoteAddress, const char* buf, int len, PNF_UDP_OPTIONS options)
	{
		UNREFERENCED_PARAMETER(id);
		UNREFERENCED_PARAMETER(remoteAddress);
		UNREFERENCED_PARAMETER(buf);
		UNREFERENCED_PARAMETER(len);
		UNREFERENCED_PARAMETER(options);
		lock_guard<mutex> lg(g_mockLock);
		++g_directUdpSends;
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_udpPostReceive(ENDPOINT_ID id, const unsigned char* remoteAddress, const char* buf, int len, PNF_UDP_OPTIONS options)
	{
		CapturedUdpReceive cap;
		cap.id = id;
		if (remoteAddress != nullptr)
			memcpy(&cap.remote, remoteAddress, sizeof(SOCKADDR_IN6));
		if (buf != nullptr && len > 0)
			cap.payload.assign(buf, buf + len);
		if (options != nullptr)
		{
			const size_t optLen = options->optionsLength > 0 ? static_cast<size_t>(options->optionsLength) : 0;
			const size_t total = offsetof(NF_UDP_OPTIONS, options) + optLen;
			cap.options.resize(total);
			memcpy(cap.options.data(), options, total);
		}
		{
			lock_guard<mutex> lg(g_mockLock);
			g_udpReceives.push_back(std::move(cap));
		}
		g_mockCv.notify_all();
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_udpDisableFiltering(ENDPOINT_ID id)
	{
		{
			lock_guard<mutex> lg(g_mockLock);
			g_disabledUdpEndpoints.push_back(id);
		}
		g_mockCv.notify_all();
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_ipPostSend(const char* buf, int len, PNF_IP_PACKET_OPTIONS options)
	{
		UNREFERENCED_PARAMETER(buf);
		UNREFERENCED_PARAMETER(len);
		UNREFERENCED_PARAMETER(options);
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_ipPostReceive(const char* buf, int len, PNF_IP_PACKET_OPTIONS options)
	{
		CapturedIpReceive cap;
		if (buf != nullptr && len > 0)
			cap.packet.assign(buf, buf + len);
		if (options != nullptr)
			cap.options = *options;
		cap.receivedAt = chrono::steady_clock::now();
		{
			lock_guard<mutex> lg(g_mockLock);
			g_ipReceives.push_back(std::move(cap));
		}
		g_mockCv.notify_all();
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_addRule(PNF_RULE pRule, int toHead)
	{
		UNREFERENCED_PARAMETER(pRule);
		UNREFERENCED_PARAMETER(toHead);
		lock_guard<mutex> lg(g_mockLock);
		++g_mockRulesAdded;
		return NF_STATUS_SUCCESS;
	}

	NF_STATUS __cdecl nf_deleteRules()
	{
		lock_guard<mutex> lg(g_mockLock);
		++g_mockRulesDeleted;
		return NF_STATUS_SUCCESS;
	}

	BOOL __cdecl nf_getProcessNameW(DWORD processId, wchar_t* buf, DWORD len)
	{
		UNREFERENCED_PARAMETER(processId);
		if (buf == nullptr || len == 0)
			return FALSE;
		wstring procName;
		{
			lock_guard<mutex> lg(g_mockLock);
			procName = g_mockProcessName;
		}
		wcsncpy_s(buf, len, procName.c_str(), _TRUNCATE);
		return TRUE;
	}

	BOOL __cdecl nf_getProcessNameFromKernel(DWORD processId, wchar_t* buf, DWORD len)
	{
		return nf_getProcessNameW(processId, buf, len);
	}

	void __cdecl nf_adjustProcessPriviledges()
	{
	}

	void __cdecl nf_setIPEventHandler(NF_IPEventHandler* pHandler)
	{
		UNREFERENCED_PARAMETER(pHandler);
	}
}

namespace
{
#define CHECK(cond) do { if (!(cond)) { fprintf(stderr, "CHECK FAILED at line %d: %s\n", __LINE__, #cond); abort(); } } while (0)

	bool SendAllSock(SOCKET s, const char* buf, int len)
	{
		int sent = 0;
		while (sent < len)
		{
			const int rc = send(s, buf + sent, len - sent, 0);
			if (rc <= 0)
				return false;
			sent += rc;
		}
		return true;
	}

	bool RecvExactSock(SOCKET s, char* buf, int len)
	{
		int recvd = 0;
		while (recvd < len)
		{
			const int rc = recv(s, buf + recvd, len - recvd, 0);
			if (rc <= 0)
				return false;
			recvd += rc;
		}
		return true;
	}

	USHORT GetBoundPort(SOCKET s)
	{
		SOCKADDR_IN addr{};
		int len = sizeof(addr);
		CHECK(getsockname(s, reinterpret_cast<PSOCKADDR>(&addr), &len) == 0);
		return ntohs(addr.sin_port);
	}

	SOCKET CreateLoopbackTcpListener(USHORT& outPort)
	{
		SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		CHECK(s != INVALID_SOCKET);
		SOCKADDR_IN addr{};
		addr.sin_family = AF_INET;
		addr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(s, reinterpret_cast<PSOCKADDR>(&addr), sizeof(addr)) == 0);
		CHECK(listen(s, 16) == 0);
		outPort = GetBoundPort(s);
		return s;
	}

	class MockSocks5Server
	{
	public:
		explicit MockSocks5Server(string user, string pass)
			: user_(std::move(user)), pass_(std::move(pass))
		{
			listener_ = CreateLoopbackTcpListener(port_);
			thread_ = thread([this]() { Run(); });
		}

		~MockSocks5Server()
		{
			Stop();
		}

		USHORT Port() const { return port_; }

		void Stop()
		{
			if (stopping_.exchange(true))
				return;
			const SOCKET listener = listener_.exchange(INVALID_SOCKET);
			if (listener != INVALID_SOCKET)
				closesocket(listener);
			{
				lock_guard<mutex> lg(sockLock_);
				for (SOCKET s : sockets_)
				{
					shutdown(s, SD_BOTH);
					closesocket(s);
				}
				sockets_.clear();
			}
			if (thread_.joinable())
				thread_.join();
			for (auto& t : clientThreads_)
			{
				if (t.joinable())
					t.join();
			}
			clientThreads_.clear();
		}

	private:
		void TrackSocket(SOCKET s)
		{
			lock_guard<mutex> lg(sockLock_);
			sockets_.push_back(s);
		}

		void CloseTrackedSocket(SOCKET s)
		{
			if (s == INVALID_SOCKET)
				return;
			{
				lock_guard<mutex> lg(sockLock_);
				auto it = find(sockets_.begin(), sockets_.end(), s);
				if (it == sockets_.end())
					return;
				sockets_.erase(it);
			}
			shutdown(s, SD_BOTH);
			closesocket(s);
		}

		void Run()
		{
			while (!stopping_.load())
			{
				const SOCKET listener = listener_.load();
				if (listener == INVALID_SOCKET)
					break;
				SOCKET c = accept(listener, NULL, NULL);
				if (c == INVALID_SOCKET)
					break;
				TrackSocket(c);
				clientThreads_.emplace_back([this, c]() { HandleClient(c); });
			}
		}

		void HandleClient(SOCKET client)
		{
			char hdr[2]{};
			if (!RecvExactSock(client, hdr, 2) || hdr[0] != 0x05)
				return;
			const int nMethods = static_cast<unsigned char>(hdr[1]);
			vector<char> methods(nMethods);
			if (nMethods > 0 && !RecvExactSock(client, methods.data(), nMethods))
				return;

			if (!user_.empty())
			{
				const char methodChoice[2] = { 0x05, 0x02 };
				if (!SendAllSock(client, methodChoice, 2))
					return;
				char authHdr[2]{};
				if (!RecvExactSock(client, authHdr, 2) || authHdr[0] != 0x01)
					return;
				const int ulen = static_cast<unsigned char>(authHdr[1]);
				string u(ulen, '\0');
				if (ulen > 0 && !RecvExactSock(client, u.data(), ulen))
					return;
				char plenByte = 0;
				if (!RecvExactSock(client, &plenByte, 1))
					return;
				const int plen = static_cast<unsigned char>(plenByte);
				string p(plen, '\0');
				if (plen > 0 && !RecvExactSock(client, p.data(), plen))
					return;
				const char authReply[2] = { 0x01, static_cast<char>((u == user_ && p == pass_) ? 0x00 : 0x01) };
				if (!SendAllSock(client, authReply, 2) || authReply[1] != 0x00)
					return;
			}
			else
			{
				const char methodChoice[2] = { 0x05, 0x00 };
				if (!SendAllSock(client, methodChoice, 2))
					return;
			}

			char req[4]{};
			if (!RecvExactSock(client, req, 4) || req[0] != 0x05)
				return;
			SOCKADDR_IN target{};
			target.sin_family = AF_INET;
			if (req[3] == 0x01)
			{
				if (!RecvExactSock(client, reinterpret_cast<char*>(&target.sin_addr), 4))
					return;
				if (!RecvExactSock(client, reinterpret_cast<char*>(&target.sin_port), 2))
					return;
			}
			else
			{
				return;
			}

			if (req[1] == 0x01) // CONNECT
			{
				SOCKET up = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
				if (up == INVALID_SOCKET || connect(up, reinterpret_cast<PSOCKADDR>(&target), sizeof(target)) != 0)
				{
					if (up != INVALID_SOCKET) closesocket(up);
					return;
				}
				TrackSocket(up);

				const char reply[10] = { 0x05, 0x00, 0x00, 0x01, 127, 0, 0, 1, 0, 0 };
				if (!SendAllSock(client, reply, 10))
					return;

				thread tUp([client, up]()
				{
					char buf[8192];
					for (;;)
					{
						const int n = recv(client, buf, sizeof(buf), 0);
						if (n <= 0)
						{
							if (n == 0)
								shutdown(up, SD_SEND);
							break;
						}
						if (!SendAllSock(up, buf, n))
							break;
					}
				});

				char buf[8192];
				for (;;)
				{
					const int n = recv(up, buf, sizeof(buf), 0);
					if (n <= 0)
					{
						if (n == 0)
							shutdown(client, SD_SEND);
						break;
					}
					if (!SendAllSock(client, buf, n))
						break;
				}
				tUp.join();
				return;
			}

			if (req[1] == 0x03) // UDP ASSOCIATE
			{
				SOCKET relay = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
				CHECK(relay != INVALID_SOCKET);
				SOCKADDR_IN bindAddr{};
				bindAddr.sin_family = AF_INET;
				bindAddr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
				CHECK(::bind(relay, reinterpret_cast<PSOCKADDR>(&bindAddr), sizeof(bindAddr)) == 0);
				TrackSocket(relay);

				const USHORT relayPort = htons(GetBoundPort(relay));
				char reply[10] = { 0x05, 0x00, 0x00, 0x01, 127, 0, 0, 1, 0, 0 };
				memcpy(reply + 8, &relayPort, 2);
				if (!SendAllSock(client, reply, 10))
					return;

				thread udpLoop([this, relay]()
				{
					SOCKET outUdp = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
					if (outUdp == INVALID_SOCKET)
						return;
					SOCKADDR_IN any{};
					any.sin_family = AF_INET;
					any.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
					::bind(outUdp, reinterpret_cast<PSOCKADDR>(&any), sizeof(any));
					TrackSocket(outUdp);

					char buf[65535];
					while (!stopping_.load())
					{
						SOCKADDR_IN clientFrom{};
						int clientFromLen = sizeof(clientFrom);
						const int n = recvfrom(relay, buf, sizeof(buf), 0, reinterpret_cast<PSOCKADDR>(&clientFrom), &clientFromLen);
						if (n <= 10)
						{
							if (n <= 0) break;
							continue;
						}
						if (buf[3] != 0x01)
							continue;

						SOCKADDR_IN dest{};
						dest.sin_family = AF_INET;
						memcpy(&dest.sin_addr, buf + 4, 4);
						memcpy(&dest.sin_port, buf + 8, 2);

						sendto(outUdp, buf + 10, n - 10, 0, reinterpret_cast<PSOCKADDR>(&dest), sizeof(dest));

						char respBuf[65535];
						SOCKADDR_IN remoteFrom{};
						int remoteFromLen = sizeof(remoteFrom);
						const int rn = recvfrom(outUdp, respBuf + 10, static_cast<int>(sizeof(respBuf) - 10), 0,
							reinterpret_cast<PSOCKADDR>(&remoteFrom), &remoteFromLen);
						if (rn > 0)
						{
							memset(respBuf, 0, 10);
							respBuf[3] = 0x01;
							memcpy(respBuf + 4, &remoteFrom.sin_addr, 4);
							memcpy(respBuf + 8, &remoteFrom.sin_port, 2);
							sendto(relay, respBuf, rn + 10, 0, reinterpret_cast<PSOCKADDR>(&clientFrom), clientFromLen);
						}
					}
				});

				char ctrlBuf[16];
				while (recv(client, ctrlBuf, sizeof(ctrlBuf), 0) > 0) {}
				CloseTrackedSocket(relay);
				udpLoop.join();
			}
		}

		string user_;
		string pass_;
		atomic<SOCKET> listener_{ INVALID_SOCKET };
		USHORT port_ = 0;
		atomic<bool> stopping_{ false };
		thread thread_;
		vector<thread> clientThreads_;
		mutex sockLock_;
		vector<SOCKET> sockets_;
	};

	void TestDialValidationAndSharedRules()
	{
		ResetMocks();

		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"0")) == TRUE);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"25")) == TRUE);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"60000")) == TRUE);

		CHECK(aio_dial(AIO_ICMPING, nullptr) == FALSE);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"")) == FALSE);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"-1")) == FALSE);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"60001")) == FALSE);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"10ms")) == FALSE);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"abc")) == FALSE);

		CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"[invalid")) == FALSE);
		CHECK(aio_dial(AIO_BYPNAME, const_cast<LPWSTR>(L"[invalid")) == FALSE);
		CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"game\\.exe$")) == TRUE);

		atomic<bool> stopReaders{ false };
		vector<thread> readers;
		for (int t = 0; t < 4; ++t)
		{
			readers.emplace_back([&]()
			{
				while (!stopReaders.load())
				{
					shared_lock<shared_mutex> sl(ruleLock);
					volatile size_t total = bypassList.size() + handleList.size();
					(void)total;
				}
			});
		}

		for (int i = 0; i < 100; ++i)
		{
			CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"app\\.exe$")) == TRUE);
			CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		}
		stopReaders = true;
		for (auto& r : readers)
			r.join();

		puts("[PASS] TestDialValidationAndSharedRules");
	}

	void TestAioInitRollbackAndLifecycle()
	{
		ResetMocks();
		g_mockNfInitStatus = NF_STATUS_FAIL;

		CHECK(aio_dial(AIO_FILTERLOOPBACK, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERINTRANET, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERICMP, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_DNSHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_DNSPORT, const_cast<LPWSTR>(L"5353")) == TRUE);

		CHECK(aio_init() == FALSE);
		CHECK(!g_mockNfInitialized);

		g_mockNfInitStatus = NF_STATUS_SUCCESS;
		CHECK(aio_init() == TRUE);
		CHECK(g_mockNfInitialized);
		CHECK(g_mockRulesAdded > 0);
		CHECK(aio_init() == FALSE); // double-init rejected cleanly

		aio_free();
		CHECK(!g_mockNfInitialized);
		CHECK(g_mockRulesDeleted == 1);
		CHECK(aio_getUP() == 0);
		CHECK(aio_getDL() == 0);

		puts("[PASS] TestAioInitRollbackAndLifecycle");
	}

	void TestTcpRelayAuthPayloadHalfCloseAndCleanFree()
	{
		ResetMocks();

		// Echo server reading until client half-close FIN, then echoing 256 KiB back + FIN.
		USHORT echoPort = 0;
		SOCKET echoListener = CreateLoopbackTcpListener(echoPort);
		thread echoThread([echoListener]()
		{
			SOCKET c = accept(echoListener, NULL, NULL);
			if (c == INVALID_SOCKET)
				return;
			vector<char> payload;
			char buf[8192];
			for (;;)
			{
				const int n = recv(c, buf, sizeof(buf), 0);
				if (n <= 0)
					break;
				payload.insert(payload.end(), buf, buf + n);
			}
			SendAllSock(c, payload.data(), static_cast<int>(payload.size()));
			shutdown(c, SD_SEND);
			closesocket(c);
		});

		MockSocks5Server socks("user1", "pass1");
		const wstring portStr = to_wstring(socks.Port());
		CHECK(aio_dial(AIO_TGTHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(portStr.c_str())) == TRUE);
		CHECK(aio_dial(AIO_TGTUSER, const_cast<LPWSTR>(L"user1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPASS, const_cast<LPWSTR>(L"pass1")) == TRUE);

		CHECK(IocpPool::Init());
		CHECK(TCPHandler::INIT());

		SOCKET localClient = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		CHECK(localClient != INVALID_SOCKET);
		SOCKADDR_IN localBind{};
		localBind.sin_family = AF_INET;
		localBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(localClient, reinterpret_cast<PSOCKADDR>(&localBind), sizeof(localBind)) == 0);

		SOCKADDR_IN6 clientCtx{};
		int clientCtxLen = sizeof(SOCKADDR_IN);
		CHECK(getsockname(localClient, reinterpret_cast<PSOCKADDR>(&clientCtx), &clientCtxLen) == 0);

		SOCKADDR_IN6 targetCtx{};
		auto* target4 = reinterpret_cast<SOCKADDR_IN*>(&targetCtx);
		target4->sin_family = AF_INET;
		target4->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		target4->sin_port = htons(echoPort);
		TCPHandler::CreateHandler(1, clientCtx, targetCtx);

		SOCKADDR_IN proxyListenAddr{};
		proxyListenAddr.sin_family = AF_INET;
		proxyListenAddr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		proxyListenAddr.sin_port = tcpListen.load();
		CHECK(proxyListenAddr.sin_port != 0);
		CHECK(connect(localClient, reinterpret_cast<PSOCKADDR>(&proxyListenAddr), sizeof(proxyListenAddr)) == 0);

		const int totalBytes = 256 * 1024;
		vector<char> sent(totalBytes);
		for (int i = 0; i < totalBytes; ++i)
			sent[i] = static_cast<char>((i * 31 + 7) & 0xff);

		CHECK(SendAllSock(localClient, sent.data(), totalBytes));
		CHECK(shutdown(localClient, SD_SEND) == 0); // Half-close: echo response must still arrive!

		vector<char> recvd;
		char buf[8192];
		for (;;)
		{
			const int n = recv(localClient, buf, sizeof(buf), 0);
			if (n <= 0)
				break;
			recvd.insert(recvd.end(), buf, buf + n);
		}
		closesocket(localClient);

		CHECK(recvd == sent);
		CHECK(aio_getUP() == static_cast<ULONG64>(totalBytes));
		CHECK(aio_getDL() == static_cast<ULONG64>(totalBytes));

		TCPHandler::FREE();
		IocpPool::Shutdown();

		closesocket(echoListener);
		echoThread.join();
		socks.Stop();
		aio_dial(AIO_TGTUSER, const_cast<LPWSTR>(L""));
		aio_dial(AIO_TGTPASS, const_cast<LPWSTR>(L""));

		puts("[PASS] TestTcpRelayAuthPayloadHalfCloseAndCleanFree");
	}

	void TestUdpRelayNonBlockingSendOptionsAndContextLimit()
	{
		ResetMocks();

		SOCKET echoUdp = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		CHECK(echoUdp != INVALID_SOCKET);
		SOCKADDR_IN echoBind{};
		echoBind.sin_family = AF_INET;
		echoBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(echoUdp, reinterpret_cast<PSOCKADDR>(&echoBind), sizeof(echoBind)) == 0);
		const USHORT echoPort = GetBoundPort(echoUdp);
		atomic<bool> stopEcho{ false };
		thread echoThread([&]()
		{
			char buf[2048];
			SOCKADDR_IN from{};
			int fromLen = sizeof(from);
			while (!stopEcho.load())
			{
				const int n = recvfrom(echoUdp, buf, sizeof(buf), 0, reinterpret_cast<PSOCKADDR>(&from), &fromLen);
				if (n <= 0)
					break;
				sendto(echoUdp, buf, n, 0, reinterpret_cast<PSOCKADDR>(&from), fromLen);
			}
		});

		MockSocks5Server socks("", "");
		const wstring portStr = to_wstring(socks.Port());

		CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"game\\.exe$")) == TRUE);
		CHECK(aio_dial(AIO_FILTERTCP, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERUDP, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_FILTERDNS, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERICMP, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_DNSHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_DNSPORT, const_cast<LPWSTR>(L"5353")) == TRUE);
		CHECK(aio_dial(AIO_TGTHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(portStr.c_str())) == TRUE);

		CHECK(aio_init() == TRUE);

		NF_UDP_CONN_INFO info{};
		info.processId = GetCurrentProcessId() + 99999; // non-self PID mapped to mock L"game.exe"
		info.ip_family = AF_INET;
		udpCreated(101, &info);

		// Build NF_UDP_OPTIONS with exact 12-byte options tail.
		vector<char> optBuf(offsetof(NF_UDP_OPTIONS, options) + 12);
		auto* opts = reinterpret_cast<PNF_UDP_OPTIONS>(optBuf.data());
		opts->flags = 0x20;
		opts->optionsLength = 12;
		for (int i = 0; i < 12; ++i)
			opts->options[i] = static_cast<unsigned char>(0xA1 + i);

		SOCKADDR_IN6 target{};
		auto* t4 = reinterpret_cast<SOCKADDR_IN*>(&target);
		t4->sin_family = AF_INET;
		t4->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		t4->sin_port = htons(echoPort);

		const char msg[] = "hello-udp-iocp";
		const auto t0 = chrono::steady_clock::now();
		udpSend(101, reinterpret_cast<const unsigned char*>(&target), msg, static_cast<int>(sizeof(msg) - 1), opts);
		const auto elapsedMs = chrono::duration_cast<chrono::milliseconds>(chrono::steady_clock::now() - t0).count();
		CHECK(elapsedMs < 100); // must never block callback thread on SOCKS handshake

		{
			unique_lock<mutex> ul(g_mockLock);
			const bool got = g_mockCv.wait_for(ul, chrono::seconds(5), []() { return !g_udpReceives.empty(); });
			CHECK(got);
			CHECK(g_udpReceives[0].id == 101);
			CHECK(string(g_udpReceives[0].payload.begin(), g_udpReceives[0].payload.end()) == "hello-udp-iocp");
			CHECK(g_udpReceives[0].options == optBuf);
		}

		// Exceed MaxUdpContexts (1024) and confirm nf_udpDisableFiltering fires.
		for (ENDPOINT_ID eid = 1000; eid < 1000 + 1030; ++eid)
			udpCreated(eid, &info);
		{
			lock_guard<mutex> lg(g_mockLock);
			CHECK(!g_disabledUdpEndpoints.empty());
		}

		udpClosed(101, &info);
		aio_free();
		CHECK(aio_getUP() == 0);
		CHECK(aio_getDL() == 0);

		stopEcho = true;
		closesocket(echoUdp);
		echoThread.join();
		socks.Stop();
		CHECK(aio_dial(AIO_FILTERTCP, const_cast<LPWSTR>(L"true")) == TRUE);

		puts("[PASS] TestUdpRelayNonBlockingSendOptionsAndContextLimit");
	}

	void TestIcmpSchedulerOrderingAndCancellation()
	{
		ResetMocks();
		CHECK(IPEventHandler::INIT());

		auto makeEchoPacket = [](BYTE seqByte)
		{
			vector<char> pkt(28, 0);
			pkt[0] = 0x45;
			pkt[3] = 28;
			pkt[8] = 64;
			pkt[9] = IPPROTO_ICMP;
			pkt[12] = 10; pkt[13] = 0; pkt[14] = 0; pkt[15] = 2;
			pkt[16] = 1; pkt[17] = 1; pkt[18] = 1; pkt[19] = 1;
			pkt[20] = 0x08; // Echo request
			pkt[27] = seqByte;
			return pkt;
		};

		NF_IP_PACKET_OPTIONS opts{};
		opts.ip_family = AF_INET;
		opts.ipHeaderSize = 20;

		const auto pktSlow = makeEchoPacket(1);
		const auto pktFast = makeEchoPacket(2);

		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"120")) == TRUE);
		ipSend(pktSlow.data(), static_cast<int>(pktSlow.size()), &opts);

		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"15")) == TRUE);
		ipSend(pktFast.data(), static_cast<int>(pktFast.size()), &opts);

		{
			unique_lock<mutex> ul(g_mockLock);
			const bool gotBoth = g_mockCv.wait_for(ul, chrono::seconds(3), []() { return g_ipReceives.size() >= 2; });
			CHECK(gotBoth);
			CHECK(g_ipReceives[0].packet[20] == 0x00); // Echo reply
			CHECK(static_cast<BYTE>(g_ipReceives[0].packet[27]) == 2); // 15ms packet delivered first
			CHECK(static_cast<BYTE>(g_ipReceives[1].packet[27]) == 1); // 120ms packet delivered second
			// Swapped IPs: reply dst == original src (10.0.0.2)
			CHECK(g_ipReceives[0].packet[16] == 10 && g_ipReceives[0].packet[19] == 2);
		}

		// Queue a 5000ms delayed packet and verify FREE() cancels immediately.
		const auto pktLong = makeEchoPacket(9);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"5000")) == TRUE);
		ipSend(pktLong.data(), static_cast<int>(pktLong.size()), &opts);

		const auto freeStart = chrono::steady_clock::now();
		IPEventHandler::FREE();
		const auto freeMs = chrono::duration_cast<chrono::milliseconds>(chrono::steady_clock::now() - freeStart).count();
		CHECK(freeMs < 500);
		CHECK(aio_dial(AIO_ICMPING, const_cast<LPWSTR>(L"0")) == TRUE);

		puts("[PASS] TestIcmpSchedulerOrderingAndCancellation");
	}

	vector<char> BuildDnsPacket(USHORT txId)
	{
		vector<char> pkt(16, 0);
		memcpy(pkt.data(), &txId, 2);
		pkt[2] = 0x01;
		pkt[5] = 0x01; // QDCOUNT = 1
		return pkt;
	}

	void TestDnsDirectAndProxiedMultiplexing()
	{
		ResetMocks();

		SOCKET mockDns = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		CHECK(mockDns != INVALID_SOCKET);
		SOCKADDR_IN dnsBind{};
		dnsBind.sin_family = AF_INET;
		dnsBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(mockDns, reinterpret_cast<PSOCKADDR>(&dnsBind), sizeof(dnsBind)) == 0);
		const USHORT mockDnsPort = GetBoundPort(mockDns);

		SOCKET spoofSock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		CHECK(spoofSock != INVALID_SOCKET);
		SOCKADDR_IN spoofBind{};
		spoofBind.sin_family = AF_INET;
		spoofBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(spoofSock, reinterpret_cast<PSOCKADDR>(&spoofBind), sizeof(spoofBind)) == 0);

		atomic<bool> stopDns{ false };
		USHORT observedWireId = 0;
		SOCKADDR_IN lastQueryClient{};
		atomic<bool> holdReply{ true };
		thread dnsThread([&]()
		{
			char buf[512];
			int fromLen = sizeof(lastQueryClient);
			while (!stopDns.load())
			{
				const int n = recvfrom(mockDns, buf, sizeof(buf), 0, reinterpret_cast<PSOCKADDR>(&lastQueryClient), &fromLen);
				if (n < 12)
				{
					if (n <= 0) break;
					continue;
				}
				memcpy(&observedWireId, buf, 2);

				// First while holdReply is true: send a wrong wireId from real peer AND a spoofed response from rogue socket.
				if (holdReply.load())
				{
					char wrongId[16];
					memcpy(wrongId, buf, 16);
					const USHORT badTx = static_cast<USHORT>(observedWireId ^ 0x5555);
					memcpy(wrongId, &badTx, 2);
					wrongId[2] = static_cast<char>(0x81);
					sendto(mockDns, wrongId, 16, 0, reinterpret_cast<PSOCKADDR>(&lastQueryClient), fromLen);

					char spoofPkt[16];
					memcpy(spoofPkt, buf, 16);
					spoofPkt[2] = static_cast<char>(0x81);
					sendto(spoofSock, spoofPkt, 16, 0, reinterpret_cast<PSOCKADDR>(&lastQueryClient), fromLen);
					this_thread::sleep_for(chrono::milliseconds(40));
				}

				buf[2] = static_cast<char>(0x81);
				sendto(mockDns, buf, n, 0, reinterpret_cast<PSOCKADDR>(&lastQueryClient), fromLen);
			}
		});

		const wstring dnsPortStr = to_wstring(mockDnsPort);
		CHECK(aio_dial(AIO_DNSHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_DNSPORT, const_cast<LPWSTR>(dnsPortStr.c_str())) == TRUE);

		vector<char> optBuf(offsetof(NF_UDP_OPTIONS, options) + 6, 0);
		auto* opts = reinterpret_cast<PNF_UDP_OPTIONS>(optBuf.data());
		opts->flags = 0x20;
		opts->optionsLength = 6;
		opts->options[0] = 0x42;

		SOCKADDR_IN6 target{};
		auto* t4 = reinterpret_cast<SOCKADDR_IN*>(&target);
		t4->sin_family = AF_INET;
		t4->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		t4->sin_port = htons(53);

		// 1. Direct persistent DNS socket test
		CHECK(aio_dial(AIO_DNSPROX, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(DNSHandler::INIT());
		const USHORT origIdDirect = 0x1234;
		const auto pktDirect = BuildDnsPacket(origIdDirect);
		DNSHandler::CreateHandler(77, &target, pktDirect.data(), static_cast<int>(pktDirect.size()), opts);

		{
			unique_lock<mutex> ul(g_mockLock);
			const bool got = g_mockCv.wait_for(ul, chrono::seconds(5), []() { return !g_udpReceives.empty(); });
			CHECK(got);
			CHECK(g_udpReceives.size() == 1); // spoofed peer and wrong wireId must both be dropped
			CHECK(g_udpReceives[0].id == 77);
			USHORT gotTxId = 0;
			memcpy(&gotTxId, g_udpReceives[0].payload.data(), 2);
			CHECK(gotTxId == origIdDirect);
			CHECK(g_udpReceives[0].options == optBuf);
		}
		DNSHandler::FREE();

		// 2. Proxied SOCKS5 DNS multiplexing test
		ResetMocks();
		holdReply = false;
		MockSocks5Server socks("", "");
		const wstring socksPortStr = to_wstring(socks.Port());
		CHECK(aio_dial(AIO_TGTHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(socksPortStr.c_str())) == TRUE);
		CHECK(aio_dial(AIO_DNSPROX, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(DNSHandler::INIT());

		const USHORT origIdProx = 0xABCD;
		const auto pktProx = BuildDnsPacket(origIdProx);
		DNSHandler::CreateHandler(88, &target, pktProx.data(), static_cast<int>(pktProx.size()), opts);

		{
			unique_lock<mutex> ul(g_mockLock);
			const bool got = g_mockCv.wait_for(ul, chrono::seconds(5), []() { return !g_udpReceives.empty(); });
			CHECK(got);
			CHECK(g_udpReceives[0].id == 88);
			USHORT gotTxId = 0;
			memcpy(&gotTxId, g_udpReceives[0].payload.data(), 2);
			CHECK(gotTxId == origIdProx);
			CHECK(g_udpReceives[0].options == optBuf);
		}
		DNSHandler::FREE();

		stopDns = true;
		closesocket(mockDns);
		closesocket(spoofSock);
		dnsThread.join();
		socks.Stop();

		puts("[PASS] TestDnsDirectAndProxiedMultiplexing");
	}

	void TestLifecycleDnsRefCountBypassLoopAndSpoofProtection()
	{
		ResetMocks();

		// 1. Validate strict port bounds rejection in aio_dial
		CHECK(aio_dial(AIO_DNSPORT, const_cast<LPWSTR>(L"0")) == FALSE);
		CHECK(aio_dial(AIO_DNSPORT, const_cast<LPWSTR>(L"65536")) == FALSE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(L"0")) == FALSE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(L"abc")) == FALSE);
		CHECK(aio_dial(AIO_DNSPORT, const_cast<LPWSTR>(L"5353")) == TRUE);

		// 2. Repeated DNSHandler::INIT while TCPHandler is active must NOT tear down IocpPool
		USHORT echoTcpPort = 0;
		SOCKET tcpEchoListener = CreateLoopbackTcpListener(echoTcpPort);
		thread tcpEchoThread([tcpEchoListener]()
		{
			SOCKET c = accept(tcpEchoListener, NULL, NULL);
			if (c == INVALID_SOCKET)
				return;
			char buf[64];
			const int n = recv(c, buf, sizeof(buf), 0);
			if (n > 0)
				SendAllSock(c, buf, n);
			shutdown(c, SD_SEND);
			closesocket(c);
		});

		MockSocks5Server socks("", "");
		const wstring portStr = to_wstring(socks.Port());
		CHECK(aio_dial(AIO_TGTHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(portStr.c_str())) == TRUE);
		CHECK(aio_dial(AIO_DNSHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);

		CHECK(IocpPool::Init());
		CHECK(TCPHandler::INIT());

		SOCKET localClient = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		CHECK(localClient != INVALID_SOCKET);
		SOCKADDR_IN localBind{};
		localBind.sin_family = AF_INET;
		localBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(localClient, reinterpret_cast<PSOCKADDR>(&localBind), sizeof(localBind)) == 0);

		SOCKADDR_IN6 clientCtx{};
		int clientCtxLen = sizeof(SOCKADDR_IN);
		CHECK(getsockname(localClient, reinterpret_cast<PSOCKADDR>(&clientCtx), &clientCtxLen) == 0);
		SOCKADDR_IN6 targetCtx{};
		auto* target4 = reinterpret_cast<SOCKADDR_IN*>(&targetCtx);
		target4->sin_family = AF_INET;
		target4->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		target4->sin_port = htons(echoTcpPort);
		TCPHandler::CreateHandler(42, clientCtx, targetCtx);

		SOCKADDR_IN proxyListenAddr{};
		proxyListenAddr.sin_family = AF_INET;
		proxyListenAddr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		proxyListenAddr.sin_port = tcpListen.load();
		CHECK(proxyListenAddr.sin_port != 0);
		CHECK(connect(localClient, reinterpret_cast<PSOCKADDR>(&proxyListenAddr), sizeof(proxyListenAddr)) == 0);

		// Calling DNSHandler::INIT twice must leave TCP IOCP pumping intact.
		CHECK(DNSHandler::INIT());
		CHECK(DNSHandler::INIT());
		DNSHandler::FREE();

		const char ping[] = "ping-after-dns-reinit";
		CHECK(SendAllSock(localClient, ping, static_cast<int>(sizeof(ping) - 1)));
		char pong[64]{};
		CHECK(RecvExactSock(localClient, pong, static_cast<int>(sizeof(ping) - 1)));
		CHECK(string(pong, sizeof(ping) - 1) == ping);

		closesocket(localClient);
		TCPHandler::FREE();
		IocpPool::Shutdown();
		closesocket(tcpEchoListener);
		tcpEchoThread.join();
		socks.Stop();

		// 3. Bypassed process (e.g. mihomo.exe) must disable UDP filtering even when !dnsOnly or (!filterUDP && filterDNS)
		ResetMocks();
		{
			lock_guard<mutex> lg(g_mockLock);
			g_mockProcessName = L"C:\\Program Files\\Netch\\bin\\mihomo.exe";
		}
		CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_BYPNAME, const_cast<LPWSTR>(L"mihomo\\.exe$")) == TRUE);
		CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"game\\.exe$")) == TRUE);
		CHECK(aio_dial(AIO_DNSONLY, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERTCP, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERUDP, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_FILTERDNS, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_FILTERICMP, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_init() == TRUE);

		NF_UDP_CONN_INFO udpInfo{};
		udpInfo.processId = GetCurrentProcessId() + 88888;
		udpInfo.ip_family = AF_INET;
		udpCreated(501, &udpInfo);
		{
			lock_guard<mutex> lg(g_mockLock);
			CHECK(find(g_disabledUdpEndpoints.begin(), g_disabledUdpEndpoints.end(), 501) != g_disabledUdpEndpoints.end());
		}
		aio_free();

		CHECK(aio_dial(AIO_FILTERUDP, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_init() == TRUE);
		udpCreated(502, &udpInfo);
		{
			lock_guard<mutex> lg(g_mockLock);
			CHECK(find(g_disabledUdpEndpoints.begin(), g_disabledUdpEndpoints.end(), 502) != g_disabledUdpEndpoints.end());
		}
		aio_free();
		CHECK(aio_dial(AIO_FILTERTCP, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_FILTERUDP, const_cast<LPWSTR>(L"true")) == TRUE);

		// 4. Dual-stack [::]:port wildcard UDP ASSOCIATE over IPv4 control peer + spoofed sender filtering
		ResetMocks();
		CHECK(IocpPool::Init());

		USHORT v6SocksPort = 0;
		SOCKET v6TcpListener = CreateLoopbackTcpListener(v6SocksPort);
		SOCKET relayUdp = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		CHECK(relayUdp != INVALID_SOCKET);
		SOCKADDR_IN relayBind{};
		relayBind.sin_family = AF_INET;
		relayBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(relayUdp, reinterpret_cast<PSOCKADDR>(&relayBind), sizeof(relayBind)) == 0);
		const USHORT relayPortHost = GetBoundPort(relayUdp);
		const USHORT relayPortNet = htons(relayPortHost);

		atomic<bool> stopCustomSocks{ false };
		thread customSocksCtrl([&]()
		{
			SOCKET c = accept(v6TcpListener, NULL, NULL);
			if (c == INVALID_SOCKET)
				return;
			char hello[3]{};
			if (RecvExactSock(c, hello, 3))
			{
				const char choice[2] = { 0x05, 0x00 };
				SendAllSock(c, choice, 2);
				char req[10]{};
				if (RecvExactSock(c, req, 10) && req[1] == 0x03)
				{
					// Reply with ATYP=0x04 wildcard IPv6 [::]:relayPortNet
					char reply[22]{};
					reply[0] = 0x05;
					reply[3] = 0x04;
					memcpy(reply + 20, &relayPortNet, 2);
					SendAllSock(c, reply, sizeof(reply));
					while (!stopCustomSocks.load())
					{
						char tmp[8];
						if (recv(c, tmp, sizeof(tmp), 0) <= 0)
							break;
					}
				}
			}
			closesocket(c);
		});

		const wstring v6PortW = to_wstring(v6SocksPort);
		CHECK(aio_dial(AIO_TGTHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(v6PortW.c_str())) == TRUE);

		auto udp = make_shared<SocksHelper::UDP>();
		CHECK(udp->Associate());
		CHECK(udp->address.sin6_family == AF_INET6);
		CHECK(IN6_IS_ADDR_V4MAPPED(&udp->address.sin6_addr));
		CHECK(udp->CreateUDP());

		SOCKADDR_IN6 localUdpBound{};
		int localUdpLen = sizeof(localUdpBound);
		CHECK(getsockname(udp->udpSocket.load(), reinterpret_cast<PSOCKADDR>(&localUdpBound), &localUdpLen) == 0);
		const USHORT localUdpPort = localUdpBound.sin6_port;

		mutex cbLock;
		condition_variable cbCv;
		vector<string> receivedPayloads;
		CHECK(udp->StartAsync([&](const SOCKADDR_IN6&, const char* data, int len)
		{
			lock_guard<mutex> lg(cbLock);
			receivedPayloads.emplace_back(data, len);
			cbCv.notify_all();
		}));

		// Send spoofed SOCKS5 UDP packet from rogue loopback port -> must be dropped
		SOCKET rogueUdp = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		CHECK(rogueUdp != INVALID_SOCKET);
		char spoofFrame[16]{};
		spoofFrame[3] = 0x01;
		spoofFrame[4] = 1; spoofFrame[5] = 1; spoofFrame[6] = 1; spoofFrame[7] = 1;
		spoofFrame[8] = 0; spoofFrame[9] = 53;
		memcpy(spoofFrame + 10, "spoof!", 6);
		SOCKADDR_IN rogueDest{};
		rogueDest.sin_family = AF_INET;
		rogueDest.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		rogueDest.sin_port = localUdpPort;
		sendto(rogueUdp, spoofFrame, sizeof(spoofFrame), 0, reinterpret_cast<PSOCKADDR>(&rogueDest), sizeof(rogueDest));
		closesocket(rogueUdp);

		// Send real packet through ::ffff:127.0.0.1 relay on AF_INET6 socket (requires IPV6_V6ONLY=0)
		SOCKADDR_IN6 fakeDest{};
		auto* fakeDest4 = reinterpret_cast<SOCKADDR_IN*>(&fakeDest);
		fakeDest4->sin_family = AF_INET;
		fakeDest4->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		fakeDest4->sin_port = htons(9999);
		const char realMsg[] = "v4mapped-ok";
		CHECK(udp->Send(&fakeDest, realMsg, static_cast<int>(sizeof(realMsg) - 1)) == static_cast<int>(sizeof(realMsg) - 1));

		char relayBuf[128]{};
		SOCKADDR_IN clientFrom{};
		int clientFromLen = sizeof(clientFrom);
		const int rn = recvfrom(relayUdp, relayBuf, sizeof(relayBuf), 0, reinterpret_cast<PSOCKADDR>(&clientFrom), &clientFromLen);
		CHECK(rn == 10 + static_cast<int>(sizeof(realMsg) - 1));
		sendto(relayUdp, relayBuf, rn, 0, reinterpret_cast<PSOCKADDR>(&clientFrom), clientFromLen);

		{
			unique_lock<mutex> ul(cbLock);
			const bool got = cbCv.wait_for(ul, chrono::seconds(5), [&]() { return !receivedPayloads.empty(); });
			CHECK(got);
			CHECK(receivedPayloads.size() == 1);
			CHECK(receivedPayloads[0] == "v4mapped-ok");
		}

		udp->Stop();
		stopCustomSocks = true;
		closesocket(v6TcpListener);
		closesocket(relayUdp);
		customSocksCtrl.join();
		SocksHelper::UDP::WaitAllIoDrained();
		IocpPool::Shutdown();

		puts("[PASS] TestLifecycleDnsRefCountBypassLoopAndSpoofProtection");
	}

	string QueryConnectionsJson()
	{
		int cap = aio_getConnectionsJson(nullptr, 0) + 1;
		for (;;)
		{
			vector<char> buf(static_cast<size_t>(cap), '\0');
			const int needed = aio_getConnectionsJson(buf.data(), cap);
			CHECK(needed >= 0);
			if (needed < cap)
				return string(buf.data(), static_cast<size_t>(needed));
			cap = needed + 4096;
		}
	}

	string DrainEventsJson(ULONG64 afterSeq)
	{
		int cap = aio_drainConnectionEventsJson(afterSeq, nullptr, 0) + 1;
		for (;;)
		{
			vector<char> buf(static_cast<size_t>(cap), '\0');
			const int needed = aio_drainConnectionEventsJson(afterSeq, buf.data(), cap);
			CHECK(needed >= 0);
			if (needed < cap)
				return string(buf.data(), static_cast<size_t>(needed));
			cap = needed + 4096;
		}
	}

	ULONG64 ExtractNextSeq(const string& eventsJson)
	{
		const size_t pos = eventsJson.find("\"nextSeq\":");
		CHECK(pos != string::npos);
		return strtoull(eventsJson.c_str() + pos + 10, nullptr, 10);
	}

	ULONG64 ExtractFirstConnId(const string& connJson)
	{
		const size_t activePos = connJson.find("\"active\":[");
		CHECK(activePos != string::npos);
		const size_t idPos = connJson.find("\"id\":", activePos);
		CHECK(idPos != string::npos);
		return strtoull(connJson.c_str() + idPos + 5, nullptr, 10);
	}

	void TestIpFilterParsingTransactionsAndPrecedence()
	{
		ResetMocks();

		IpCidrRule r4{}, r6{}, rmapped{}, bad{};
		CHECK(ParseIpCidrRule(L"10.0.0.0/8", r4) && r4.canonicalText == "10.0.0.0/8");
		CHECK(ParseIpCidrRule(L" 192.168.1.5 ", r4) && r4.canonicalText == "192.168.1.5/32");
		CHECK(ParseIpCidrRule(L"2001:db8::/32", r6) && r6.canonicalText == "2001:db8::/32");
		CHECK(ParseIpCidrRule(L"[::ffff:8.8.8.8]", rmapped) && rmapped.family == AF_INET && rmapped.canonicalText == "8.8.8.8/32");
		CHECK(!ParseIpCidrRule(L"", bad));
		CHECK(!ParseIpCidrRule(L"999.0.0.1", bad));
		CHECK(!ParseIpCidrRule(L"1.2.3.4/33", bad));
		CHECK(!ParseIpCidrRule(L"1.2.3.4/", bad));
		CHECK(!ParseIpCidrRule(L"1.2.3.4/1/2", bad));

		SOCKADDR_IN v4{};
		v4.sin_family = AF_INET;
		InetPtonW(AF_INET, L"192.168.1.5", &v4.sin_addr);
		v4.sin_port = htons(443);

		SOCKADDR_IN6 v4Mapped{};
		v4Mapped.sin6_family = AF_INET6;
		v4Mapped.sin6_addr.u.Byte[10] = 0xFF;
		v4Mapped.sin6_addr.u.Byte[11] = 0xFF;
		memcpy(v4Mapped.sin6_addr.u.Byte + 12, &v4.sin_addr, 4);
		v4Mapped.sin6_port = htons(443);

		SOCKADDR_IN6 v6{};
		v6.sin6_family = AF_INET6;
		InetPtonW(AF_INET6, L"2001:db8::42", &v6.sin6_addr);
		v6.sin6_port = htons(8443);

		IpCidrRule allV6{};
		CHECK(ParseIpCidrRule(L"::/0", allV6));
		CHECK(MatchesIpCidrRule(reinterpret_cast<const sockaddr*>(&v4), r4));
		CHECK(MatchesIpCidrRule(reinterpret_cast<const sockaddr*>(&v4Mapped), r4));
		CHECK(!MatchesIpCidrRule(reinterpret_cast<const sockaddr*>(&v4Mapped), allV6));
		CHECK(MatchesIpCidrRule(reinterpret_cast<const sockaddr*>(&v6), r6));
		CHECK(MatchesIpCidrRule(reinterpret_cast<const sockaddr*>(&v6), allV6));

		IpCidrRule cidr198{}, cidr198Bypass{};
		CHECK(ParseIpCidrRule(L"198.51.100.0/24", cidr198));
		CHECK(ParseIpCidrRule(L"198.51.100.99", cidr198Bypass));
		vector<IpCidrRule> bypassVec{ cidr198Bypass };
		vector<IpCidrRule> handleVec{ cidr198 };

		SOCKADDR_IN targetAllowed{};
		targetAllowed.sin_family = AF_INET;
		InetPtonW(AF_INET, L"198.51.100.1", &targetAllowed.sin_addr);
		SOCKADDR_IN targetBypassed{};
		targetBypassed.sin_family = AF_INET;
		InetPtonW(AF_INET, L"198.51.100.99", &targetBypassed.sin_addr);
		SOCKADDR_IN targetOutside{};
		targetOutside.sin_family = AF_INET;
		InetPtonW(AF_INET, L"203.0.113.5", &targetOutside.sin_addr);

		CHECK(EvaluateIpFilter(reinterpret_cast<const sockaddr*>(&targetAllowed), bypassVec, handleVec).action == IpFilterAction::Proxy);
		CHECK(EvaluateIpFilter(reinterpret_cast<const sockaddr*>(&targetBypassed), bypassVec, handleVec).action == IpFilterAction::BypassByRule);
		CHECK(EvaluateIpFilter(reinterpret_cast<const sockaddr*>(&targetOutside), bypassVec, handleVec).action == IpFilterAction::BypassNotInHandleList);

		// Commit without begin must fail cleanly.
		CHECK(aio_dial(AIO_COMMITRULES, const_cast<LPWSTR>(L"")) == FALSE);

		// Atomic rule transaction preserves live rules until commit.
		CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"game\\.exe$")) == TRUE);
		CHECK(aio_dial(AIO_CLRIP, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_BEGINRULES, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_CLRIP, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_ADDIP, const_cast<LPWSTR>(L"not-a-cidr")) == FALSE);
		{
			shared_lock<shared_mutex> sl(ruleLock);
			CHECK(handleList.size() == 1);
			CHECK(handleIpList.empty());
		}

		// Start fresh transaction & commit.
		CHECK(aio_dial(AIO_BEGINRULES, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_CLRIP, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"game\\.exe$")) == TRUE);
		CHECK(aio_dial(AIO_ADDIP, const_cast<LPWSTR>(L"198.51.100.0/24")) == TRUE);
		CHECK(aio_dial(AIO_BYPIP, const_cast<LPWSTR>(L"198.51.100.99/32")) == TRUE);
		{
			shared_lock<shared_mutex> sl(ruleLock);
			CHECK(handleIpList.empty());
		}
		CHECK(aio_dial(AIO_COMMITRULES, const_cast<LPWSTR>(L"")) == TRUE);
		{
			shared_lock<shared_mutex> sl(ruleLock);
			CHECK(handleList.size() == 1);
			CHECK(handleIpList.size() == 1);
			CHECK(bypassIpList.size() == 1);
		}

		// Integration check: tcpConnectRequest & udpSend with DNS precedence
		MockSocks5Server socks("", "");
		const wstring portStr = to_wstring(socks.Port());
		CHECK(aio_dial(AIO_TGTHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(portStr.c_str())) == TRUE);
		CHECK(aio_dial(AIO_FILTERLOOPBACK, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERINTRANET, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERTCP, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_FILTERUDP, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_FILTERDNS, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_TRACKCONNECTIONS, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_TRACKUNMATCHED, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_init() == TRUE);

		const ULONG64 startSeq = ExtractNextSeq(DrainEventsJson(0));
		auto makeTcpConn = [](USHORT localPort, const wchar_t* remoteIp, USHORT remotePort)
		{
			SOCKADDR_IN local{};
			local.sin_family = AF_INET;
			local.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
			local.sin_port = htons(localPort);
			SOCKADDR_IN remote{};
			remote.sin_family = AF_INET;
			InetPtonW(AF_INET, remoteIp, &remote.sin_addr);
			remote.sin_port = htons(remotePort);
			return pair<SOCKADDR_IN, SOCKADDR_IN>{ local, remote };
		};

		NF_TCP_CONN_INFO tcpInfo{};
		tcpInfo.processId = GetCurrentProcessId() + 70001;
		tcpInfo.ip_family = AF_INET;

		auto cAllow = makeTcpConn(41001, L"198.51.100.1", 443);
		memcpy(tcpInfo.localAddress, &cAllow.first, sizeof(SOCKADDR_IN));
		memcpy(tcpInfo.remoteAddress, &cAllow.second, sizeof(SOCKADDR_IN));
		tcpConnectRequest(701, &tcpInfo);

		auto cByp = makeTcpConn(41002, L"198.51.100.99", 443);
		memcpy(tcpInfo.localAddress, &cByp.first, sizeof(SOCKADDR_IN));
		memcpy(tcpInfo.remoteAddress, &cByp.second, sizeof(SOCKADDR_IN));
		tcpConnectRequest(702, &tcpInfo);

		auto cOut = makeTcpConn(41003, L"203.0.113.5", 80);
		memcpy(tcpInfo.localAddress, &cOut.first, sizeof(SOCKADDR_IN));
		memcpy(tcpInfo.remoteAddress, &cOut.second, sizeof(SOCKADDR_IN));
		tcpConnectRequest(703, &tcpInfo);

		{
			lock_guard<mutex> lg(g_mockLock);
			CHECK(find(g_disabledTcpEndpoints.begin(), g_disabledTcpEndpoints.end(), 701) == g_disabledTcpEndpoints.end());
			CHECK(find(g_disabledTcpEndpoints.begin(), g_disabledTcpEndpoints.end(), 702) != g_disabledTcpEndpoints.end());
			CHECK(find(g_disabledTcpEndpoints.begin(), g_disabledTcpEndpoints.end(), 703) != g_disabledTcpEndpoints.end());
		}

		// Clean up un-accepted endpoint 701
		memcpy(tcpInfo.localAddress, &cAllow.first, sizeof(SOCKADDR_IN));
		tcpClosed(701, &tcpInfo);

		// UDP DNS to 8.8.8.8:53 must hit DNS path (!filterDNS -> nf_udpPostSend -> DNS_DIRECT), not !HandleIPs bypass
		NF_UDP_CONN_INFO udpInfo{};
		udpInfo.processId = GetCurrentProcessId() + 70001;
		udpInfo.ip_family = AF_INET;
		udpCreated(801, &udpInfo);

		SOCKADDR_IN6 dnsTgt{};
		auto* dns4 = reinterpret_cast<SOCKADDR_IN*>(&dnsTgt);
		dns4->sin_family = AF_INET;
		InetPtonW(AF_INET, L"8.8.8.8", &dns4->sin_addr);
		dns4->sin_port = htons(53);
		const auto dnsPkt = BuildDnsPacket(0x1122);
		udpSend(801, reinterpret_cast<const unsigned char*>(&dnsTgt), dnsPkt.data(), static_cast<int>(dnsPkt.size()), nullptr);

		SOCKADDR_IN6 bypTgt{};
		auto* byp4 = reinterpret_cast<SOCKADDR_IN*>(&bypTgt);
		byp4->sin_family = AF_INET;
		InetPtonW(AF_INET, L"198.51.100.99", &byp4->sin_addr);
		byp4->sin_port = htons(7777);
		const char udpMsg[] = "bypass-ip-udp";
		udpSend(801, reinterpret_cast<const unsigned char*>(&bypTgt), udpMsg, static_cast<int>(sizeof(udpMsg) - 1), nullptr);

		{
			lock_guard<mutex> lg(g_mockLock);
			CHECK(g_directUdpSends == 2);
		}
		udpClosed(801, &udpInfo);

		const string evJson = DrainEventsJson(startSeq);
		CHECK(evJson.find("\"action\":\"PROXY\"") != string::npos);
		CHECK(evJson.find("\"198.51.100.0/24\"") != string::npos);
		CHECK(evJson.find("\"action\":\"BYPASS_IP\"") != string::npos);
		CHECK(evJson.find("\"198.51.100.99/32\"") != string::npos);
		CHECK(evJson.find("\"!HandleIPs\"") != string::npos);
		CHECK(evJson.find("\"action\":\"DNS_DIRECT\"") != string::npos);

		aio_free();
		CHECK(aio_dial(AIO_CLRIP, const_cast<LPWSTR>(L"")) == TRUE);
		socks.Stop();

		puts("[PASS] TestIpFilterParsingTransactionsAndPrecedence");
	}

	void TestConnectionTrackingBytesCloseAndJsonApis()
	{
		ResetMocks();
		aio_clearClosedConnections();

		USHORT echoTcpPort = 0;
		SOCKET echoTcpListener = CreateLoopbackTcpListener(echoTcpPort);
		thread echoTcpThread([echoTcpListener]()
		{
			SOCKET c = accept(echoTcpListener, NULL, NULL);
			if (c == INVALID_SOCKET)
				return;
			char buf[256];
			for (;;)
			{
				const int n = recv(c, buf, sizeof(buf), 0);
				if (n <= 0)
					break;
				if (!SendAllSock(c, buf, n))
					break;
			}
			closesocket(c);
		});

		MockSocks5Server socks("", "");
		const wstring socksPortStr = to_wstring(socks.Port());
		CHECK(aio_dial(AIO_TGTHOST, const_cast<LPWSTR>(L"127.0.0.1")) == TRUE);
		CHECK(aio_dial(AIO_TGTPORT, const_cast<LPWSTR>(socksPortStr.c_str())) == TRUE);
		CHECK(aio_dial(AIO_CLRNAME, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_CLRIP, const_cast<LPWSTR>(L"")) == TRUE);
		CHECK(aio_dial(AIO_ADDNAME, const_cast<LPWSTR>(L"game\\.exe$")) == TRUE);
		CHECK(aio_dial(AIO_TRACKCONNECTIONS, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_TRACKUNMATCHED, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERLOOPBACK, const_cast<LPWSTR>(L"false")) == TRUE);
		CHECK(aio_dial(AIO_FILTERTCP, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_dial(AIO_FILTERUDP, const_cast<LPWSTR>(L"true")) == TRUE);
		CHECK(aio_init() == TRUE);

		ULONG64 seqCursor = ExtractNextSeq(DrainEventsJson(0));

		// 1. Live TCP connection tracking + byte counts + active close
		SOCKET localClient = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		CHECK(localClient != INVALID_SOCKET);
		SOCKADDR_IN localBind{};
		localBind.sin_family = AF_INET;
		localBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(localClient, reinterpret_cast<PSOCKADDR>(&localBind), sizeof(localBind)) == 0);

		SOCKADDR_IN localBound{};
		int localBoundLen = sizeof(localBound);
		CHECK(getsockname(localClient, reinterpret_cast<PSOCKADDR>(&localBound), &localBoundLen) == 0);

		SOCKADDR_IN targetAddr{};
		targetAddr.sin_family = AF_INET;
		targetAddr.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		targetAddr.sin_port = htons(echoTcpPort);

		NF_TCP_CONN_INFO connInfo{};
		connInfo.processId = GetCurrentProcessId() + 70002;
		connInfo.ip_family = AF_INET;
		memcpy(connInfo.localAddress, &localBound, sizeof(SOCKADDR_IN));
		memcpy(connInfo.remoteAddress, &targetAddr, sizeof(SOCKADDR_IN));
		tcpConnectRequest(901, &connInfo);

		SOCKADDR_IN proxyListen{};
		proxyListen.sin_family = AF_INET;
		proxyListen.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		proxyListen.sin_port = tcpListen.load();
		CHECK(connect(localClient, reinterpret_cast<PSOCKADDR>(&proxyListen), sizeof(proxyListen)) == 0);

		const char tcpMsg[] = "track-tcp-bytes-12345";
		CHECK(SendAllSock(localClient, tcpMsg, static_cast<int>(sizeof(tcpMsg) - 1)));
		char tcpEcho[64]{};
		CHECK(RecvExactSock(localClient, tcpEcho, static_cast<int>(sizeof(tcpMsg) - 1)));
		CHECK(string(tcpEcho, sizeof(tcpMsg) - 1) == tcpMsg);

		// Give IOCP worker a moment to finish updating bytes
		this_thread::sleep_for(chrono::milliseconds(30));
		const string activeJson = QueryConnectionsJson();
		CHECK(activeJson.find("\"protocol\":\"TCP\"") != string::npos);
		CHECK(activeJson.find("\"state\":\"Active\"") != string::npos);
		CHECK(activeJson.find("\"processName\":\"game.exe\"") != string::npos);
		const ULONG64 connId = ExtractFirstConnId(activeJson);
		CHECK(connId > 0);
		CHECK(activeJson.find("\"upBytes\":21") != string::npos);
		CHECK(activeJson.find("\"downBytes\":21") != string::npos);

		// Force-close connection via exported API
		CHECK(aio_closeConnection(connId) == TRUE);
		this_thread::sleep_for(chrono::milliseconds(30));

		const string closedJson = QueryConnectionsJson();
		CHECK(closedJson.find("\"active\":[]") != string::npos);
		CHECK(closedJson.find("\"closeReason\":\"Closed by user\"") != string::npos);

		closesocket(localClient);

		// 2. TrackUnmatched toggle + 10s event deduplication for BYPASS_PROC
		{
			lock_guard<mutex> lg(g_mockLock);
			g_mockProcessName = L"C:\\Tools\\otherapp.exe";
		}
		NF_TCP_CONN_INFO unmatchedInfo{};
		unmatchedInfo.processId = GetCurrentProcessId() + 70003;
		unmatchedInfo.ip_family = AF_INET;
		memcpy(unmatchedInfo.localAddress, &localBound, sizeof(SOCKADDR_IN));
		memcpy(unmatchedInfo.remoteAddress, &targetAddr, sizeof(SOCKADDR_IN));
		tcpConnectRequest(902, &unmatchedInfo);

		string evBatch1 = DrainEventsJson(seqCursor);
		seqCursor = ExtractNextSeq(evBatch1);
		CHECK(evBatch1.find("BYPASS_PROC") == string::npos);

		CHECK(aio_dial(AIO_TRACKUNMATCHED, const_cast<LPWSTR>(L"true")) == TRUE);
		tcpConnectRequest(903, &unmatchedInfo);
		tcpConnectRequest(904, &unmatchedInfo); // duplicate within 10s -> deduplicated
		string evBatch2 = DrainEventsJson(seqCursor);
		seqCursor = ExtractNextSeq(evBatch2);
		const size_t firstBypassPos = evBatch2.find("BYPASS_PROC");
		CHECK(firstBypassPos != string::npos);
		CHECK(evBatch2.find("BYPASS_PROC", firstBypassPos + 1) == string::npos);

		// 3. Proxied UDP flow tracking + upload/download byte counts + udpClosed
		{
			lock_guard<mutex> lg(g_mockLock);
			g_mockProcessName = L"C:\\Games\\game.exe";
		}
		SOCKET echoUdp = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		CHECK(echoUdp != INVALID_SOCKET);
		SOCKADDR_IN echoUdpBind{};
		echoUdpBind.sin_family = AF_INET;
		echoUdpBind.sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		CHECK(::bind(echoUdp, reinterpret_cast<PSOCKADDR>(&echoUdpBind), sizeof(echoUdpBind)) == 0);
		const USHORT echoUdpPort = GetBoundPort(echoUdp);
		atomic<bool> stopEchoUdp{ false };
		thread echoUdpThread([&]()
		{
			char buf[512];
			SOCKADDR_IN from{};
			int fromLen = sizeof(from);
			while (!stopEchoUdp.load())
			{
				const int n = recvfrom(echoUdp, buf, sizeof(buf), 0, reinterpret_cast<PSOCKADDR>(&from), &fromLen);
				if (n <= 0)
					break;
				sendto(echoUdp, buf, n, 0, reinterpret_cast<PSOCKADDR>(&from), fromLen);
			}
		});

		NF_UDP_CONN_INFO udpConnInfo{};
		udpConnInfo.processId = GetCurrentProcessId() + 70004;
		udpConnInfo.ip_family = AF_INET;
		udpCreated(950, &udpConnInfo);

		SOCKADDR_IN6 udpTarget{};
		auto* udpT4 = reinterpret_cast<SOCKADDR_IN*>(&udpTarget);
		udpT4->sin_family = AF_INET;
		udpT4->sin_addr.S_un.S_addr = htonl(INADDR_LOOPBACK);
		udpT4->sin_port = htons(echoUdpPort);
		const char udpPayload[] = "udp-bytes-9";
		udpSend(950, reinterpret_cast<const unsigned char*>(&udpTarget), udpPayload, static_cast<int>(sizeof(udpPayload) - 1), nullptr);
		{
			unique_lock<mutex> ul(g_mockLock);
			const bool gotUdp = g_mockCv.wait_for(ul, chrono::seconds(5), []() { return !g_udpReceives.empty(); });
			CHECK(gotUdp);
		}

		const string udpActiveJson = QueryConnectionsJson();
		CHECK(udpActiveJson.find("\"protocol\":\"UDP\"") != string::npos);
		CHECK(udpActiveJson.find("\"upBytes\":11") != string::npos);
		CHECK(udpActiveJson.find("\"downBytes\":11") != string::npos);

		udpClosed(950, &udpConnInfo);
		const string udpClosedJson = QueryConnectionsJson();
		CHECK(udpClosedJson.find("\"active\":[]") != string::npos);
		CHECK(udpClosedJson.find("\"closeReason\":\"Socket closed\"") != string::npos);

		stopEchoUdp = true;
		closesocket(echoUdp);
		echoUdpThread.join();

		// 4. ClearClosedConnections
		aio_clearClosedConnections();
		CHECK(QueryConnectionsJson() == "{\"active\":[],\"closed\":[]}");

		aio_free();
		closesocket(echoTcpListener);
		echoTcpThread.join();
		socks.Stop();

		puts("[PASS] TestConnectionTrackingBytesCloseAndJsonApis");
	}
}

int main()
{
	WSADATA wsa{};
	CHECK(WSAStartup(MAKEWORD(2, 2), &wsa) == 0);

	TestDialValidationAndSharedRules();
	TestAioInitRollbackAndLifecycle();
	TestTcpRelayAuthPayloadHalfCloseAndCleanFree();
	TestUdpRelayNonBlockingSendOptionsAndContextLimit();
	TestIcmpSchedulerOrderingAndCancellation();
	TestDnsDirectAndProxiedMultiplexing();
	TestLifecycleDnsRefCountBypassLoopAndSpoofProtection();
	TestIpFilterParsingTransactionsAndPrecedence();
	TestConnectionTrackingBytesCloseAndJsonApis();

	WSACleanup();
	puts("ALL REDIRECTOR C++ TESTS PASSED");
	return 0;
}

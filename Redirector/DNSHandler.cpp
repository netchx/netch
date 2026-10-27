#include "DNSHandler.h"

#include "SocksHelper.h"

extern bool dnsProx;
extern string dnsHost;
extern USHORT dnsPort;

SOCKADDR_IN6 dnsAddr;
constexpr size_t DnsResponseBufferSize = 65535;

namespace
{
	// Proxied DNS used to open a new SOCKS5 control connection, perform a UDP
	// ASSOCIATE and spawn two threads for every single query. With remote
	// protocols that map one UDP association to one upstream session (VLESS,
	// VMess, Trojan, ...) that also meant a fresh TLS handshake to the proxy
	// server per lookup. Queries now share one long-lived association and are
	// multiplexed by rewriting the DNS transaction ID.
	constexpr auto DnsQueryTimeout = chrono::seconds(5);
	constexpr size_t MaxPendingQueries = 4096;
	constexpr size_t RecycleAfterUnansweredQueries = 3;
	constexpr long long RelayRetryCooldownMs = 1000;

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

	struct DnsRelay
	{
		shared_ptr<SocksHelper::UDP> udp;

		mutex lock;
		unordered_map<USHORT, PendingQuery> pending;
		USHORT nextId = 0;
		size_t unansweredExpired = 0;
	};

	mutex relayLock;
	shared_ptr<DnsRelay> currentRelay;

	// Serializes the slow SOCKS handshake so a burst of queries creates one
	// association instead of one per waiting thread.
	mutex relaySetupLock;
	atomic<long long> relayRetryAfterMs{ 0 };

	long long NowMs()
	{
		return chrono::duration_cast<chrono::milliseconds>(
			chrono::steady_clock::now().time_since_epoch()).count();
	}

	vector<char> CopyOptions(PNF_UDP_OPTIONS options)
	{
		// NF_UDP_OPTIONS ends with a one byte flexible array.
		vector<char> copy(sizeof(NF_UDP_OPTIONS) + options->optionsLength);
		memcpy(copy.data(), options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);
		return copy;
	}

	void StopRelay(const shared_ptr<DnsRelay>& relay)
	{
		relay->udp->Stop();

		lock_guard<mutex> lg(relayLock);
		if (currentRelay == relay)
			currentRelay.reset();
	}

	void RelayReceiveLoop(shared_ptr<DnsRelay> relay)
	{
		vector<char> buffer(DnsResponseBufferSize);

		while (!relay->udp->stopping)
		{
			int payloadOffset = 0;
			const int size = relay->udp->Read(NULL, buffer.data(), static_cast<int>(buffer.size()), &payloadOffset, NULL);
			if (size == SOCKET_ERROR)
				break;
			if (size < 2)
				continue;

			char* payload = buffer.data() + payloadOffset;
			USHORT wireId = 0;
			memcpy(&wireId, payload, sizeof(wireId));

			PendingQuery query;
			{
				lock_guard<mutex> lg(relay->lock);
				auto it = relay->pending.find(wireId);
				if (it == relay->pending.end())
					continue;

				query = move(it->second);
				relay->pending.erase(it);
				relay->unansweredExpired = 0;
			}

			memcpy(payload, &query.originalId, sizeof(query.originalId));
			nf_udpPostReceive(query.id, (PBYTE)&query.target, payload, size, (PNF_UDP_OPTIONS)query.options.data());
		}

		StopRelay(relay);

		lock_guard<mutex> lg(relay->lock);
		relay->pending.clear();
	}

	shared_ptr<DnsRelay> GetRelay(bool allowConnect)
	{
		{
			lock_guard<mutex> lg(relayLock);
			if (currentRelay && !currentRelay->udp->stopping)
				return currentRelay;
		}

		if (!allowConnect)
			return nullptr;

		lock_guard<mutex> setup(relaySetupLock);
		{
			lock_guard<mutex> lg(relayLock);
			if (currentRelay && !currentRelay->udp->stopping)
				return currentRelay;
		}

		if (NowMs() < relayRetryAfterMs.load())
			return nullptr;

		auto udp = make_shared<SocksHelper::UDP>();
		if (!udp->Associate() || !udp->CreateUDP())
		{
			udp->Stop();
			relayRetryAfterMs = NowMs() + RelayRetryCooldownMs;
			return nullptr;
		}

		auto relay = make_shared<DnsRelay>();
		relay->udp = udp;
		relay->nextId = static_cast<USHORT>(GetTickCount64());

		{
			lock_guard<mutex> lg(relayLock);
			currentRelay = relay;
		}

		thread(RelayReceiveLoop, relay).detach();
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

			// The association can stay open while the upstream path is dead.
			// Recycle it once several queries in a row went unanswered.
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

				relay->pending.emplace(wireId, move(pending));
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
		if (relay->udp->Send(&dnsAddr, packet.data(), length) != length)
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

	void SendViaNewRelay(DnsQuery query)
	{
		SendViaRelay(query, true);
	}
}

void HandleClientDNS(ENDPOINT_ID id, PSOCKADDR_IN6 target, char* packet, int length, PNF_UDP_OPTIONS option)
{
	const auto family = dnsAddr.sin6_family == AF_INET ? AF_INET : AF_INET6;
	auto remote = socket(family, SOCK_DGRAM, IPPROTO_UDP);
	if (remote != INVALID_SOCKET)
	{
		bool bound = false;
		if (family == AF_INET)
		{
			SOCKADDR_IN addr{};
			addr.sin_family = AF_INET;
			bound = bind(remote, (PSOCKADDR)&addr, sizeof(addr)) != SOCKET_ERROR;
		}
		else
		{
			SOCKADDR_IN6 addr;
			IN6ADDR_SETANY(&addr);
			bound = bind(remote, (PSOCKADDR)&addr, sizeof(addr)) != SOCKET_ERROR;
		}

		if (bound)
		{
			const int targetLength = family == AF_INET ? sizeof(SOCKADDR_IN) : sizeof(SOCKADDR_IN6);
			if (sendto(remote, packet, length, 0, (PSOCKADDR)&dnsAddr, targetLength) == length)
			{
				timeval timeout{};
				timeout.tv_sec = 4;

				fd_set fds;
				FD_ZERO(&fds);
				FD_SET(remote, &fds);

				int size = select(NULL, &fds, NULL, NULL, &timeout);
				if (size != 0 && size != SOCKET_ERROR)
				{
					vector<char> buffer(DnsResponseBufferSize);

					size = recvfrom(remote, buffer.data(), static_cast<int>(buffer.size()), 0, NULL, NULL);
					if (size != 0 && size != SOCKET_ERROR)
						nf_udpPostReceive(id, (PBYTE)target, buffer.data(), size, option);
				}
			}
		}
	}

	if (remote != INVALID_SOCKET)
		closesocket(remote);

	delete target;
	delete[] packet;
	delete[] option;
}

bool DNSHandler::INIT()
{
	DNSHandler::FREE();

	memset(&dnsAddr, 0, sizeof(dnsAddr));

	auto ipv4 = (PSOCKADDR_IN)&dnsAddr;
	if (inet_pton(AF_INET, dnsHost.c_str(), &ipv4->sin_addr) == 1)
	{
		ipv4->sin_family = AF_INET;
		ipv4->sin_port = htons(dnsPort);
		return true;
	}

	auto ipv6 = (PSOCKADDR_IN6)&dnsAddr;
	if (inet_pton(AF_INET6, dnsHost.c_str(), &ipv6->sin6_addr) == 1)
	{
		ipv6->sin6_family = AF_INET6;
		ipv6->sin6_port = htons(dnsPort);
		return true;
	}

	return false;
}

void DNSHandler::FREE()
{
	shared_ptr<DnsRelay> relay;
	{
		lock_guard<mutex> lg(relayLock);
		relay.swap(currentRelay);
	}

	if (relay)
		relay->udp->Stop();

	relayRetryAfterMs = 0;
}

bool DNSHandler::IsDNS(PSOCKADDR_IN6 target)
{
	if (target->sin6_family == AF_INET)
		return ((PSOCKADDR_IN)target)->sin_port == htons(53);
	else
		return target->sin6_port == htons(53);
}

void DNSHandler::CreateHandler(ENDPOINT_ID id, PSOCKADDR_IN6 target, const char* packet, int length, PNF_UDP_OPTIONS options)
{
	if (length <= 0)
		return;

	if (dnsProx)
	{
		// A DNS message starts with a 12 byte header; anything shorter is
		// malformed and cannot be multiplexed by transaction ID.
		if (length < 12)
			return;

		DnsQuery query;
		query.id = id;
		memcpy(&query.target, target, sizeof(SOCKADDR_IN6));
		query.options = CopyOptions(options);
		query.packet.assign(packet, packet + length);

		// Fast path: the shared association already exists, so sending is a
		// single non-blocking datagram and needs no extra thread.
		if (SendViaRelay(query, false))
			return;

		// Establishing the association blocks on the SOCKS handshake; never
		// do that on the Netfilter callback thread.
		thread(SendViaNewRelay, move(query)).detach();
		return;
	}

	auto remote = new SOCKADDR_IN6();
	auto buffer = new char[length]();
	auto option = (PNF_UDP_OPTIONS)new char[sizeof(NF_UDP_OPTIONS) + options->optionsLength];

	memcpy(remote, target, sizeof(SOCKADDR_IN6));
	memcpy(buffer, packet, length);
	memcpy(option, options, sizeof(NF_UDP_OPTIONS) + options->optionsLength - 1);

	thread(HandleClientDNS, id, remote, buffer, length, option).detach();
}

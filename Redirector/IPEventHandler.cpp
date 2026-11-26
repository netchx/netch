#include "IPEventHandler.h"

extern atomic<DWORD> icmping;

namespace
{
	constexpr size_t MaxPendingIcmp = 1024;

	struct DelayedIcmpPacket
	{
		chrono::steady_clock::time_point due;
		uint64_t seq = 0;
		vector<BYTE> data;
		NF_IP_PACKET_OPTIONS options{};

		bool operator>(const DelayedIcmpPacket& other) const
		{
			if (due != other.due)
				return due > other.due;
			return seq > other.seq;
		}
	};

	mutex g_icmpLock;
	condition_variable g_icmpCv;
	bool g_icmpRunning = false;
	uint64_t g_nextSeq = 0;
	priority_queue<DelayedIcmpPacket, vector<DelayedIcmpPacket>, greater<DelayedIcmpPacket>> g_icmpQueue;
	thread g_icmpThread;

	void SchedulerLoop()
	{
		unique_lock<mutex> ul(g_icmpLock);
		while (g_icmpRunning)
		{
			if (g_icmpQueue.empty())
			{
				g_icmpCv.wait(ul, []() { return !g_icmpRunning || !g_icmpQueue.empty(); });
				continue;
			}

			const auto nextDue = g_icmpQueue.top().due;
			const auto now = chrono::steady_clock::now();
			if (now < nextDue)
			{
				g_icmpCv.wait_until(ul, nextDue);
				continue;
			}

			auto pkt = g_icmpQueue.top();
			g_icmpQueue.pop();
			ul.unlock();

			nf_ipPostReceive(
				reinterpret_cast<const char*>(pkt.data.data()),
				static_cast<int>(pkt.data.size()),
				&pkt.options);

			ul.lock();
		}
	}
}

USHORT IPv4Checksum(PBYTE buffer, ULONG64 size)
{
	UINT32 sum = 0;
	for (ULONG64 i = 0; i + 1 < size; i += 2)
	{
		sum += (buffer[i] << 8) + buffer[i + 1];
	}

	if ((size % 2) == 1)
	{
		sum += buffer[size - 1] << 8;
	}

	while (sum > 0xffff)
	{
		sum = (sum >> 16) + (sum & 0xffff);
	}

	return ~sum & 0xffff;
}

USHORT ICMPChecksum(PBYTE buffer, ULONG64 size)
{
	UINT32 sum = 0;
	for (ULONG64 i = 0; i + 1 < size; i += 2)
	{
		sum += buffer[i] + (buffer[i + 1] << 8);
	}

	if ((size % 2) == 1)
	{
		sum += buffer[size - 1];
	}

	sum = (sum >> 16) + (sum & 0xffff);
	sum += (sum >> 16);

	return ~sum & 0xffff;
}

bool IPEventHandler::INIT()
{
	IPEventHandler::FREE();

	{
		lock_guard<mutex> lg(g_icmpLock);
		g_icmpRunning = true;
		g_nextSeq = 0;
		while (!g_icmpQueue.empty())
			g_icmpQueue.pop();
	}

	try
	{
		g_icmpThread = thread(SchedulerLoop);
	}
	catch (...)
	{
		lock_guard<mutex> lg(g_icmpLock);
		g_icmpRunning = false;
		return false;
	}

	return true;
}

void IPEventHandler::FREE()
{
	{
		lock_guard<mutex> lg(g_icmpLock);
		g_icmpRunning = false;
		while (!g_icmpQueue.empty())
			g_icmpQueue.pop();
	}
	g_icmpCv.notify_all();

	if (g_icmpThread.joinable())
		g_icmpThread.join();
}

void ipSend(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options)
{
	if (buffer == nullptr || options == nullptr ||
		options->ip_family != AF_INET ||
		options->ipHeaderSize < 20 || options->ipHeaderSize > 60 || (options->ipHeaderSize % 4) != 0 ||
		length < static_cast<int>(options->ipHeaderSize) + 8 ||
		(static_cast<unsigned char>(buffer[0]) >> 4) != 4 ||
		((((static_cast<unsigned char>(buffer[6]) << 8) | static_cast<unsigned char>(buffer[7])) & 0x3FFF) != 0) ||
		static_cast<unsigned char>(buffer[options->ipHeaderSize]) != 0x08 ||
		static_cast<unsigned char>(buffer[options->ipHeaderSize + 1]) != 0x00)
	{
		nf_ipPostSend(buffer, length, options);
		return;
	}

	vector<BYTE> data(buffer, buffer + length);
	auto* packet = data.data();

	{
		BYTE src[4];
		BYTE dst[4];
		memcpy(src, packet + 12, 4);
		memcpy(dst, packet + 16, 4);
		memcpy(packet + 12, dst, 4);
		memcpy(packet + 16, src, 4);
	}

	packet[10] = 0x00;
	packet[11] = 0x00;
	auto ipv4sum = IPv4Checksum(packet, options->ipHeaderSize);
	packet[10] = (ipv4sum >> 8);
	packet[11] = ipv4sum & 0xff;

	packet[options->ipHeaderSize] = 0x00;
	packet[options->ipHeaderSize + 2] = 0x00;
	packet[options->ipHeaderSize + 3] = 0x00;
	auto icmpsum = ICMPChecksum(packet + options->ipHeaderSize, (ULONG64)length - options->ipHeaderSize);
	packet[options->ipHeaderSize + 2] = icmpsum & 0xff;
	packet[options->ipHeaderSize + 3] = (icmpsum >> 8);

	REDIRECTOR_TRACE("[Redirector][IPEventHandler][ipSend] Fake ICMP response for %d.%d.%d.%d\n", packet[12], packet[13], packet[14], packet[15]);

	const DWORD delay = (std::min)(icmping.load(), static_cast<DWORD>(60000));
	if (delay == 0)
	{
		nf_ipPostReceive(reinterpret_cast<const char*>(packet), length, options);
		return;
	}

	lock_guard<mutex> lg(g_icmpLock);
	if (!g_icmpRunning || g_icmpQueue.size() >= MaxPendingIcmp)
		return;

	DelayedIcmpPacket item;
	item.due = chrono::steady_clock::now() + chrono::milliseconds(delay);
	item.seq = g_nextSeq++;
	item.data = std::move(data);
	item.options = *options;
	g_icmpQueue.push(std::move(item));
	g_icmpCv.notify_one();
}

void ipReceive(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options)
{
	nf_ipPostReceive(buffer, length, options);
}

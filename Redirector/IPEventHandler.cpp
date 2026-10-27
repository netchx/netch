#include "IPEventHandler.h"

extern DWORD icmping;

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

void ipSend(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options)
{
	if (options->ip_family != AF_INET ||
		options->ipHeaderSize != 20   ||
		length < 28                   ||
		buffer[options->ipHeaderSize] != 0x08)
	{
		nf_ipPostSend(buffer, length, options);
		return;
	}

	auto data = make_shared<vector<BYTE>>(buffer, buffer + length);
	auto packet = data->data();

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

	const DWORD delay = icmping;
	if (delay == 0)
	{
		nf_ipPostReceive((char*)packet, length, options);
		return;
	}

	// Simulated latency must not stall the Netfilter IP callback thread,
	// which would delay every other filtered packet behind this one.
	NF_IP_PACKET_OPTIONS optionsCopy = *options;
	thread([data, optionsCopy, delay]() mutable
	{
		this_thread::sleep_for(chrono::milliseconds(delay));
		nf_ipPostReceive((char*)data->data(), static_cast<int>(data->size()), &optionsCopy);
	}).detach();
}

void ipReceive(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options)
{
	nf_ipPostReceive(buffer, length, options);
}

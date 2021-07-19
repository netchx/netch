#include "IPEventHandler.h"

#include "Utils.h"

#include <stdio.h>

using namespace std;

void ipSend(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options)
{
	if (options->ip_family != AF_INET ||
		options->ipHeaderSize != 20   ||
		length < 28                   ||
		buffer[options->ipHeaderSize] != 0x08)
	{
		UNREFERENCED_PARAMETER(nf_ipPostSend(buffer, length, options));
		return;
	}

	auto data = (PBYTE)malloc(length);
	if (!data)
	{
		puts("[Redirector][IPEventHandler][ipSend] Unable to allocate memory");

		UNREFERENCED_PARAMETER(nf_ipPostSend(buffer, length, options));
		return;
	}
	memcpy(data, buffer, length);

	{
		BYTE src[4];
		BYTE dst[4];
		memcpy(src, data + 12, 4);
		memcpy(dst, data + 16, 4);
		memcpy(data + 12, dst, 4);
		memcpy(data + 16, src, 4);
	}

	data[10] = 0x00;
	data[11] = 0x00;
	auto ipv4sum = IPv4Checksum(data, options->ipHeaderSize);
	data[10] = (ipv4sum >> 8);
	data[11] = ipv4sum & 0xff;

	data[options->ipHeaderSize] = 0x00;
	data[options->ipHeaderSize + 2] = 0x00;
	data[options->ipHeaderSize + 3] = 0x00;
	auto icmpsum = ICMPChecksum(data + options->ipHeaderSize, (ULONG64)length - options->ipHeaderSize);
	data[options->ipHeaderSize + 2] = icmpsum & 0xff;
	data[options->ipHeaderSize + 3] = (icmpsum >> 8);

	printf("[Redirector][ipSend] Fake ICMP response for %d.%d.%d.%d\n", data[12], data[13], data[14], data[15]);

	nf_ipPostReceive((PCHAR)data, length, options);
	free(data);
}

void ipReceive(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options)
{
	UNREFERENCED_PARAMETER(nf_ipPostReceive(buffer, length, options));
}

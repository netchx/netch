#include "Utils.h"

#include "Data.h"

DWORD GetCPUCount()
{
	SYSTEM_INFO info;
	GetSystemInfo(&info);

	return info.dwNumberOfProcessors;
}

USHORT IPv4Checksum(PBYTE buffer, ULONG64 size)
{
	UINT32 sum = 0;
	for (int i = 0; i < size; i += 2)
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
	for (int i = 0; i < size; i += 2)
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

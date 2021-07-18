#pragma once
#ifndef UTILS_H
#define UTILS_H

USHORT IPv4Checksum(PBYTE buffer, ULONG64 size);
USHORT ICMPChecksum(PBYTE buffer, ULONG64 size);

#endif

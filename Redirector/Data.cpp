#include "Data.h"

BOOL   filterLoopback = FALSE;
BOOL   filterICMP     = TRUE;
BOOL   filterTCP      = TRUE;
BOOL   filterUDP      = TRUE;
USHORT tcpLisn = 0;
USHORT udpLisn = 0;

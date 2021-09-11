#include "Based.h"

BOOL filterLoopback = FALSE;
BOOL filterIntranet = FALSE;
BOOL filterICMP = TRUE;
BOOL filterTCP = TRUE;
BOOL filterUDP = TRUE;
vector<wstring> bypassList;
vector<wstring> handleList;

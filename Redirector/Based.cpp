#include "Based.h"

BOOL filterLoopback = FALSE;
BOOL filterICMP = TRUE;
BOOL filterTCP = TRUE;
BOOL filterUDP = TRUE;
USHORT tcpPort = 0;
USHORT udpPort = 0;
vector<wstring> bypassList;
vector<wstring> handleList;

mutex tcpLock;
mutex udpLock;
map<ENDPOINT_ID, PTCPINFO> tcpContext;
map<ENDPOINT_ID, PUDPINFO> udpContext;

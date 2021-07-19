#include "Data.h"

#include <map>
#include <mutex>
#include <string>
#include <vector>

using namespace std;

BOOL   Started    = FALSE;
BOOL   filterLoop = FALSE;
BOOL   filterICMP = TRUE;
BOOL   filterTCP  = TRUE;
BOOL   filterUDP  = TRUE;
BOOL   dnsHook    = FALSE;
string dnsHost    = "";
USHORT dnsPort    = 0;
USHORT apiLisn    = 0;
USHORT tcpLisn    = 0;
USHORT udpLisn    = 0;
vector<wstring> handleList;
vector<wstring> bypassList;

atomic_ulong UP{ 0 };
atomic_ulong DL{ 0 };

HANDLE TCPLock = NULL;
HANDLE UDPLock = NULL;
map<ENDPOINT_ID, PTCPINFO> TCPContext;
map<ENDPOINT_ID, PUDPINFO> UDPContext;

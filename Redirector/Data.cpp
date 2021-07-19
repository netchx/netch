#include "Data.h"

#include <mutex>
#include <string>
#include <vector>

using namespace std;

BOOL Started = FALSE;
BOOL filterLoop = FALSE;
BOOL filterICMP = TRUE;
BOOL filterTCP = TRUE;
BOOL filterUDP = TRUE;
BOOL dnsHook = FALSE;
string dnsHost = "";
USHORT dnsPort = 0;
USHORT tcpLisn = 0;
USHORT udpLisn = 0;

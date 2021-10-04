#include "Based.h"

BOOL filterLoopback = FALSE;
BOOL filterIntranet = FALSE;
BOOL filterICMP = TRUE;
BOOL filterTCP = TRUE;
BOOL filterUDP = TRUE;
BOOL filterDNS = TRUE;

DWORD icmping = 0;

wstring dnsHost = L"1.1.1.1";
USHORT dnsPort = 443;

wstring tgtHost = L"127.0.0.1";
wstring tgtPort = L"1080";
string tgtUsername = "";
string tgtPassword = "";

vector<wstring> bypassList;
vector<wstring> handleList;

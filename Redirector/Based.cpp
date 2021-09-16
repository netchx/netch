#include "Based.h"

BOOL filterLoopback = FALSE;
BOOL filterIntranet = FALSE;
BOOL filterICMP = TRUE;
BOOL filterTCP = TRUE;
BOOL filterUDP = TRUE;
wstring tgtHost = L"1.1.1.1";
wstring tgtPort = L"1080";
string tgtUsername = "";
string tgtPassword = "";
vector<wstring> bypassList;
vector<wstring> handleList;

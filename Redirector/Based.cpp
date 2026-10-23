#include "Based.h"

bool filterLoopback = false;
bool filterIntranet = false;
bool filterParent = false;
bool filterICMP = true;
bool filterTCP = true;
bool filterUDP = true;
bool filterDNS = true;

DWORD icmping = 0;

bool dnsOnly = false;
bool dnsProx = true;
string dnsHost = "1.1.1.1";
USHORT dnsPort = 53;

wstring tgtHost = L"127.0.0.1";
wstring tgtPort = L"1080";
string tgtUsername = "";
string tgtPassword = "";

// Rules are compiled when they are received from the managed UI. Matching
// against these objects avoids constructing a wregex for every socket event.
vector<wregex> bypassList;
vector<wregex> handleList;
mutex ruleLock;

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

mutex nameRulesLock;
vector<wregex> bypassRules;
vector<wregex> handleRules;

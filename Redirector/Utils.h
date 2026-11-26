#pragma once
#ifndef UTILS_H
#define UTILS_H
#include "Based.h"

string ws2s(const wstring& str);
wstring s2ws(const string& str);

// Relay threads spend almost all their time blocked in recv(). Raising their
// priority lets them preempt game threads the moment a packet arrives, which
// cuts forwarding jitter when the game keeps every core busy.
void BoostRelayThread();
#endif

#pragma once
#ifndef UTILS_H
#define UTILS_H
#include "Based.h"

string ws2s(wstring str);
wstring s2ws(string str);

// Relay threads spend almost all their time blocked in recv(). Raising their
// priority lets them preempt game threads the moment a packet arrives, which
// cuts forwarding jitter when the game keeps every core busy.
void BoostRelayThread();
#endif

#pragma once
#ifndef IPEVENTHANDLER_H
#define IPEVENTHANDLER_H
#include "Based.h"

void ipSend(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options);
void ipReceive(const char* buffer, int length, PNF_IP_PACKET_OPTIONS options);

#endif

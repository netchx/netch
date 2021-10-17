#pragma once
#ifndef EVENTHANDLER_H
#define EVENTHANDLER_H
#include "Based.h"
#include "SocksHelper.h"

bool eh_init();
void eh_free();

void threadStart();
void threadEnd();
void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO info);
void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO info);
void tcpCanSend(ENDPOINT_ID id);
void tcpSend(ENDPOINT_ID id, const char* buffer, int length);
void tcpCanReceive(ENDPOINT_ID id);
void tcpReceive(ENDPOINT_ID id, const char* buffer, int length);
void tcpClosed(ENDPOINT_ID id, PNF_TCP_CONN_INFO info);
void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info);
void udpConnectRequest(ENDPOINT_ID id, PNF_UDP_CONN_REQUEST info);
void udpCanSend(ENDPOINT_ID id);
void udpSend(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options);
void udpCanReceive(ENDPOINT_ID id);
void udpReceive(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options);
void udpClosed(ENDPOINT_ID id, PNF_UDP_CONN_INFO info);

void udpReceiveHandler(ENDPOINT_ID id, SocksHelper::PUDP remote, PNF_UDP_OPTIONS options);

#endif

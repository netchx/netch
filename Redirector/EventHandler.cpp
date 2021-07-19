#include "EventHandler.h"

#include "Data.h"

#include <stdio.h>

BOOL checkBypassName(DWORD id)
{
	return FALSE;
}

BOOL checkHandleName(DWORD id)
{
	return FALSE;
}

BOOL eh_init()
{
	return TRUE;
}

void eh_free()
{
	
}

void threadStart()
{

}

void threadEnd()
{

}

void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{
	nf_tcpDisableFiltering(id);
}

void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{

}

void tcpCanSend(ENDPOINT_ID id)
{

}

void tcpSend(ENDPOINT_ID id, const char* buffer, int length)
{
	nf_tcpPostSend(id, buffer, length);
}

void tcpCanReceive(ENDPOINT_ID id)
{

}

void tcpReceive(ENDPOINT_ID id, const char* buffer, int length)
{
	nf_tcpPostReceive(id, buffer, length);
}

void tcpClosed(ENDPOINT_ID id, PNF_TCP_CONN_INFO info)
{

}

void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{
	nf_udpDisableFiltering(id);
}

void udpConnectRequest(ENDPOINT_ID id, PNF_UDP_CONN_REQUEST info)
{

}

void udpCanSend(ENDPOINT_ID id)
{

}

void udpSend(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options)
{
	nf_udpPostSend(id, target, buffer, length, options);
}

void udpCanReceive(ENDPOINT_ID id)
{

}

void udpReceive(ENDPOINT_ID id, const unsigned char* target, const char* buffer, int length, PNF_UDP_OPTIONS options)
{
	nf_udpPostReceive(id, target, buffer, length, options);
}

void udpClosed(ENDPOINT_ID id, PNF_UDP_CONN_INFO info)
{

}

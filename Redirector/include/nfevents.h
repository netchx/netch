//
// 	NetFilterSDK 
// 	Copyright (C) Vitaly Sidorov
//	All rights reserved.
//
//	This file is a part of the NetFilter SDK.
//	The code and information is provided "as-is" without
//	warranty of any kind, either expressed or implied.
//


#ifndef _NFEVENTS_H
#define _NFEVENTS_H
#pragma warning(disable: 26812)

/**
*	Return status codes
**/
typedef enum _NF_STATUS
{
	NF_STATUS_SUCCESS		= 0,
	NF_STATUS_FAIL			= -1,
	NF_STATUS_INVALID_ENDPOINT_ID	= -2,
	NF_STATUS_NOT_INITIALIZED	= -3,
	NF_STATUS_IO_ERROR		= -4,
	NF_STATUS_REBOOT_REQUIRED	= -5
} NF_STATUS;

#define _C_API
#ifndef _C_API

	#define NFAPI_NS	nfapi::
	#define NFAPI_CC	

	/////////////////////////////////////////////////////////////////////////////////////
	// C++ API
	/////////////////////////////////////////////////////////////////////////////////////

	namespace nfapi
	{
		#include <nfdriver.h>

		/**
		*	Filtering events
		**/
		class NF_EventHandler
		{
		public:

			/**
			* Called immediately after starting the filtering thread.
			* Use this event for thread-specific initialization, e.g. calling 
			* CoInitialize() etc.
			**/
			virtual void threadStart() = 0;

			/**
			* Called before stopping the thread.
			**/
			virtual void threadEnd() = 0;
			
			//
			// TCP events
			//

			/**
			* Called before establishing an outgoing TCP connection, 
			* when NF_INDICATE_CONNECT_REQUESTS flag is specified in an appropriate rule.
			* It is possible to change pConnInfo->filteringFlag and pConnInfo->remoteAddress
			* in this handler. The changes will be applied to connection.
			* @param id Unique connection identifier
			* @param pConnInfo Connection parameters, see <tt>NF_TCP_CONN_INFO</tt>
			**/
			virtual void tcpConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo) = 0;

			/**
			* Called after successful establishing the incoming or outgoing TCP connection.
			* @param id Unique connection identifier
			* @param pConnInfo Connection parameters, see <tt>NF_TCP_CONN_INFO</tt>
			**/
			virtual void tcpConnected(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo) = 0;

			/**
			* Called after closing the connection identified by id.
			* @param id Unique connection identifier
			* @param pConnInfo Connection parameters, see <tt>NF_TCP_CONN_INFO</tt>
			**/
			virtual void tcpClosed(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo) = 0;

			/**
			* Indicates the buffer received from server.
			* @param id Unique connection identifier
			* @param buf Pointer to data buffer
			* @param len Buffer length
			**/
			virtual void tcpReceive(ENDPOINT_ID id, const char * buf, int len) = 0;

			/**
			* Indicates the buffer sent from the local socket.
			* @param id Unique connection identifier
			* @param buf Pointer to data buffer
			* @param len Buffer length
			**/
			virtual void tcpSend(ENDPOINT_ID id, const char * buf, int len) = 0;

			/**
			* Informs that the internal buffer for receives is empty and
			* it is possible to call nf_tcpPostReceive for pushing receives
			* via specified connection.
			* @param id Unique connection identifier
			**/
			virtual void tcpCanReceive(ENDPOINT_ID id) = 0;

			/**
			* Informs that the internal buffer for sends is empty and
			* it is possible to call nf_tcpPostSend for pushing sends
			* via specified connection.
			* @param id Unique connection identifier
			**/
			virtual void tcpCanSend(ENDPOINT_ID id) = 0;


			//
			// UDP events
			//

			/**
			* Called after creating UDP socket.
			* @param id Unique socket identifier
			* @param pConnInfo Socket parameters, see <tt>NF_UDP_CONN_INFO</tt>
			**/
			virtual void udpCreated(ENDPOINT_ID id, PNF_UDP_CONN_INFO pConnInfo) = 0;

			/**
			* Called before establishing an outgoing UDP connection, 
			* when NF_INDICATE_CONNECT_REQUESTS flag is specified in an appropriate rule.
			* It is possible to change pConnReq->filteringFlag and pConnReq->remoteAddress
			* in this handler. The changes will be applied to connection.
			* @param id Unique connection identifier
			* @param pConnInfo Connection parameters, see <tt>NF_UDP_CONN_REQUEST</tt>
			**/
			virtual void udpConnectRequest(ENDPOINT_ID id, PNF_UDP_CONN_REQUEST pConnReq) = 0;

			/**
			* Called after closing UDP socket identified by id.
			* @param id Unique socket identifier
			* @param pConnInfo Socket parameters, see <tt>NF_UDP_CONN_INFO</tt>
			**/
			virtual void udpClosed(ENDPOINT_ID id, PNF_UDP_CONN_INFO pConnInfo) = 0;

			/**
			* Indicates the buffer received from server.
			* @param id Unique socket identifier
			* @param options UDP options
			* @param remoteAddress Source address
			* @param buf Pointer to data buffer
			* @param len Buffer length
			**/
			virtual void udpReceive(ENDPOINT_ID id, const unsigned char * remoteAddress, const char * buf, int len, PNF_UDP_OPTIONS options) = 0;

			/**
			* Indicates the buffer sent from the local socket.
			* @param id Unique socket identifier
			* @param options UDP options
			* @param remoteAddress Destination address
			* @param buf Pointer to data buffer
			* @param len Buffer length
			**/
			virtual void udpSend(ENDPOINT_ID id, const unsigned char * remoteAddress, const char * buf, int len, PNF_UDP_OPTIONS options) = 0;

			/**
			* Informs that the internal buffer for receives is empty and
			* it is possible to call nf_udpPostReceive for pushing receives
			* via specified socket.
			* @param id Unique socket identifier
			**/
			virtual void udpCanReceive(ENDPOINT_ID id) = 0;

			/**
			* Informs that the internal buffer for sends is empty and
			* it is possible to call nf_udpPostSend for pushing sends
			* via specified socket.
			* @param id Unique socket identifier
			**/
			virtual void udpCanSend(ENDPOINT_ID id) = 0;
		};

		/**
		*	IP level filtering events
		**/
		class NF_IPEventHandler
		{
		public:
			/**
			* Indicates a packet received from server.
			* @param buf Pointer to data buffer
			* @param len Buffer length
			* @param options IP options
			**/
			virtual void ipReceive(const char * buf, int len, PNF_IP_PACKET_OPTIONS options) = 0;

			/**
			* Indicates a packet sent to server.
			* @param buf Pointer to data buffer
			* @param len Buffer length
			* @param options IP options
			**/
			virtual void ipSend(const char * buf, int len, PNF_IP_PACKET_OPTIONS options) = 0;
		};

#else 

#ifdef WIN32
	#define NFAPI_CC __cdecl
#else
	#define NFAPI_CC 
#endif
	#define NFAPI_NS

	/////////////////////////////////////////////////////////////////////////////////////
	// C API
	/////////////////////////////////////////////////////////////////////////////////////

	#ifdef __cplusplus
	extern "C" 
	{
	#endif

	#include <nfdriver.h>

	#pragma pack(push, 1)

	// C analogue of the class NF_EventHandler (see the definition above)
	typedef struct _NF_EventHandler
	{
		 void (NFAPI_CC *threadStart)();
		 void (NFAPI_CC *threadEnd)();
		 void (NFAPI_CC *tcpConnectRequest)(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo);
		 void (NFAPI_CC *tcpConnected)(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo);
		 void (NFAPI_CC *tcpClosed)(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo);
		 void (NFAPI_CC *tcpReceive)(ENDPOINT_ID id, const char * buf, int len);
		 void (NFAPI_CC *tcpSend)(ENDPOINT_ID id, const char * buf, int len);
		 void (NFAPI_CC *tcpCanReceive)(ENDPOINT_ID id);
		 void (NFAPI_CC *tcpCanSend)(ENDPOINT_ID id);
		 void (NFAPI_CC *udpCreated)(ENDPOINT_ID id, PNF_UDP_CONN_INFO pConnInfo);
		 void (NFAPI_CC *udpConnectRequest)(ENDPOINT_ID id, PNF_UDP_CONN_REQUEST pConnReq);
		 void (NFAPI_CC *udpClosed)(ENDPOINT_ID id, PNF_UDP_CONN_INFO pConnInfo);
		 void (NFAPI_CC *udpReceive)(ENDPOINT_ID id, const unsigned char * remoteAddress, const char * buf, int len, PNF_UDP_OPTIONS options);
		 void (NFAPI_CC *udpSend)(ENDPOINT_ID id, const unsigned char * remoteAddress, const char * buf, int len, PNF_UDP_OPTIONS options);
		 void (NFAPI_CC *udpCanReceive)(ENDPOINT_ID id);
		 void (NFAPI_CC *udpCanSend)(ENDPOINT_ID id);
	} NF_EventHandler, *PNF_EventHandler;

	// C analogue of the class NF_IPEventHandler (see the definition above)
	typedef struct _NF_IPEventHandler
	{
		 void (NFAPI_CC *ipReceive)(const char * buf, int len, PNF_IP_PACKET_OPTIONS options);
		 void (NFAPI_CC *ipSend)(const char * buf, int len, PNF_IP_PACKET_OPTIONS options);
	} NF_IPEventHandler, *PNF_IPEventHandler;

	#pragma pack(pop)

#endif


#ifdef __cplusplus
}
#endif

#endif
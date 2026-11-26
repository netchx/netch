//
// 	NetFilterSDK 
// 	Copyright (C) Vitaly Sidorov
//	All rights reserved.
//
//	This file is a part of the NetFilter SDK.
//	The code and information is provided "as-is" without
//	warranty of any kind, either expressed or implied.
//


#ifndef _NFAPI_H
#define _NFAPI_H

#include "nfevents.h"

#ifdef _NFAPI_STATIC_LIB
	#define NFAPI_API
#else
	#ifdef NFAPI_EXPORTS
	#define NFAPI_API __declspec(dllexport) 
	#else
	#define NFAPI_API __declspec(dllimport) 
	#endif
#endif

// Flags for NF_UDP_OPTIONS.flags

#define TDI_RECEIVE_BROADCAST           0x00000004 // received TSDU was broadcast.
#define TDI_RECEIVE_MULTICAST           0x00000008 // received TSDU was multicast.
#define TDI_RECEIVE_PARTIAL             0x00000010 // received TSDU is not fully presented.
#define TDI_RECEIVE_NORMAL              0x00000020 // received TSDU is normal data
#define TDI_RECEIVE_EXPEDITED           0x00000040 // received TSDU is expedited data
#define TDI_RECEIVE_PEEK                0x00000080 // received TSDU is not released
#define TDI_RECEIVE_NO_RESPONSE_EXP     0x00000100 // HINT: no back-traffic expected
#define TDI_RECEIVE_COPY_LOOKAHEAD      0x00000200 // for kernel-mode indications
#define TDI_RECEIVE_ENTIRE_MESSAGE      0x00000400 // opposite of RECEIVE_PARTIAL
                                                   //  (for kernel-mode indications)
#define TDI_RECEIVE_AT_DISPATCH_LEVEL   0x00000800 // receive indication called
                                                   //  at dispatch level
#define TDI_RECEIVE_CONTROL_INFO        0x00001000 // Control info is being passed up.
#define TDI_RECEIVE_FORCE_INDICATION    0x00002000 // reindicate rejected data.
#define TDI_RECEIVE_NO_PUSH             0x00004000 // complete only when full.

typedef enum _NF_FLAGS
{
	NFF_NONE		= 0,
	NFF_DONT_DISABLE_TEREDO	= 1,
	NFF_DONT_DISABLE_TCP_OFFLOADING	= 2,
	NFF_DISABLE_AUTO_REGISTER	= 4,
	NFF_DISABLE_AUTO_START	= 8,
} NF_FLAGS;

#ifndef _C_API
	namespace nfapi
	{
		#define NFAPI_NS	nfapi::
		#define NFAPI_CC	
#else // _C_API
	#define NFAPI_CC __cdecl
	#define NFAPI_NS
	#ifdef __cplusplus
	extern "C" 
	{
	#endif
#endif // _C_API

/**
* Initializes the internal data structures and starts the filtering thread.
* @param driverName The name of hooking driver, without ".sys" extension.
* @param pHandler Pointer to event handling object
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_init(const char * driverName, NF_EventHandler * pHandler);

/**
* Stops the filtering thread, breaks all filtered connections and closes
* a connection with the hooking driver.
**/
NFAPI_API void NFAPI_CC 
nf_free();

/**
* Registers and starts a driver with specified name (without ".sys" extension)
* @param driverName 
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_registerDriver(const char * driverName);

/**
* Registers and starts a driver with specified name (without ".sys" extension) and path to driver folder
* @param driverName 
* @param driverPath 
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_registerDriverEx(const char * driverName, const char * driverPath);

/**
* Unregisters a driver with specified name (without ".sys" extension)
* @param driverName 
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_unRegisterDriver(const char * driverName);


//
// TCP control routines
//

/**
* Suspends or resumes indicating of sends and receives for specified connection.
* @param id Connection identifier
* @param suspended TRUE(1) for suspend, FALSE(0) for resume 
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_tcpSetConnectionState(ENDPOINT_ID id, int suspended);

/**
* Sends the buffer to remote server via specified connection.
* @param id Connection identifier
* @param buf Pointer to data buffer
* @param len Buffer length
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_tcpPostSend(ENDPOINT_ID id, const char * buf, int len);

/**
* Indicates the buffer to local process via specified connection.
* @param id Unique connection identifier
* @param buf Pointer to data buffer
* @param len Buffer length
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_tcpPostReceive(ENDPOINT_ID id, const char * buf, int len);

/**
* Breaks the connection with given id.
* @param id Connection identifier
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_tcpClose(ENDPOINT_ID id);

/**
 *	Sets the timeout for TCP connections and returns old timeout.
 *	@param timeout Timeout value in milliseconds. Specify zero value to disable timeouts.
 */
NFAPI_API unsigned long NFAPI_CC 
nf_setTCPTimeout(unsigned long timeout);

/**
 *	Disables indicating TCP packets to user mode for the specified endpoint
 *  @param id Socket identifier
 */
NFAPI_API NF_STATUS NFAPI_CC 
nf_tcpDisableFiltering(ENDPOINT_ID id);


//
// UDP control routines
//

/**
* Suspends or resumes indicating of sends and receives for specified socket.
* @param id Socket identifier
* @param suspended TRUE(1) for suspend, FALSE(0) for resume 
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_udpSetConnectionState(ENDPOINT_ID id, int suspended);

/**
* Sends the buffer to remote server via specified socket.
* @param id Socket identifier
* @param options UDP options
* @param remoteAddress Destination address
* @param buf Pointer to data buffer
* @param len Buffer length
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_udpPostSend(ENDPOINT_ID id, const unsigned char * remoteAddress, const char * buf, int len, PNF_UDP_OPTIONS options);

/**
* Indicates the buffer to local process via specified socket.
* @param id Unique connection identifier
* @param options UDP options
* @param remoteAddress Source address
* @param buf Pointer to data buffer
* @param len Buffer length
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_udpPostReceive(ENDPOINT_ID id, const unsigned char * remoteAddress, const char * buf, int len, PNF_UDP_OPTIONS options);

/**
 *	Disables indicating UDP packets to user mode for the specified endpoint
 *  @param id Socket identifier
 */
NFAPI_API NF_STATUS NFAPI_CC 
nf_udpDisableFiltering(ENDPOINT_ID id);


/**
* Sends a packet to remote IP
* @param buf Pointer to IP packet
* @param len Buffer length
* @param options IP options
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_ipPostSend(const char * buf, int len, PNF_IP_PACKET_OPTIONS options);

/**
* Indicates a packet to TCP/IP stack
* @param buf Pointer to IP packet
* @param len Buffer length
* @param options IP options
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_ipPostReceive(const char * buf, int len, PNF_IP_PACKET_OPTIONS options);

//
// Filtering rules 
//

/**
* Add a rule to the head of rules list in driver.
* @param pRule See <tt>NF_RULE</tt>
* @param toHead TRUE (1) - add rule to list head, FALSE (0) - add rule to tail
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_addRule(PNF_RULE pRule, int toHead);

/**
* Removes all rules from driver.
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_deleteRules();

/**
* Replace the rules in driver with the specified array.
* @param pRules Array of <tt>NF_RULE</tt> structures
* @param count Number of items in array
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_setRules(PNF_RULE pRules, int count);

/**
* Add a rule to the head of rules list in driver.
* @param pRule See <tt>NF_RULE_EX</tt>
* @param toHead TRUE (1) - add rule to list head, FALSE (0) - add rule to tail
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_addRuleEx(PNF_RULE_EX pRule, int toHead);

/**
* Replace the rules in driver with the specified array.
* @param pRules Array of <tt>NF_RULE_EX</tt> structures
* @param count Number of items in array
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_setRulesEx(PNF_RULE_EX pRules, int count);

//
// Debug routine
//

NFAPI_API unsigned long NFAPI_CC 
nf_getConnCount();

NFAPI_API NF_STATUS NFAPI_CC 
nf_tcpSetSockOpt(ENDPOINT_ID id, int optname, const char* optval, int optlen);

/**
* Returns the process name for given process id
* @param processId Process identifier
* @param buf Buffer
* @param len Buffer length
**/
NFAPI_API BOOL NFAPI_CC 
nf_getProcessNameA(DWORD processId, char * buf, DWORD len);

NFAPI_API BOOL NFAPI_CC 
nf_getProcessNameW(DWORD processId, wchar_t * buf, DWORD len);

#ifdef UNICODE
#define nf_getProcessName nf_getProcessNameW
#else
#define nf_getProcessName nf_getProcessNameA
#endif 

NFAPI_API BOOL NFAPI_CC 
nf_getProcessNameFromKernel(DWORD processId, wchar_t * buf, DWORD len);

/**
*	Allows the current process to see the names of all processes in system
**/
NFAPI_API void NFAPI_CC 
nf_adjustProcessPriviledges();

/**
* Returns TRUE if the specified process acts as a local proxy, accepting the redirected TCP connections.
**/
NFAPI_API BOOL NFAPI_CC 
nf_tcpIsProxy(DWORD processId);

/**
* Set the number of worker threads and initialization flags.
* The function should be called before nf_init. 
* By default nThreads = 1 and flags = 0
* @param nThreads Number of worker threads for NF_EventHandler events 
* @param flags A combination of flags from <tt>NF_FLAGS</tt>
**/
NFAPI_API void NFAPI_CC 
nf_setOptions(DWORD nThreads, DWORD flags);

/**
* Complete TCP connect request pended using flag NF_PEND_CONNECT_REQUEST.
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_completeTCPConnectRequest(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo);

/**
* Complete UDP connect request pended using flag NF_PEND_CONNECT_REQUEST.
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_completeUDPConnectRequest(ENDPOINT_ID id, PNF_UDP_CONN_REQUEST pConnInfo);

/**
* Returns in pConnInfo the properties of TCP connection with specified id.
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_getTCPConnInfo(ENDPOINT_ID id, PNF_TCP_CONN_INFO pConnInfo);

/**
* Returns in pConnInfo the properties of UDP socket with specified id.
**/
NFAPI_API NF_STATUS NFAPI_CC 
nf_getUDPConnInfo(ENDPOINT_ID id, PNF_UDP_CONN_INFO pConnInfo);

/**
* Set the event handler for IP filtering events
*/
NFAPI_API void NFAPI_CC 
nf_setIPEventHandler(NF_IPEventHandler * pHandler);

/**
* Add flow control context
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_addFlowCtl(PNF_FLOWCTL_DATA pData, unsigned int * pFcHandle);

/**
* Delete flow control context
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_deleteFlowCtl(unsigned int fcHandle);

/**
* Associate flow control context with TCP connection
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_setTCPFlowCtl(ENDPOINT_ID id, unsigned int fcHandle);

/**
* Associate flow control context with UDP socket
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_setUDPFlowCtl(ENDPOINT_ID id, unsigned int fcHandle);

/**
* Modify flow control context limits
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_modifyFlowCtl(unsigned int fcHandle, PNF_FLOWCTL_DATA pData);

/**
* Get flow control context statistics as the numbers of in/out bytes
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_getFlowCtlStat(unsigned int fcHandle, PNF_FLOWCTL_STAT pStat);

/**
* Get TCP connection statistics as the numbers of in/out bytes.
* The function can be called only from tcpClosed handler!
*/
NFAPI_API NF_STATUS NFAPI_CC 
nf_getTCPStat(ENDPOINT_ID id, PNF_FLOWCTL_STAT pStat);

/**
* Get UDP socket statistics as the numbers of in/out bytes.
* The function can be called only from udpClosed handler!
*/
NFAPI_API NF_STATUS NFAPI_CC 
nf_getUDPStat(ENDPOINT_ID id, PNF_FLOWCTL_STAT pStat);

/**
* Add binding rule to driver
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_addBindingRule(PNF_BINDING_RULE pRule, int toHead);

/**
* Delete all binding rules from driver
*/
NFAPI_API NF_STATUS NFAPI_CC
nf_deleteBindingRules();

/**
* Returns the type of attached driver (DT_WFP, DT_TDI or DT_UNKNOWN)
*/
NFAPI_API unsigned long NFAPI_CC 
nf_getDriverType();

#ifdef __cplusplus
}
#endif

#endif
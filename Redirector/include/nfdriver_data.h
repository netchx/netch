//
// 	NetFilterSDK 
// 	Copyright (C) Vitaly Sidorov
//	All rights reserved.
//
//	This file is a part of the NetFilter SDK.
//	The code and information is provided "as-is" without
//	warranty of any kind, either expressed or implied.
//


#ifndef _NFDRIVER_DATA_H
#define _NFDRIVER_DATA_H

#define NF_TCP_PACKET_BUF_SIZE 8192
#define NF_UDP_PACKET_BUF_SIZE 2 * 65536

typedef enum _NF_DIRECTION
{
	NF_D_IN = 1,		// Incoming TCP connection or UDP packet
	NF_D_OUT = 2,		// Outgoing TCP connection or UDP packet
	NF_D_BOTH = 3		// Any direction
} NF_DIRECTION;

typedef enum _NF_FILTERING_FLAG
{
	NF_ALLOW = 0,		// Allow the activity without filtering transmitted packets
	NF_BLOCK = 1,		// Block the activity
	NF_FILTER = 2,		// Filter the transmitted packets
	NF_SUSPENDED = 4,	// Suspend receives from server and sends from client
	NF_OFFLINE = 8,		// Emulate establishing a TCP connection with remote server
	NF_INDICATE_CONNECT_REQUESTS = 16, // Indicate outgoing connect requests to API
	NF_DISABLE_REDIRECT_PROTECTION = 32, // Disable blocking indicating connect requests for outgoing connections of local proxies
	NF_PEND_CONNECT_REQUEST = 64,	// Pend outgoing connect request to complete it later using nf_complete(TCP|UDP)ConnectRequest
	NF_FILTER_AS_IP_PACKETS = 128,	// Indicate the traffic as IP packets via ipSend/ipReceive
	NF_READONLY = 256,				// Don't block the IP packets and indicate them to ipSend/ipReceive only for monitoring
	NF_CONTROL_FLOW = 512,			// Use the flow limit rules even without NF_FILTER flag
	NF_REDIRECT = 1024,			// Redirect the outgoing TCP connections to address specified in redirectTo
	NF_BYPASS_IP_PACKETS = 2048,	// Bypass the traffic as IP packets, when used with NF_FILTER_AS_IP_PACKETS flag
} NF_FILTERING_FLAG;

#pragma pack(push, 1)

#define NF_MAX_ADDRESS_LENGTH		28
#define NF_MAX_IP_ADDRESS_LENGTH	16

#ifndef AF_INET
#define AF_INET         2               /* internetwork: UDP, TCP, etc. */
#endif

#if !defined(AF_INET6) && defined(_WIN32)
#define AF_INET6        23          
#endif

// Protocols

#ifndef IPPROTO_TCP
#define IPPROTO_TCP 6
#endif

#ifndef IPPROTO_UDP
#define IPPROTO_UDP 17
#endif

/**
*	Filtering rule
**/
typedef UNALIGNED struct _NF_RULE
{
    int				protocol;	// IPPROTO_TCP or IPPROTO_UDP        
	unsigned long	processId;	// Process identifier
	unsigned char	direction;	// See NF_DIRECTION
	unsigned short	localPort;	// Local port
	unsigned short	remotePort;	// Remote port
	unsigned short	ip_family;	// AF_INET for IPv4 and AF_INET6 for IPv6
	
	// Local IP (or network if localIpAddressMask is not zero)
	unsigned char	localIpAddress[NF_MAX_IP_ADDRESS_LENGTH];	
	
	// Local IP mask
	unsigned char	localIpAddressMask[NF_MAX_IP_ADDRESS_LENGTH]; 
	
	// Remote IP (or network if remoteIpAddressMask is not zero)
	unsigned char	remoteIpAddress[NF_MAX_IP_ADDRESS_LENGTH]; 
	
	// Remote IP mask
	unsigned char	remoteIpAddressMask[NF_MAX_IP_ADDRESS_LENGTH]; 

	unsigned long	filteringFlag;	// See NF_FILTERING_FLAG
} NF_RULE, *PNF_RULE;


typedef struct _NF_PORT_RANGE
{
    unsigned short valueLow;
    unsigned short valueHigh;
} NF_PORT_RANGE, *PNF_PORT_RANGE;


/**
*	Filtering rule with additional fields
**/
typedef UNALIGNED struct _NF_RULE_EX
{
    int				protocol;	// IPPROTO_TCP or IPPROTO_UDP        
	unsigned long	processId;	// Process identifier
	unsigned char	direction;	// See NF_DIRECTION
	unsigned short	localPort;	// Local port
	unsigned short	remotePort;	// Remote port
	unsigned short	ip_family;	// AF_INET for IPv4 and AF_INET6 for IPv6
	
	// Local IP (or network if localIpAddressMask is not zero)
	unsigned char	localIpAddress[NF_MAX_IP_ADDRESS_LENGTH];	
	
	// Local IP mask
	unsigned char	localIpAddressMask[NF_MAX_IP_ADDRESS_LENGTH]; 
	
	// Remote IP (or network if remoteIpAddressMask is not zero)
	unsigned char	remoteIpAddress[NF_MAX_IP_ADDRESS_LENGTH]; 
	
	// Remote IP mask
	unsigned char	remoteIpAddressMask[NF_MAX_IP_ADDRESS_LENGTH]; 

	unsigned long	filteringFlag;	// See NF_FILTERING_FLAG

	// Process name tail mask (supports * as 0 or more symbols)
	wchar_t			processName[_MAX_PATH];

	NF_PORT_RANGE	localPortRange; // Local port(s)
	NF_PORT_RANGE	remotePortRange; // Remote port(s)

	// Remote address for redirection as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
	unsigned char	redirectTo[NF_MAX_ADDRESS_LENGTH];
	// Process identifier of a local proxy
	unsigned long	localProxyProcessId;	

} NF_RULE_EX, *PNF_RULE_EX;

typedef unsigned __int64 ENDPOINT_ID;


/**
*	TCP connection properties
**/
typedef UNALIGNED struct _NF_TCP_CONN_INFO
{
	unsigned long	filteringFlag;	// See NF_FILTERING_FLAG
	unsigned long	processId;		// Process identifier
	unsigned char	direction;		// See NF_DIRECTION
	unsigned short	ip_family;		// AF_INET for IPv4 and AF_INET6 for IPv6
	
	// Local address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
	unsigned char	localAddress[NF_MAX_ADDRESS_LENGTH]; 
	
	// Remote address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
	unsigned char	remoteAddress[NF_MAX_ADDRESS_LENGTH];

} NF_TCP_CONN_INFO, *PNF_TCP_CONN_INFO;

/**
*	UDP endpoint properties
**/
typedef UNALIGNED struct _NF_UDP_CONN_INFO
{
	unsigned long	processId;		// Process identifier
	unsigned short	ip_family;		// AF_INET for IPv4 and AF_INET6 for IPv6
	
	// Local address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
	unsigned char	localAddress[NF_MAX_ADDRESS_LENGTH]; 

} NF_UDP_CONN_INFO, *PNF_UDP_CONN_INFO;

/**
*	UDP TDI_CONNECT request properties
**/
typedef UNALIGNED struct _NF_UDP_CONN_REQUEST
{
	unsigned long	filteringFlag;	// See NF_FILTERING_FLAG
	unsigned long	processId;		// Process identifier
	unsigned short	ip_family;		// AF_INET for IPv4 and AF_INET6 for IPv6
	
	// Local address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
	unsigned char	localAddress[NF_MAX_ADDRESS_LENGTH]; 

	// Remote address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
	unsigned char	remoteAddress[NF_MAX_ADDRESS_LENGTH];

} NF_UDP_CONN_REQUEST, *PNF_UDP_CONN_REQUEST;

/**
*	UDP options
**/
typedef UNALIGNED struct _NF_UDP_OPTIONS
{
	unsigned long	flags;		// Datagram flags
	long			optionsLength;	// Length of options buffer
	unsigned char	options[1]; // Options of variable size
} NF_UDP_OPTIONS, *PNF_UDP_OPTIONS;

typedef enum _NF_IP_FLAG
{
	NFIF_NONE = 0,		// No flags
	NFIF_READONLY = 1,	// The packet was not blocked and indicated only for monitoring in read-only mode 
						// (see NF_READ_ONLY flags from NF_FILTERING_FLAG).
} NF_IP_FLAG;

/**
*	IP options
**/
typedef struct _NF_IP_PACKET_OPTIONS
{	
	unsigned short	ip_family;			// AF_INET for IPv4 and AF_INET6 for IPv6
	unsigned int	ipHeaderSize;   	// Size in bytes of IP header
	unsigned long	compartmentId;		// Network routing compartment identifier (can be zero)
	unsigned long	interfaceIndex;   	// Index of the interface on which the original packet data was received (irrelevant to outgoing packets)
	unsigned long	subInterfaceIndex;  // Index of the subinterface on which the original packet data was received (irrelevant to outgoing packets)
	unsigned long	flags;				// Can be a combination of flags from NF_IP_FLAG enumeration
} NF_IP_PACKET_OPTIONS, *PNF_IP_PACKET_OPTIONS;

/**
*	Internal IO structure
**/
typedef UNALIGNED struct _NF_DATA
{
	int				code;
	ENDPOINT_ID		id;
	unsigned long	bufferSize;
	char 			buffer[1];
} NF_DATA, *PNF_DATA;

typedef UNALIGNED struct _NF_BUFFERS
{
    unsigned __int64 inBuf;
    unsigned __int64 inBufLen;
    unsigned __int64 outBuf;
    unsigned __int64 outBufLen;
} NF_BUFFERS, *PNF_BUFFERS;

typedef UNALIGNED struct _NF_READ_RESULT
{
    unsigned __int64 length;
} NF_READ_RESULT, *PNF_READ_RESULT;

typedef UNALIGNED struct _NF_FLOWCTL_DATA
{
    unsigned __int64 inLimit;
    unsigned __int64 outLimit;
} NF_FLOWCTL_DATA, *PNF_FLOWCTL_DATA;

typedef UNALIGNED struct _NF_FLOWCTL_MODIFY_DATA
{
    unsigned int fcHandle;
    NF_FLOWCTL_DATA	data;
} NF_FLOWCTL_MODIFY_DATA, *PNF_FLOWCTL_MODIFY_DATA;

typedef UNALIGNED struct _NF_FLOWCTL_STAT
{
    unsigned __int64 inBytes;
    unsigned __int64 outBytes;
} NF_FLOWCTL_STAT, *PNF_FLOWCTL_STAT;

typedef UNALIGNED struct _NF_FLOWCTL_SET_DATA
{
    unsigned __int64 endpointId;
    unsigned int fcHandle;
} NF_FLOWCTL_SET_DATA, *PNF_FLOWCTL_SET_DATA;


/**
*	Binding rule
**/
typedef UNALIGNED struct _NF_BINDING_RULE
{
    int				protocol;	// IPPROTO_TCP or IPPROTO_UDP        

	unsigned long	processId;	// Process identifier

	// Process name tail mask (supports * as 0 or more symbols)
	wchar_t			processName[_MAX_PATH];

	unsigned short	localPort;	// Local port

	unsigned short	ip_family;	// AF_INET for IPv4 and AF_INET6 for IPv6
	
	// Local IP (or network if localIpAddressMask is not zero)
	unsigned char	localIpAddress[NF_MAX_IP_ADDRESS_LENGTH];	
	
	// Local IP mask
	unsigned char	localIpAddressMask[NF_MAX_IP_ADDRESS_LENGTH]; 
	
	// Redirect bind request to this IP 
	unsigned char	newLocalIpAddress[NF_MAX_IP_ADDRESS_LENGTH]; 

	// Redirect bind request to this port, if it is not zero
	unsigned short	newLocalPort;

	unsigned long	filteringFlag;	// See NF_FILTERING_FLAG, NF_ALLOW or NF_FILTER

} NF_BINDING_RULE, *PNF_BINDING_RULE;


#pragma pack(pop)

#ifdef WIN32

typedef enum _NF_DRIVER_TYPE
{
	DT_UNKNOWN = 0,
	DT_TDI = 1,
	DT_WFP = 2,
	DT_SOCK = 3,
} NF_DRIVER_TYPE;

#endif

#endif // _NFDRIVER_DATA_H
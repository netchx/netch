#nullable disable
using System;
using System.Runtime.InteropServices;

namespace nfapinet
{
    public enum NF_STATUS
    {
        NF_STATUS_SUCCESS = 0,
        NF_STATUS_FAIL = -1,
        NF_STATUS_INVALID_ENDPOINT_ID = -2,
        NF_STATUS_NOT_INITIALIZED = -3,
        NF_STATUS_IO_ERROR = -4
    };

    public enum NF_DIRECTION
    {
        NF_D_IN = 1,  // Incoming TCP connection or UDP packet
        NF_D_OUT = 2, // Outgoing TCP connection or UDP packet
        NF_D_BOTH = 3 // Any direction
    };

    public enum NF_FILTERING_FLAG
    {
        NF_ALLOW = 0,                        // Allow the activity without filtering transmitted packets
        NF_BLOCK = 1,                        // Block the activity
        NF_FILTER = 2,                       // Filter the transmitted packets
        NF_SUSPENDED = 4,                    // Suspend receives from server and sends from client
        NF_OFFLINE = 8,                      // Emulate establishing a TCP connection with remote server
        NF_INDICATE_CONNECT_REQUESTS = 16,   // Indicate outgoing connect requests to API
        NF_DISABLE_REDIRECT_PROTECTION = 32, // Disable blocking indicating connect requests for outgoing connections of local proxies
        NF_PEND_CONNECT_REQUEST = 64,        // Pend outgoing connect request to complete it later using nf_complete(TCP|UDP)ConnectRequest
        NF_FILTER_AS_IP_PACKETS = 128,       // Indicate the traffic as IP packets via ipSend/ipReceive
        NF_READONLY = 256,                   // Don't block the IP packets and indicate them to ipSend/ipReceive only for monitoring
        NF_CONTROL_FLOW = 512,               // Use the flow limit rules even without NF_FILTER flag
    };

    public enum NF_FLAGS
    {
        NFF_NONE = 0,
        NFF_DONT_DISABLE_TEREDO = 1,        // Don't disable Teredo 
        NFF_DONT_DISABLE_TCP_OFFLOADING = 2 // Don't disable TCP offloading
    };

    public enum NF_CONSTS
    {
        NF_MAX_ADDRESS_LENGTH = 28,
        NF_MAX_IP_ADDRESS_LENGTH = 16
    };

    public enum NF_DRIVER_TYPE
    {
        DT_UNKNOWN = 0,
        DT_TDI = 1,
        DT_WFP = 2
    };

    /**
	*	Filtering rule
	**/
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_RULE
    {
        public int protocol;      // IPPROTO_TCP or IPPROTO_UDP        
        public UInt32 processId;  // Process identifier
        public Byte direction;    // See NF_DIRECTION
        public ushort localPort;  // Local port
        public ushort remotePort; // Remote port
        public ushort ip_family;  // AF_INET for IPv4 and AF_INET6 for IPv6

        // Local IP (or network if localIpAddressMask is not zero)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] localIpAddress;

        // Local IP mask
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] localIpAddressMask;

        // Remote IP (or network if remoteIpAddressMask is not zero)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] remoteIpAddress;

        // Remote IP mask
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] remoteIpAddressMask;

        public UInt32 filteringFlag; // See NF_FILTERING_FLAG
    };

    /**
    *	Filtering rule with additional fields
    **/
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct NF_RULE_EX
    {
        public int protocol;      // IPPROTO_TCP or IPPROTO_UDP        
        public UInt32 processId;  // Process identifier
        public Byte direction;    // See NF_DIRECTION
        public ushort localPort;  // Local port
        public ushort remotePort; // Remote port
        public ushort ip_family;  // AF_INET for IPv4 and AF_INET6 for IPv6

        // Local IP (or network if localIpAddressMask is not zero)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] localIpAddress;

        // Local IP mask
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] localIpAddressMask;

        // Remote IP (or network if remoteIpAddressMask is not zero)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] remoteIpAddress;

        // Remote IP mask
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] remoteIpAddressMask;

        public UInt32 filteringFlag; // See NF_FILTERING_FLAG

        // Tail part of the process path mask 
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string processName;
    };

    /**
	*	TCP connection properties
	**/
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_TCP_CONN_INFO
    {
        public UInt32 filteringFlag; // See NF_FILTERING_FLAG
        public UInt32 processId;     // Process identifier
        public Byte direction;       // See NF_DIRECTION
        public ushort ip_family;     // AF_INET for IPv4 and AF_INET6 for IPv6

        // Local address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_ADDRESS_LENGTH)]
        public byte[] localAddress;

        // Remote address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_ADDRESS_LENGTH)]
        public byte[] remoteAddress;
    };

    /**
	*	UDP endpoint properties
	**/
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_UDP_CONN_INFO
    {
        public UInt32 processId; // Process identifier
        public ushort ip_family; // AF_INET for IPv4 and AF_INET6 for IPv6

        // Local address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_ADDRESS_LENGTH)]
        public byte[] localAddress;
    };

    /**
    *	UDP options
    **/
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_UDP_OPTIONS
    {
        public UInt32 flags;        // UDP flags
        public Int32 optionsLength; // options length
        [MarshalAs(UnmanagedType.ByValArray)]
        public byte[] options; // Options array
    };

    /**
	*	UDP connect request properties
	**/
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_UDP_CONN_REQUEST
    {
        public UInt32 filteringFlag; // See NF_FILTERING_FLAG
        public UInt32 processId;     // Process identifier
        public ushort ip_family;     // AF_INET for IPv4 and AF_INET6 for IPv6

        // Local address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_ADDRESS_LENGTH)]
        public byte[] localAddress;

        // Remote address as sockaddr_in for IPv4 and sockaddr_in6 for IPv6
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_ADDRESS_LENGTH)]
        public byte[] remoteAddress;
    };

    public enum NF_IP_FLAG
    {
        NFIF_NONE = 0,     // No flags
        NFIF_READONLY = 1, // The packet was not blocked and indicated only for monitoring in read-only mode 
        // (see NF_READ_ONLY flags from NF_FILTERING_FLAG).
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_IP_PACKET_OPTIONS
    {
        public ushort ip_family;         // AF_INET for IPv4 and AF_INET6 for IPv6
        public UInt32 ipHeaderSize;      // Size in bytes of IP header
        public UInt32 compartmentId;     // Network routing compartment identifier (can be zero)
        public UInt32 interfaceIndex;    // Index of the interface on which the original packet data was received (irrelevant to outgoing packets)
        public UInt32 subInterfaceIndex; // Index of the subinterface on which the original packet data was received (irrelevant to outgoing packets)
        public UInt32 flags;             // Can be a combination of flags from NF_IP_FLAG enumeration
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_FLOWCTL_DATA
    {
        public ulong inLimit;
        public ulong outLimit;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_FLOWCTL_MODIFY_DATA
    {
        public UInt32 fcHandle;
        public NF_FLOWCTL_DATA data;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_FLOWCTL_STAT
    {
        public ulong inBytes;
        public ulong outBytes;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NF_FLOWCTL_SET_DATA
    {
        public ulong endpointId;
        public UInt32 fcHandle;
    };

    /**
    *	Binding rule
    **/
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct NF_BINDING_RULE
    {
        // IPPROTO_TCP or IPPROTO_UDP
        public int protocol;

        // Process identifier
        public UInt32 processId;

        // Tail part of the process path mask 
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string processName;

        // Local port
        public ushort localPort;

        // AF_INET for IPv4 and AF_INET6 for IPv6
        public ushort ip_family;

        // Local IP (or network if localIpAddressMask is not zero)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] localIpAddress;

        // Local IP mask
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] localIpAddressMask;

        // Redirect bind request to this IP 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)]
        public byte[] newLocalIpAddress;

        // Redirect bind request to this port, if it is not zero
        public ushort newLocalPort;

        // See NF_FILTERING_FLAG, NF_ALLOW or NF_FILTER
        public ulong filteringFlag;
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_threadStart();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_threadEnd();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_tcpConnectRequest(ulong id, ref NF_TCP_CONN_INFO pConnInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_tcpConnected(ulong id, ref NF_TCP_CONN_INFO pConnInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_tcpClosed(ulong id, ref NF_TCP_CONN_INFO pConnInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_tcpReceive(ulong id, IntPtr buf, int len);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_tcpSend(ulong id, IntPtr buf, int len);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_tcpCanReceive(ulong id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_tcpCanSend(ulong id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_udpCreated(ulong id, ref NF_UDP_CONN_INFO pConnInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_udpConnectRequest(ulong id, ref NF_UDP_CONN_REQUEST pConnReq);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_udpClosed(ulong id, ref NF_UDP_CONN_INFO pConnInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_udpReceive(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_udpSend(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_udpCanReceive(ulong id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_udpCanSend(ulong id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_ipReceive(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS ipOptions);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void cbd_ipSend(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS ipOptions);

    /*
     * Event handler interface
     */
    public interface NF_EventHandler
    {
        void threadStart();
        void threadEnd();
        void tcpConnectRequest(ulong id, ref NF_TCP_CONN_INFO pConnInfo);
        void tcpConnected(ulong id, NF_TCP_CONN_INFO pConnInfo);
        void tcpClosed(ulong id, NF_TCP_CONN_INFO pConnInfo);
        void tcpReceive(ulong id, IntPtr buf, int len);
        void tcpSend(ulong id, IntPtr buf, int len);
        void tcpCanReceive(ulong id);
        void tcpCanSend(ulong id);
        void udpCreated(ulong id, NF_UDP_CONN_INFO pConnInfo);
        void udpConnectRequest(ulong id, ref NF_UDP_CONN_REQUEST pConnReq);
        void udpClosed(ulong id, NF_UDP_CONN_INFO pConnInfo);
        void udpReceive(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options, int optionsLen);
        void udpSend(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options, int optionsLen);
        void udpCanReceive(ulong id);
        void udpCanSend(ulong id);
    };

    /*
     * IP event handler interface
     */
    public interface NF_IPEventHandler
    {
        void ipReceive(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS ipOptions);
        void ipSend(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS ipOptions);
    };

    /*
     *  Internal events forwarder
     */
    class NF_EventHandlerFwd
    {
        public static NF_EventHandler m_pEventHandler = null;

        public static void threadStart()
        {
            m_pEventHandler.threadStart();
        }

        public static void threadEnd()
        {
            m_pEventHandler.threadEnd();
        }

        public static void tcpConnectRequest(ulong id, ref NF_TCP_CONN_INFO pConnInfo)
        {
            m_pEventHandler.tcpConnectRequest(id, ref pConnInfo);
        }

        public static void tcpConnected(ulong id, ref NF_TCP_CONN_INFO pConnInfo)
        {
            m_pEventHandler.tcpConnected(id, pConnInfo);
        }

        public static void tcpClosed(ulong id, ref NF_TCP_CONN_INFO pConnInfo)
        {
            m_pEventHandler.tcpClosed(id, pConnInfo);
        }

        public static void tcpReceive(ulong id, IntPtr buf, int len)
        {
            m_pEventHandler.tcpReceive(id, buf, len);
        }

        public static void tcpSend(ulong id, IntPtr buf, int len)
        {
            m_pEventHandler.tcpSend(id, buf, len);
        }

        public static void tcpCanReceive(ulong id)
        {
            m_pEventHandler.tcpCanReceive(id);
        }

        public static void tcpCanSend(ulong id)
        {
            m_pEventHandler.tcpCanSend(id);
        }

        public static void udpCreated(ulong id, ref NF_UDP_CONN_INFO pConnInfo)
        {
            m_pEventHandler.udpCreated(id, pConnInfo);
        }

        public static void udpConnectRequest(ulong id, ref NF_UDP_CONN_REQUEST pConnReq)
        {
            m_pEventHandler.udpConnectRequest(id, ref pConnReq);
        }

        public static void udpClosed(ulong id, ref NF_UDP_CONN_INFO pConnInfo)
        {
            m_pEventHandler.udpClosed(id, pConnInfo);
        }

        public static void udpReceive(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options)
        {
            if (options.ToInt64() != 0)
            {
                NF_UDP_OPTIONS optionsCopy = (NF_UDP_OPTIONS) Marshal.PtrToStructure((IntPtr) options, typeof(NF_UDP_OPTIONS));
                int optionsLen = 8 + optionsCopy.optionsLength;
                m_pEventHandler.udpReceive(id, remoteAddress, buf, len, options, optionsLen);
            }
            else
            {
                m_pEventHandler.udpReceive(id, remoteAddress, buf, len, (IntPtr) null, 0);
            }
        }

        public static void udpSend(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options)
        {
            if (options.ToInt64() != 0)
            {
                NF_UDP_OPTIONS optionsCopy = (NF_UDP_OPTIONS) Marshal.PtrToStructure((IntPtr) options, typeof(NF_UDP_OPTIONS));
                int optionsLen = 8 + optionsCopy.optionsLength;
                m_pEventHandler.udpSend(id, remoteAddress, buf, len, options, optionsLen);
            }
            else
            {
                m_pEventHandler.udpSend(id, remoteAddress, buf, len, (IntPtr) null, 0);
            }
        }

        public static void udpCanReceive(ulong id)
        {
            m_pEventHandler.udpCanReceive(id);
        }

        public static void udpCanSend(ulong id)
        {
            m_pEventHandler.udpCanSend(id);
        }
    };

    /*
     *  Internal IP events forwarder
     */
    class NF_IPEventHandlerFwd
    {
        public static NF_IPEventHandler m_pEventHandler = null;

        public static void ipReceive(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS ipOptions)
        {
            m_pEventHandler.ipReceive(buf, len, ref ipOptions);
        }

        public static void ipSend(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS ipOptions)
        {
            m_pEventHandler.ipSend(buf, len, ref ipOptions);
        }
    };

    /*
     *  Event handler structure for C API
     */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct NF_EventHandlerInternal
    {
        public cbd_threadStart threadStart;
        public cbd_threadEnd threadEnd;
        public cbd_tcpConnectRequest tcpConnectRequest;
        public cbd_tcpConnected tcpConnected;
        public cbd_tcpClosed tcpClosed;
        public cbd_tcpReceive tcpReceive;
        public cbd_tcpSend tcpSend;
        public cbd_tcpCanReceive tcpCanReceive;
        public cbd_tcpCanSend tcpCanSend;
        public cbd_udpCreated udpCreated;
        public cbd_udpConnectRequest udpConnectRequest;
        public cbd_udpClosed udpClosed;
        public cbd_udpReceive udpReceive;
        public cbd_udpSend udpSend;
        public cbd_udpCanReceive udpCanReceive;
        public cbd_udpCanSend udpCanSend;
    };

    /*
     *  IP event handler structure for C API
     */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct NF_IPEventHandlerInternal
    {
        public cbd_ipReceive ipReceive;
        public cbd_ipSend ipSend;
    };

    // Managed wrapper over API 
    public class NFAPI
    {
        private static IntPtr m_pEventHandlerRaw = (IntPtr) null;
        private static NF_EventHandlerInternal m_pEventHandler;
        private static IntPtr m_pIPEventHandlerRaw = (IntPtr) null;
        private static NF_IPEventHandlerInternal m_pIPEventHandler;

        /**
		* Initializes the internal data structures and starts the filtering thread.
		* @param driverName The name of hooking driver, without ".sys" extension.
		* @param pHandler Pointer to event handling object
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_init(String driverName, IntPtr pHandler);

        public static NF_STATUS nf_init(String driverName, NF_EventHandler pHandler)
        {
            NF_EventHandlerFwd.m_pEventHandler = pHandler;

            nf_adjustProcessPriviledges();

            m_pEventHandler = new NF_EventHandlerInternal();

            m_pEventHandler.threadStart = new cbd_threadStart(NF_EventHandlerFwd.threadStart);
            m_pEventHandler.threadEnd = new cbd_threadEnd(NF_EventHandlerFwd.threadEnd);
            m_pEventHandler.tcpConnectRequest = new cbd_tcpConnectRequest(NF_EventHandlerFwd.tcpConnectRequest);
            m_pEventHandler.tcpConnected = new cbd_tcpConnected(NF_EventHandlerFwd.tcpConnected);
            m_pEventHandler.tcpClosed = new cbd_tcpClosed(NF_EventHandlerFwd.tcpClosed);
            m_pEventHandler.tcpReceive = new cbd_tcpReceive(NF_EventHandlerFwd.tcpReceive);
            m_pEventHandler.tcpSend = new cbd_tcpSend(NF_EventHandlerFwd.tcpSend);
            m_pEventHandler.tcpCanReceive = new cbd_tcpCanReceive(NF_EventHandlerFwd.tcpCanReceive);
            m_pEventHandler.tcpCanSend = new cbd_tcpCanSend(NF_EventHandlerFwd.tcpCanSend);
            m_pEventHandler.udpCreated = new cbd_udpCreated(NF_EventHandlerFwd.udpCreated);
            m_pEventHandler.udpConnectRequest = new cbd_udpConnectRequest(NF_EventHandlerFwd.udpConnectRequest);
            m_pEventHandler.udpClosed = new cbd_udpClosed(NF_EventHandlerFwd.udpClosed);
            m_pEventHandler.udpReceive = new cbd_udpReceive(NF_EventHandlerFwd.udpReceive);
            m_pEventHandler.udpSend = new cbd_udpSend(NF_EventHandlerFwd.udpSend);
            m_pEventHandler.udpCanReceive = new cbd_udpCanReceive(NF_EventHandlerFwd.udpCanReceive);
            m_pEventHandler.udpCanSend = new cbd_udpCanSend(NF_EventHandlerFwd.udpCanSend);

            m_pEventHandlerRaw = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NF_EventHandlerInternal)));
            Marshal.StructureToPtr(m_pEventHandler, m_pEventHandlerRaw, true);

            return nf_init(driverName, m_pEventHandlerRaw);
        }

        /**
		* Stops the filtering thread, breaks all filtered connections and closes
		* a connection with the hooking driver.
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern void nf_free();

        /**
		* Registers and starts a driver with specified name (without ".sys" extension)
		* @param driverName 
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_registerDriver(String driverName);

        /**
		* Unregisters a driver with specified name (without ".sys" extension)
		* @param driverName 
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_unRegisterDriver(String driverName);

        //
        // TCP control routines
        //

        /**
		* Suspends or resumes indicating of sends and receives for specified connection.
		* @param id Connection identifier
		* @param suspended true for suspend, false for resume 
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_tcpSetConnectionState(ulong id, int suspended);

        /**
		* Sends the buffer to remote server via specified connection.
		* @param id Connection identifier
		* @param buf Pointer to data buffer
		* @param len Buffer length
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_tcpPostSend(ulong id, IntPtr buf, int len);

        /**
		* Indicates the buffer to local process via specified connection.
		* @param id Unique connection identifier
		* @param buf Pointer to data buffer
		* @param len Buffer length
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_tcpPostReceive(ulong id, IntPtr buf, int len);

        /**
		* Breaks the connection with given id.
		* @param id Connection identifier
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_tcpClose(ulong id);

        //
        // UDP control routines
        //

        /**
		* Suspends or resumes indicating of sends and receives for specified socket.
		* @param id Socket identifier
		* @param suspended true for suspend, false for resume 
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_udpSetConnectionState(ulong id, int suspended);

        /**
		* Sends the buffer to remote server via specified socket.
		* @param id Socket identifier
		* @param remoteAddress Destination address
		* @param buf Pointer to data buffer
		* @param len Buffer length
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_udpPostSend(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options);

        /**
        * Indicates the buffer to local process via specified socket.
        * @param id Unique connection identifier
        * @param remoteAddress Source address
        * @param buf Pointer to data buffer
        * @param len Buffer length
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_udpPostReceive(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options);

        /**
		* Indicates a packet to TCP/IP stack
		* @param buf Pointer to IP packet
		* @param len Buffer length
		* @param options IP options
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_ipPostReceive(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS options);

        /**
		* Sends a packet to remote IP
		* @param buf Pointer to IP packet
		* @param len Buffer length
		* @param options IP options
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_ipPostSend(IntPtr buf, int len, ref NF_IP_PACKET_OPTIONS options);

        //
        // Filtering rules 
        //

        /**
		* Add a rule to the head of rules list in driver.
		* @param pRule See <tt>NF_RULE</tt>
		* @param toHead TRUE (1) - add rule to list head, FALSE (0) - add rule to tail
		**/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern NF_STATUS nf_addRule(ref NF_RULE pRule, int toHead);

        private static void updateAddressLength(ref byte[] buf)
        {
            if (buf == null)
            {
                buf = new byte[(int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH];
            }
            else
            {
                if (buf.Length < (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH)
                {
                    Array.Resize(ref buf, (int) NF_CONSTS.NF_MAX_IP_ADDRESS_LENGTH);
                }
            }
        }

        public static NF_STATUS nf_addRule(NF_RULE pRule, int toHead)
        {
            updateAddressLength(ref pRule.localIpAddress);
            updateAddressLength(ref pRule.localIpAddressMask);
            updateAddressLength(ref pRule.remoteIpAddress);
            updateAddressLength(ref pRule.remoteIpAddressMask);

            return nf_addRule(ref pRule, toHead);
        }

        /**
        * Add a rule to the head of rules list in driver.
        * @param pRule See <tt>NF_RULE</tt>
        * @param toHead TRUE (1) - add rule to list head, FALSE (0) - add rule to tail
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern NF_STATUS nf_addRuleEx(ref NF_RULE_EX pRule, int toHead);

        public static NF_STATUS nf_addRuleEx(NF_RULE_EX pRule, int toHead)
        {
            updateAddressLength(ref pRule.localIpAddress);
            updateAddressLength(ref pRule.localIpAddressMask);
            updateAddressLength(ref pRule.remoteIpAddress);
            updateAddressLength(ref pRule.remoteIpAddressMask);

            return nf_addRuleEx(ref pRule, toHead);
        }

        /**
        * Replace the rules in driver with the specified array.
        * @param pRules Array of <tt>NF_RULE</tt> structures
        * @param count Number of items in array
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern NF_STATUS nf_setRules(IntPtr pRules, int count);

        public static NF_STATUS nf_setRules(NF_RULE[] rules)
        {
            NF_RULE pRule;

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NF_RULE)) * rules.Length);

            long longPtr = ptr.ToInt64();
            for (int i = 0; i < rules.Length; i++)
            {
                pRule = rules[i];

                updateAddressLength(ref pRule.localIpAddress);
                updateAddressLength(ref pRule.localIpAddressMask);
                updateAddressLength(ref pRule.remoteIpAddress);
                updateAddressLength(ref pRule.remoteIpAddressMask);

                Marshal.StructureToPtr(pRule, new IntPtr(longPtr), false);

                longPtr += Marshal.SizeOf(typeof(NF_RULE));
            }

            return nf_setRules(ptr, rules.Length);
        }

        /**
        * Replace the rules in driver with the specified array.
        * @param pRules Array of <tt>NF_RULE</tt> structures
        * @param count Number of items in array
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern NF_STATUS nf_setRulesEx(IntPtr pRules, int count);

        public static NF_STATUS nf_setRulesEx(NF_RULE_EX[] rules)
        {
            NF_RULE_EX pRule;

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NF_RULE_EX)) * rules.Length);

            long longPtr = ptr.ToInt64();
            for (int i = 0; i < rules.Length; i++)
            {
                pRule = rules[i];

                updateAddressLength(ref pRule.localIpAddress);
                updateAddressLength(ref pRule.localIpAddressMask);
                updateAddressLength(ref pRule.remoteIpAddress);
                updateAddressLength(ref pRule.remoteIpAddressMask);

                Marshal.StructureToPtr(pRule, new IntPtr(longPtr), false);

                longPtr += Marshal.SizeOf(typeof(NF_RULE_EX));
            }

            return nf_setRules(ptr, rules.Length);
        }

        /**
        * Removes all rules from driver.
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_deleteRules();

        /**
		 *	Sets the timeout for TCP connections and returns old timeout.
		 *	@param timeout Timeout value in milliseconds. Specify zero value to disable timeouts.
		 */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 nf_setTCPTimeout(UInt32 timeout);

        /**
		 *	Disables indicating TCP packets to user mode for the specified endpoint
		 *  @param id Socket identifier
		 */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_tcpDisableFiltering(ulong id);

        /**
		 *	Disables indicating UDP packets to user mode for the specified endpoint
		 *  @param id Socket identifier
		 */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_udpDisableFiltering(ulong id);

        /**
        * Returns TRUE if the specified process acts as a local proxy, accepting the redirected TCP connections.
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool nf_tcpIsProxy(uint processId);

        /**
        * Set the number of worker threads and initialization flags.
        * The function should be called before nf_init. 
        * By default nThreads = 1 and flags = 0
        * @param nThreads Number of worker threads for NF_EventHandler events 
        * @param flags A combination of flags from <tt>NF_FLAGS</tt>
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern void nf_setOptions(uint nThreads, uint flags);

        /**
        * Complete TCP connect request pended using flag NF_PEND_CONNECT_REQUEST.
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_completeTCPConnectRequest(ulong id, ref NF_TCP_CONN_INFO pConnInfo);

        /**
        * Complete UDP connect request pended using flag NF_PEND_CONNECT_REQUEST.
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_completeUDPConnectRequest(ulong id, ref NF_UDP_CONN_REQUEST pConnInfo);

        /**
        * Returns in pConnInfo the properties of TCP connection with specified id.
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_getTCPConnInfo(ulong id, ref NF_TCP_CONN_INFO pConnInfo);

        /**
        * Returns in pConnInfo the properties of UDP socket with specified id.
        **/
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_getUDPConnInfo(ulong id, ref NF_UDP_CONN_INFO pConnInfo);

        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool nf_getProcessNameW(uint processId, IntPtr buf, int len);

        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool nf_getProcessNameFromKernel(uint processId, IntPtr buf, int len);

        /**
        * Returns the process name for given process id
        **/
        public static unsafe String nf_getProcessNameFromKernel(UInt32 processId)
        {
            char[] buf = new char[1024];

            fixed (char* p = buf)
            {
                if (nf_getProcessNameFromKernel(processId, (IntPtr) p, buf.Length))
                {
                    return Marshal.PtrToStringUni((IntPtr) p);
                }
            }

            return "System";
        }

        /**
        * Returns the process name for given process id
        **/
        public static unsafe String nf_getProcessName(UInt32 processId)
        {
            char[] buf = new char[256];

            fixed (char* p = buf)
            {
                if (nf_getProcessNameW(processId, (IntPtr) p, buf.Length))
                {
                    return Marshal.PtrToStringUni((IntPtr) p);
                }
            }

            return "System";
        }

        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern void nf_adjustProcessPriviledges();

        /*
        * Set the event handler for IP filtering events
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern void nf_setIPEventHandler(IntPtr pHandler);

        /*
        * Set the event handler for IP filtering events
        */
        public static void nf_setIPEventHandler(NF_IPEventHandler pHandler)
        {
            NF_IPEventHandlerFwd.m_pEventHandler = pHandler;

            m_pIPEventHandler = new NF_IPEventHandlerInternal();

            m_pIPEventHandler.ipReceive = new cbd_ipReceive(NF_IPEventHandlerFwd.ipReceive);
            m_pIPEventHandler.ipSend = new cbd_ipSend(NF_IPEventHandlerFwd.ipSend);

            m_pIPEventHandlerRaw = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NF_IPEventHandlerInternal)));
            Marshal.StructureToPtr(m_pIPEventHandler, m_pIPEventHandlerRaw, true);

            nf_setIPEventHandler(m_pIPEventHandlerRaw);
        }

        /**
        * Add flow control context
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_addFlowCtl(ref NF_FLOWCTL_DATA pData, ref UInt32 pFcHandle);

        /**
        * Delete flow control context
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_deleteFlowCtl(UInt32 fcHandle);

        /**
        * Associate flow control context with TCP connection
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_setTCPFlowCtl(ulong id, UInt32 fcHandle);

        /**
        * Associate flow control context with UDP socket
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_setUDPFlowCtl(ulong id, UInt32 fcHandle);

        /**
        * Modify flow control context limits
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_modifyFlowCtl(UInt32 fcHandle, ref NF_FLOWCTL_DATA pData);

        /**
        * Get flow control context statistics as the numbers of in/out bytes
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_getFlowCtlStat(UInt32 fcHandle, ref NF_FLOWCTL_STAT pStat);

        /**
        * Get TCP connection statistics as the numbers of in/out bytes.
        * The function can be called only from tcpClosed handler!
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_getTCPStat(ulong id, ref NF_FLOWCTL_STAT pStat);

        /**
        * Get UDP socket statistics as the numbers of in/out bytes.
        * The function can be called only from udpClosed handler!
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_getUDPStat(ulong id, ref NF_FLOWCTL_STAT pStat);

        public enum NF_SOCKET_OPTIONS
        {
            TCP_SOCKET_NODELAY = 1,
            TCP_SOCKET_KEEPALIVE = 2,
            TCP_SOCKET_OOBINLINE = 3,
            TCP_SOCKET_BSDURGENT = 4,
            TCP_SOCKET_ATMARK = 5,
            TCP_SOCKET_WINDOW = 6
        }

        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_tcpSetSockOpt(ulong id, NF_SOCKET_OPTIONS optname, ref int optval, int optlen);

        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_tcpSetSockOpt(ulong id, NF_SOCKET_OPTIONS optname, IntPtr optval, int optlen);

        public static NF_STATUS nf_tcpSetSockOpt(ulong id, NF_SOCKET_OPTIONS optname, bool optval)
        {
            int dword = optval ? 1 : 0;
            return nf_tcpSetSockOpt(id, optname, ref dword, Marshal.SizeOf(typeof(int)));
        }

        /**
        * Add binding rule to driver
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        private static extern NF_STATUS nf_addBindingRule(ref NF_BINDING_RULE pRule, int toHead);

        public static NF_STATUS nf_addBindingRule(NF_BINDING_RULE pRule, int toHead)
        {
            updateAddressLength(ref pRule.localIpAddress);
            updateAddressLength(ref pRule.localIpAddressMask);
            updateAddressLength(ref pRule.newLocalIpAddress);

            return nf_addBindingRule(ref pRule, toHead);
        }

        /**
        * Delete all binding rules from driver
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_deleteBindingRules();

        /**
        * Returns the type of attached driver (DT_WFP, DT_TDI or DT_UNKNOWN)
        */
        [DllImport("bin\\nfapinet", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 nf_getDriverType();
    };
}
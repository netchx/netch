using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace Netch
{
    public static class Win32Native
    {
        public enum ForwardType
        {
            Other = 1,
            Invalid = 2,
            Direct = 3,
            Indirect = 4,
        }

        public enum ForwardProtocol
        {
            Other = 1,
            Local = 2,
            NetMGMT = 3,
            ICMP = 4,
            EGP = 5,
            GGP = 6,
            Hello = 7,
            RIP = 8,
            IS_IS = 9,
            ES_IS = 10, // 0x0000000A
            CISCO = 11, // 0x0000000B
            BBN = 12, // 0x0000000C
            OSPF = 13, // 0x0000000D
            BGP = 14, // 0x0000000E
            NT_AUTOSTATIC = 10002, // 0x00002712
            NT_STATIC = 10006, // 0x00002716
            NT_STATIC_NON_DOD = 10007, // 0x00002717
        }

        public class RouteEntry
        {
            internal MIB_IPFORWARDROW _ipFwdNative;
            private int _metric1;
            private int _metric2;
            private int _metric3;
            private int _metric4;
            private int _metric5;
            private IPAddress _destination;
            private IPAddress _mask;
            private int _policy;
            private IPAddress _nextHop;
            private NetworkInterface _interface;
            private ForwardProtocol _protocol;
            private ForwardType _type;
            private int _nextHopAS;
            private int _age;
            private int _index;

            public int Index
            {
                get
                {
                    return this._index;
                }
                set
                {
                    this._index = value;
                }
            }

            public IPAddress Destination
            {
                get
                {
                    return this._destination;
                }
                set
                {
                    this._destination = value;
                }
            }

            public IPAddress Mask
            {
                get
                {
                    return this._mask;
                }
                set
                {
                    this._mask = value;
                }
            }

            public int Policy
            {
                get
                {
                    return this._policy;
                }
                set
                {
                    this._policy = value;
                }
            }

            public IPAddress NextHop
            {
                get
                {
                    return this._nextHop;
                }
                set
                {
                    this._nextHop = value;
                }
            }

            public NetworkInterface RelatedInterface
            {
                get
                {
                    return this._interface;
                }
            }

            public string InterfaceName
            {
                get
                {
                    if (this.RelatedInterface == null)
                        return string.Empty;
                    return this.RelatedInterface.Name;
                }
            }

            public ForwardType ForwardType
            {
                get
                {
                    return this._type;
                }
                set
                {
                    this._type = value;
                }
            }

            public ForwardProtocol Protocol
            {
                get
                {
                    return this._protocol;
                }
                set
                {
                    this._protocol = value;
                }
            }

            public int Age
            {
                get
                {
                    return this._age;
                }
                set
                {
                    this._age = value;
                }
            }

            public int NextHopAS
            {
                get
                {
                    return this._nextHopAS;
                }
                set
                {
                    this._nextHopAS = value;
                }
            }

            public int Metric1
            {
                get
                {
                    return this._metric1;
                }
                set
                {
                    this._metric1 = value;
                }
            }

            public int Metric2
            {
                get
                {
                    return this._metric2;
                }
                set
                {
                    this._metric2 = value;
                }
            }

            public int Metric3
            {
                get
                {
                    return this._metric3;
                }
                set
                {
                    this._metric3 = value;
                }
            }

            public int Metric4
            {
                get
                {
                    return this._metric4;
                }
                set
                {
                    this._metric4 = value;
                }
            }

            public int Metric5
            {
                get
                {
                    return this._metric5;
                }
                set
                {
                    this._metric5 = value;
                }
            }

            public RouteEntry(
              uint destination,
              uint mask,
              int policy,
              uint nextHop,
              NetworkInterface intf,
              ForwardType type,
              ForwardProtocol proto,
              int age,
              int nextHopAS,
              int metric1,
              int metric2,
              int metric3,
              int metric4,
              int metric5,
              int idx)
            {
                this._age = age;
                this._policy = policy;
                this._protocol = proto;
                this._type = type;
                this._destination = new IPAddress((long)destination);
                this._mask = new IPAddress((long)mask);
                this._nextHop = new IPAddress((long)nextHop);
                this._nextHopAS = nextHopAS;
                this._interface = intf;
                this._metric1 = metric1;
                this._metric2 = metric2;
                this._metric3 = metric3;
                this._metric4 = metric4;
                this._metric5 = metric5;
                this._index = idx;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MIB_IFROW
        {
            private const int MAX_INTERFACE_NAME_LEN = 256;
            private const int MAXLEN_IFDESCR = 256;
            private const int MAXLEN_PHYSADDR = 8;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string wszName;
            public int dwIndex;
            public int dwType;
            public int dwMtu;
            public int dwSpeed;
            public int dwPhysAddrLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] bPhysAddr;
            public int dwAdminStatus;
            public int dwOperStatus;
            public int dwLastChange;
            public int dwInOctets;
            public int dwInUcastPkts;
            public int dwInNUcastPkts;
            public int dwInDiscards;
            public int dwInErrors;
            public int dwInUnknownProtos;
            public int dwOutOctets;
            public int dwOutUcastPkts;
            public int dwOutNUcastPkts;
            public int dwOutDiscards;
            public int dwOutErrors;
            public int dwOutQLen;
            public int dwDescrLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] bDescr;
        }

        public class AdaptersTable
        {
            private Dictionary<int, NetworkInterface> _adapters = new Dictionary<int, NetworkInterface>();

            public IDictionary<int, NetworkInterface> GetAdapters()
            {
                return (IDictionary<int, NetworkInterface>)this._adapters;
            }

            public NetworkInterface GetAdapter(int interfaceIndex)
            {
                NetworkInterface networkInterface = (NetworkInterface)null;
                this._adapters.TryGetValue(interfaceIndex, out networkInterface);
                return networkInterface;
            }

            public int GetAdapterIndex(NetworkInterface networkInterface)
            {
                return this._adapters.First<KeyValuePair<int, NetworkInterface>>((Func<KeyValuePair<int, NetworkInterface>, bool>)(a => a.Value == networkInterface)).Key;
            }

            public AdaptersTable()
            {
                IntPtr num1 = IntPtr.Zero;
                int pdwSize = 0;
                int num2 = 0;
                num2 = Win32Native.GetIfTable(IntPtr.Zero, ref pdwSize, true);
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                try
                {
                    num1 = Marshal.AllocHGlobal(pdwSize);
                    if (Win32Native.GetIfTable(num1, ref pdwSize, true) != 0)
                        return;
                    int num3 = Marshal.ReadInt32(num1);
                    IntPtr ptr = new IntPtr(num1.ToInt32() + 4);
                    for (int index = 0; index < num3; ++index)
                    {
                        MIB_IFROW structure = (MIB_IFROW)Marshal.PtrToStructure(ptr, typeof(MIB_IFROW));
                        MIB_IFROW pIfRow = new MIB_IFROW();
                        pIfRow.dwIndex = structure.dwIndex;
                        if (Win32Native.GetIfEntry(ref pIfRow) == 0)
                        {
                            string str = Encoding.ASCII.GetString(structure.bDescr, 0, pIfRow.dwDescrLen - 1);
                            foreach (NetworkInterface networkInterface in networkInterfaces)
                            {
                                if (networkInterface.Description == str)
                                {
                                    this._adapters.Add(structure.dwIndex, networkInterface);
                                    break;
                                }
                            }
                        }
                        ptr = new IntPtr(ptr.ToInt32() + Marshal.SizeOf(typeof(MIB_IFROW)));
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    Marshal.FreeHGlobal(num1);
                }
            }
        }

        public struct MIB_IPFORWARDROW
        {
            public uint dwForwardDest;
            public uint dwForwardMask;
            public int dwForwardPolicy;
            public uint dwForwardNextHop;
            public int dwForwardIfIndex;
            public ForwardType dwForwardType;
            public ForwardProtocol dwForwardProto;
            public int dwForwardAge;
            public int dwForwardNextHopAS;
            public int dwForwardMetric1;
            public int dwForwardMetric2;
            public int dwForwardMetric3;
            public int dwForwardMetric4;
            public int dwForwardMetric5;

            public static implicit operator MIB_IPFORWARDROW(RouteEntry value)
            {
                MIB_IPFORWARDROW mibIpforwardrow = new MIB_IPFORWARDROW();
                mibIpforwardrow.dwForwardAge = value.Age;
                mibIpforwardrow.dwForwardDest = BitConverter.ToUInt32(value.Destination.GetAddressBytes(), 0);
                mibIpforwardrow.dwForwardMask = BitConverter.ToUInt32(value.Mask.GetAddressBytes(), 0);
                mibIpforwardrow.dwForwardMetric1 = value.Metric1;
                mibIpforwardrow.dwForwardMetric2 = value.Metric2;
                mibIpforwardrow.dwForwardMetric3 = value.Metric3;
                mibIpforwardrow.dwForwardMetric4 = value.Metric4;
                mibIpforwardrow.dwForwardMetric5 = value.Metric5;
                mibIpforwardrow.dwForwardNextHop = BitConverter.ToUInt32(value.NextHop.GetAddressBytes(), 0);
                mibIpforwardrow.dwForwardNextHopAS = value.NextHopAS;
                mibIpforwardrow.dwForwardPolicy = value.Policy;
                mibIpforwardrow.dwForwardProto = value.Protocol;
                mibIpforwardrow.dwForwardType = value.ForwardType;
                AdaptersTable adaptersTable = new AdaptersTable();
                mibIpforwardrow.dwForwardIfIndex = adaptersTable.GetAdapterIndex(value.RelatedInterface);
                return mibIpforwardrow;
            }
        }

        [DllImport("iphlpapi", SetLastError = true)]
        public static extern int GetIfTable(IntPtr pIfTable, ref int pdwSize, bool bOrder);

        [DllImport("iphlpapi", SetLastError = true)]
        public static extern int GetIfEntry(ref MIB_IFROW pIfRow);

        [DllImport("iphlpapi", SetLastError = true)]
        public static extern int GetBestRoute(uint dwDestAddr, int dwSourceAddr, out MIB_IPFORWARDROW pRoute);

        [DllImport("User32", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("WinINet")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    }
}

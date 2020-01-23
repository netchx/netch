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
            Indirect = 4
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
            NT_STATIC_NON_DOD = 10007 // 0x00002717
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
                get => _index;
                set => _index = value;
            }

            public IPAddress Destination
            {
                get => _destination;
                set => _destination = value;
            }

            public IPAddress Mask
            {
                get => _mask;
                set => _mask = value;
            }

            public int Policy
            {
                get => _policy;
                set => _policy = value;
            }

            public IPAddress NextHop
            {
                get => _nextHop;
                set => _nextHop = value;
            }

            public NetworkInterface RelatedInterface => _interface;

            public string InterfaceName
            {
                get
                {
                    if (RelatedInterface == null)
                        return string.Empty;
                    return RelatedInterface.Name;
                }
            }

            public ForwardType ForwardType
            {
                get => _type;
                set => _type = value;
            }

            public ForwardProtocol Protocol
            {
                get => _protocol;
                set => _protocol = value;
            }

            public int Age
            {
                get => _age;
                set => _age = value;
            }

            public int NextHopAS
            {
                get => _nextHopAS;
                set => _nextHopAS = value;
            }

            public int Metric1
            {
                get => _metric1;
                set => _metric1 = value;
            }

            public int Metric2
            {
                get => _metric2;
                set => _metric2 = value;
            }

            public int Metric3
            {
                get => _metric3;
                set => _metric3 = value;
            }

            public int Metric4
            {
                get => _metric4;
                set => _metric4 = value;
            }

            public int Metric5
            {
                get => _metric5;
                set => _metric5 = value;
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
                _age = age;
                _policy = policy;
                _protocol = proto;
                _type = type;
                _destination = new IPAddress(destination);
                _mask = new IPAddress(mask);
                _nextHop = new IPAddress(nextHop);
                _nextHopAS = nextHopAS;
                _interface = intf;
                _metric1 = metric1;
                _metric2 = metric2;
                _metric3 = metric3;
                _metric4 = metric4;
                _metric5 = metric5;
                _index = idx;
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
                return _adapters;
            }

            public NetworkInterface GetAdapter(int interfaceIndex)
            {
                NetworkInterface networkInterface = null;
                _adapters.TryGetValue(interfaceIndex, out networkInterface);
                return networkInterface;
            }

            public int GetAdapterIndex(NetworkInterface networkInterface)
            {
                return _adapters.First(a => a.Value == networkInterface).Key;
            }

            public AdaptersTable()
            {
                var num1 = IntPtr.Zero;
                var pdwSize = 0;
                var num2 = 0;
                num2 = GetIfTable(IntPtr.Zero, ref pdwSize, true);
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                try
                {
                    num1 = Marshal.AllocHGlobal(pdwSize);
                    if (GetIfTable(num1, ref pdwSize, true) != 0)
                        return;
                    var num3 = Marshal.ReadInt32(num1);
                    var ptr = new IntPtr(num1.ToInt32() + 4);
                    for (var index = 0; index < num3; ++index)
                    {
                        var structure = (MIB_IFROW)Marshal.PtrToStructure(ptr, typeof(MIB_IFROW));
                        var pIfRow = new MIB_IFROW();
                        pIfRow.dwIndex = structure.dwIndex;
                        if (GetIfEntry(ref pIfRow) == 0)
                        {
                            var str = Encoding.ASCII.GetString(structure.bDescr, 0, pIfRow.dwDescrLen - 1);
                            foreach (var networkInterface in networkInterfaces)
                            {
                                if (networkInterface.Description == str)
                                {
                                    _adapters.Add(structure.dwIndex, networkInterface);
                                    break;
                                }
                            }
                        }
                        ptr = new IntPtr(ptr.ToInt32() + Marshal.SizeOf(typeof(MIB_IFROW)));
                    }
                }
                catch (Exception)
                {
                    // 跳过
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
                var mibIpforwardrow = new MIB_IPFORWARDROW();
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
                var adaptersTable = new AdaptersTable();
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

using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.NetworkManagement.IpHelper;
using Netch.Models;

namespace Netch.Utils;

public static class PortHelper
{
    private static readonly List<NumberRange> TCPReservedRanges = new();
    private static readonly List<NumberRange> UDPReservedRanges = new();
    private static readonly IPGlobalProperties NetInfo = IPGlobalProperties.GetIPGlobalProperties();

    static PortHelper()
    {
        try
        {
            GetReservedPortRange(PortType.TCP, ref TCPReservedRanges);
            GetReservedPortRange(PortType.UDP, ref UDPReservedRanges);
        }
        catch (Exception e)
        {
            Log.Error(e, "Get reserved ports failed");
        }
    }

    internal static IEnumerable<Process> GetProcessByUsedTcpPort(ushort port, AddressFamily inet = AddressFamily.InterNetwork)
    {
        if (port == 0)
            throw new ArgumentOutOfRangeException();

        switch (inet)
        {
            case AddressFamily.InterNetwork:
            {
                var process = new List<Process>();
                unsafe
                {
                    uint err;
                    uint size = 0;
                    PInvoke.GetExtendedTcpTable(default, ref size, false, (uint)inet, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER, 0); // get size
                    var tcpTable = (MIB_TCPTABLE_OWNER_PID*)Marshal.AllocHGlobal((int)size);

                    if ((err = PInvoke.GetExtendedTcpTable(tcpTable, ref size, false, (uint)inet, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER, 0)) !=
                        0)
                        throw new Win32Exception((int)err);

                    for (var i = 0; i < tcpTable -> dwNumEntries; i++)
                    {
                        var row = tcpTable -> table.ReadOnlyItemRef(i);

                        if (row.dwOwningPid is 0 or 4)
                            continue;

                        if (PInvoke.ntohs((ushort)row.dwLocalPort) == port)
                            process.Add(Process.GetProcessById((int)row.dwOwningPid));
                    }
                }

                return process;
            }
            case AddressFamily.InterNetworkV6:
                throw new NotImplementedException();
            default:
                throw new InvalidOperationException();
        }
    }

    private static void GetReservedPortRange(PortType portType, ref List<NumberRange> targetList)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $" int ipv4 show excludedportrange {portType}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();

        foreach (var line in output.SplitRemoveEmptyEntriesAndTrimEntries('\n'))
        {
            var value = line.Trim().SplitRemoveEmptyEntries(' ');
            if (value.Length < 2)
                continue;

            if (!ushort.TryParse(value[0], out var start) || !ushort.TryParse(value[1], out var end))
                continue;

            targetList.Add(new NumberRange(start, end));
        }
    }

    /// <summary>
    ///     指定类型的端口是否已经被使用了
    /// </summary>
    /// <param name="port">端口</param>
    /// <param name="type">检查端口类型</param>
    /// <returns>是否被占用</returns>
    public static void CheckPort(ushort port, PortType type = PortType.Both)
    {
        switch (type)
        {
            case PortType.Both:
                CheckPort(port, PortType.TCP);
                CheckPort(port, PortType.UDP);
                break;
            default:
                CheckPortInUse(port, type);
                CheckPortReserved(port, type);
                break;
        }
    }

    private static void CheckPortInUse(ushort port, PortType type)
    {
        switch (type)
        {
            case PortType.Both:
                CheckPortInUse(port, PortType.TCP);
                CheckPortInUse(port, PortType.UDP);
                break;
            case PortType.TCP:
                if (NetInfo.GetActiveTcpListeners().Any(ipEndPoint => ipEndPoint.Port == port))
                    throw new PortInUseException();

                break;
            case PortType.UDP:
                if (NetInfo.GetActiveUdpListeners().Any(ipEndPoint => ipEndPoint.Port == port))
                    throw new PortInUseException();

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    /// <summary>
    ///     检查端口是否是保留端口
    /// </summary>
    private static void CheckPortReserved(ushort port, PortType type)
    {
        switch (type)
        {
            case PortType.Both:
                CheckPortReserved(port, PortType.TCP);
                CheckPortReserved(port, PortType.UDP);
                return;
            case PortType.TCP:
                if (TCPReservedRanges.Any(range => range.InRange(port)))
                    throw new PortReservedException();

                break;
            case PortType.UDP:
                if (UDPReservedRanges.Any(range => range.InRange(port)))
                    throw new PortReservedException();

                break;
            default:
                Trace.Assert(false);
                return;
        }
    }

    public static ushort GetAvailablePort(PortType portType = PortType.Both)
    {
        var random = new Random();
        for (ushort i = 0; i < 55535; i++)
        {
            var p = (ushort)random.Next(10000, 65535);
            try
            {
                CheckPort(p, portType);
                return p;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        throw new Exception();
    }
}

/// <summary>
///     检查端口类型
/// </summary>
[Flags]
public enum PortType
{
    TCP = 0b_01,
    UDP = 0b_10,
    Both = TCP | UDP
}

public class PortInUseException : Exception
{
    public PortInUseException(string message) : base(message)
    {
    }

    public PortInUseException()
    {
    }
}

public class PortReservedException : Exception
{
    public PortReservedException(string message) : base(message)
    {
    }

    public PortReservedException()
    {
    }
}
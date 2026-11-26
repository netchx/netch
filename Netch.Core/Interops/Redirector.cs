using System.Runtime.InteropServices;
using System.Text;

namespace Netch.Interops;

public static class Redirector
{
    public enum NameList
    {
        AIO_FILTERLOOPBACK,
        AIO_FILTERINTRANET, // LAN
        AIO_FILTERPARENT,
        AIO_FILTERICMP,
        AIO_FILTERTCP,
        AIO_FILTERUDP,
        AIO_FILTERDNS,

        AIO_ICMPING,

        AIO_DNSONLY,
        AIO_DNSPROX,
        AIO_DNSHOST,
        AIO_DNSPORT,

        AIO_TGTHOST,
        AIO_TGTPORT,
        AIO_TGTUSER,
        AIO_TGTPASS,

        AIO_CLRNAME,
        AIO_ADDNAME,
        AIO_BYPNAME,

        AIO_CLRIP,
        AIO_ADDIP,
        AIO_BYPIP,

        AIO_TRACKCONNECTIONS,
        AIO_TRACKUNMATCHED,

        AIO_BEGINRULES,
        AIO_COMMITRULES
    }

    public static bool Dial(NameList name, bool value)
    {
        Log.Verbose($"[Redirector] Dial {name}: {value}");
        return aio_dial(name, value.ToString().ToLower());
    }

    public static bool Dial(NameList name, string value)
    {
        Log.Verbose($"[Redirector] Dial {name}: {value}");
        return aio_dial(name, value);
    }

    public static Task<bool> InitAsync()
    {
        return Task.Run(aio_init);
    }

    public static Task FreeAsync()
    {
        return Task.Run(aio_free);
    }

    /// <summary>
    ///     Bytes forwarded by the redirector (TCP and UDP), by direction.
    /// </summary>
    public static (ulong Download, ulong Upload) GetTrafficBytes() => (aio_getDL(), aio_getUP());

    public static string GetConnectionsJson() => ReadTwoPassUtf8(aio_getConnectionsJson);

    public static string DrainConnectionEventsJson(ulong afterSeq) =>
        ReadTwoPassUtf8((buf, cap) => aio_drainConnectionEventsJson(afterSeq, buf, cap));

    public static bool CloseConnection(ulong connId) => aio_closeConnection(connId);

    public static void CloseAllConnections() => aio_closeAllConnections();

    public static void ClearClosedConnections() => aio_clearClosedConnections();

    private static string ReadTwoPassUtf8(Func<byte[]?, int, int> reader)
    {
        var needed = reader(null, 0);
        if (needed <= 0)
            return string.Empty;

        var capacity = needed + 1024;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var buffer = new byte[capacity];
            var actual = reader(buffer, capacity);
            if (actual <= 0)
                return string.Empty;

            if (actual < capacity)
                return Encoding.UTF8.GetString(buffer, 0, actual);

            capacity = actual + 4096;
        }

        return string.Empty;
    }

    private const string Redirector_bin = "Redirector.bin";

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool aio_register([MarshalAs(UnmanagedType.LPWStr)] string value);

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool aio_unregister([MarshalAs(UnmanagedType.LPWStr)] string value);

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool aio_dial(NameList name, [MarshalAs(UnmanagedType.LPWStr)] string value);

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool aio_init();

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern void aio_free();

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong aio_getUP();

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong aio_getDL();

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern int aio_getConnectionsJson(byte[]? buffer, int capacity);

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern int aio_drainConnectionEventsJson(ulong afterSeq, byte[]? buffer, int capacity);

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool aio_closeConnection(ulong connId);

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern void aio_closeAllConnections();

    [DllImport(Redirector_bin, CallingConvention = CallingConvention.Cdecl)]
    private static extern void aio_clearClosedConnections();
}

using System.Runtime.InteropServices;

namespace Netch;

public static class NativeMethods
{
    [DllImport("dnsapi", EntryPoint = "DnsFlushResolverCache")]
    internal static extern uint RefreshDNSCache();
}
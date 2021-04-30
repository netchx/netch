using System.Runtime.InteropServices;

namespace Netch
{
    public static class NativeMethods
    {
        [DllImport("dnsapi", EntryPoint = "DnsFlushResolverCache")]
        public static extern uint RefreshDNSCache();
    }
}

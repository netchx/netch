using System.Runtime.InteropServices;

namespace Netch
{
    public static class NativeMethods
    {
        /// <summary>
        ///     分配 IP 地址
        /// </summary>
        /// <param name="inet">AF_INET / AF_INET6</param>
        /// <param name="address">目标地址</param>
        /// <param name="cidr">CIDR</param>
        /// <param name="index">适配器索引</param>
        /// <returns>是否成功</returns>
        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateUnicastIP")]
        public static extern bool CreateUnicastIP(int inet, string address, int cidr, int index);

        /// <summary>
        ///     创建路由规则
        /// </summary>
        /// <param name="inet">AF_INET / AF_INET6</param>
        /// <param name="address">目标地址</param>
        /// <param name="cidr">CIDR</param>
        /// <param name="gateway">网关地址</param>
        /// <param name="index">适配器索引</param>
        /// <param name="metric">跃点数</param>
        /// <returns>是否成功</returns>
        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateRoute")]
        public static extern bool CreateRoute(int inet, string address, int cidr, string gateway, int index, int metric = 0);

        /// <summary>
        ///     删除路由规则
        /// </summary>
        /// <param name="inet">AF_INET / AF_INET6</param>
        /// <param name="address">目标地址</param>
        /// <param name="cidr">掩码地址</param>
        /// <param name="gateway">网关地址</param>
        /// <param name="index">适配器索引</param>
        /// <param name="metric">跃点数</param>
        /// <returns>是否成功</returns>
        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl, EntryPoint = "DeleteRoute")]
        public static extern bool DeleteRoute(int inet, string address, int cidr, string gateway, int index, int metric = 0);

        [DllImport("RouteHelper.bin", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong ConvertLuidToIndex(ulong luid);

        [DllImport("dnsapi", EntryPoint = "DnsFlushResolverCache")]
        public static extern uint FlushDNSResolverCache();
    }
}
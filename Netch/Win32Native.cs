using System.Runtime.InteropServices;

namespace Netch
{
    public static class Win32Native
    {
        public enum OptionType
        {
            /// <summary>
            ///     无
            /// </summary>
            OT_NONE,

            /// <summary>
            ///     驱动名（NetFilter2）
            /// </summary>
            OT_DRIVER_NAME,

            /// <summary>
            ///     协议（TCP 或 UDP）
            /// </summary>
            OT_PROTOCOL,

            /// <summary>
            ///     进程名
            /// </summary>
            OT_PROCESS_NAME,

            /// <summary>
            ///     远程 IP CIDR
            /// </summary>
            OT_REMOTE_ADDRESS,

            /// <summary>
            ///     远程端口
            /// </summary>
            OT_REMOTE_PORT,

            /// <summary>
            ///     动作类型
            /// </summary>
            OT_ACTION,

            /// <summary>
            ///     代理地址
            /// </summary>
            OT_PROXY_ADDRESS,

            /// <summary>
            ///     代理用户名
            /// </summary>
            OT_PROXY_USER_NAME,

            /// <summary>
            ///     代理密码
            /// </summary>
            OT_PROXY_PASSWORD
        }

        /// <summary>
        ///     初始化
        /// </summary>
        /// <returns></returns>
        [DllImport("NetchCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool srn_init();

        /// <summary>
        ///     停止重定向和释放内存
        /// </summary>
        /// <returns></returns>
        [DllImport("NetchCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool srn_free();

        /// <summary>
        ///     清除所有选项
        /// </summary>
        /// <returns></returns>
        [DllImport("NetchCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool srn_clearOptions();

        /// <summary>
        ///     开始添加规则
        /// </summary>
        [DllImport("NetchCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void srn_startRule();

        /// <summary>
        ///     推送添加的规则
        /// </summary>
        [DllImport("NetchCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void srn_endRule();

        /// <summary>
        ///     添加指定类型的选项
        /// </summary>
        [DllImport("NetchCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void srn_addOption(OptionType type, string value);

        /// <summary>
        ///     启动或停止重定向
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        [DllImport("NetchCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool srn_enable(int start);
    }
}

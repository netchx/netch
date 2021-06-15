namespace Netch.Models.Mode
{
    public enum ModeType : int
    {
        /// <summary>
        ///     进程代理
        /// </summary>
        ProcessMode,

        /// <summary>
        ///     网络共享
        /// </summary>
        ShareMode,

        /// <summary>
        ///     网卡代理
        /// </summary>
        TapMode,

        /// <summary>
        ///     网卡代理
        /// </summary>
        TunMode,

        /// <summary>
        ///     网页代理
        /// </summary>
        WebMode,

        /// <summary>
        ///     代理转发
        /// </summary>
        WmpMode
    }
}

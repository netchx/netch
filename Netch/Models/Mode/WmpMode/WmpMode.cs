namespace Netch.Models.Mode.WmpMode
{
    public class WmpMode : Mode
    {
        public WmpMode()
        {
            this.Type = ModeType.WmpMode;
        }

        /// <summary>
        ///     监听地址（为空则监听所有 IPv4 + IPv6 地址）
        /// </summary>
        public string ListenAddr;

        /// <summary>
        ///     监听端口
        /// </summary>
        public ushort ListenPort;

        /// <summary>
        ///     远端地址
        /// </summary>
        public string RemoteAddr;

        /// <summary>
        ///     远端端口
        /// </summary>
        public ushort RemotePort;
    }
}

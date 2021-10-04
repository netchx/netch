namespace Netch.Models.Server
{
    public enum ServerType : int
    {
        /// <summary>
        ///     Socks5
        /// </summary>
        Socks,

        /// <summary>
        ///     Shadowsocks
        /// </summary>
        Shadowsocks,

        /// <summary>
        ///     ShadowsocksR
        /// </summary>
        ShadowsocksR,

        /// <summary>
        ///     WireGuard
        /// </summary>
        WireGuard,

        /// <summary>
        ///     Trojan
        /// </summary>
        Trojan,

        /// <summary>
        ///     VMess
        /// </summary>
        VMess,

        /// <summary>
        ///     VLess
        /// </summary>
        VLess
    }
}

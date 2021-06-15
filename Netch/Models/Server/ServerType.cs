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
        ///     Trojan
        /// </summary>
        Trojan,

        /// <summary>
        ///     VLess
        /// </summary>
        VLess,

        /// <summary>
        ///     VMess
        /// </summary>
        VMess
    }
}

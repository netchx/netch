namespace Netch.Models.Server
{
    public enum ServerType : int
    {
        /// <summary>
        ///     HTTP
        /// </summary>
        HTTP,

        /// <summary>
        ///     Clash
        /// </summary>
        Clash,

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

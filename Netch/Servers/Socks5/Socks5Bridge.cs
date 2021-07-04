namespace Netch.Servers
{
    // TODO rename it
    /// <summary>
    ///     Encrypted proxy client's local socks5 server
    ///     (<see cref="RemoteHostname"/> property is used for saving remote address/hostname for special use)
    /// </summary>
    public class Socks5Bridge : Socks5
    {
        public Socks5Bridge(string hostname, ushort port, string remoteHostname) : base(hostname, port)
        {
            RemoteHostname = remoteHostname;
        }

        public string RemoteHostname { get; set; }
    }
}
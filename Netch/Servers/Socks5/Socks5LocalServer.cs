namespace Netch.Servers
{
    // TODO rename it
    /// <summary>
    ///     Encrypted proxy client's local socks5 server
    ///     (<see cref="RemoteHostname"/> property is used for saving remote address/hostname for special use)
    /// </summary>
    public class Socks5LocalServer : Socks5Server
    {
        public Socks5LocalServer(string hostname, ushort port, string remoteHostname)
        {
            Hostname = hostname;
            Port = port;
            RemoteHostname = remoteHostname;
        }

        public string RemoteHostname { get; set; }
    }
}
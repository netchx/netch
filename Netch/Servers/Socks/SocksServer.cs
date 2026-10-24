using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class SocksServer : Server
{
    public override EConfigType ConfigType { get; } = EConfigType.SOCKS;

    public override string MaskedData()
    {
        return $"Auth: {Auth()}";
    }

    public string? RemoteHostname { get; set; }

    public SocksServer()
    {
    }

    public SocksServer(string hostname, ushort port)
    {
        Address = hostname;
        Port = port;
    }

    public SocksServer(string hostname, ushort port, string username, string password) : this(hostname, port)
    {
        Username = username;
        Password = password;
    }

    public SocksServer(string hostname, ushort port, string remoteHostname) : this(hostname, port)
    {
        RemoteHostname = remoteHostname;
    }

    /// <summary>
    /// 是否有账号密码
    /// </summary>
    /// <returns></returns>
    public bool Auth()
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }
}
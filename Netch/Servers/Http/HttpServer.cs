using Netch.Enums;
using Netch.Models;

namespace Netch.Servers;

public class HttpServer : Server
{
    public override EConfigType ConfigType { get; } = EConfigType.HTTP;

    public override string MaskedData()
    {
        return $"Auth: {Auth()}";
    }

    public HttpServer()
    {
    }

    public HttpServer(string hostname, ushort port)
    {
        Address = hostname;
        Port = port;
    }

    public HttpServer(string hostname, ushort port, string username, string password) : this(hostname, port)
    {
        Username = username;
        Password = password;
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
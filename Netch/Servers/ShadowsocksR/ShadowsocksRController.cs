using System.Net;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class ShadowsocksRController : Guard, IServerController
{
    public ShadowsocksRController() : base("ShadowsocksR.exe")
    {
    }

    protected override IEnumerable<string> StartedKeywords => new[] { "listening at" };

    protected override IEnumerable<string> FailedKeywords => new[] { "Invalid config path", "usage" };

    public override string Name => "ShadowsocksR";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public async Task<Socks5Server> StartAsync(Server s)
    {
        var server = (ShadowsocksRServer)s;

        var arguments = new object?[]
        {
            "-s", await server.AutoResolveHostnameAsync(),
            "-p", server.Port,
            "-k", server.Password,
            "-m", server.EncryptMethod,
            "-t", 120,
            "-O", server.Protocol,
            "-G", server.ProtocolParam,
            "-o", server.OBFS,
            "-g", server.OBFSParam,
            "-b", this.LocalAddress(),
            "-l", this.Socks5LocalPort(),
            "-u", SpecialArgument.Flag
        };

        await StartGuardAsync(Arguments.Format(arguments));
        return new Socks5Server(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), server.Hostname);
    }
}
using System.Net;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class TrojanController : Guard, IServerController
{
    public TrojanController() : base("Trojan.exe")
    {
    }

    protected override IEnumerable<string> StartedKeywords => new[] { "listening" };

    protected override IEnumerable<string> FailedKeywords => new[] { "exiting" };

    public override string Name => "Trojan";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public async Task<Socks5Server> StartAsync(Server s)
    {
        var server = (TrojanServer)s;
        var trojanConfig = new TrojanConfig
        {
            local_addr = this.LocalAddress(),
            local_port = this.Socks5LocalPort(),
            remote_addr = await server.AutoResolveHostnameAsync(),
            remote_port = server.Port,
            password = new List<string>
            {
                server.Password
            },
            ssl = new TrojanSSL
            {
                sni = server.Host.ValueOrDefault() ?? server.Hostname
            }
        };

        await using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(fileStream, trojanConfig, Global.NewCustomJsonSerializerOptions());
        }

        await StartGuardAsync("-config ..\\data\\last.json");
        return new Socks5Server(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), server.Hostname);
    }
}
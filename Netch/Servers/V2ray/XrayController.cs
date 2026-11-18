using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public sealed class XrayController : Guard, IServerController
{
    public XrayController() : base("xray.exe") { }

    protected override IEnumerable<string> StartedKeywords => ["started"];
    protected override IEnumerable<string> FailedKeywords => ["config file not readable", "failed to", "Failed to start"];

    public override string Name => "Xray";
    public ushort? Socks5LocalPort { get; set; }
    public string? LocalAddress { get; set; }

    public async Task<SocksServer> StartAsync(Server server)
    {
        if (!CoreSelector.UsesXhttp(server))
            throw new MessageException("Xray 仅用于 XHTTP 节点。");

        var dataDirectory = Path.Combine(Global.NetchDir, "data");
        Directory.CreateDirectory(dataDirectory);
        var configPath = Path.Combine(Global.NetchDir, Constants.TempConfig);
        var config = V2rayConfigUtils.GenerateClientConfig(server, this.LocalAddress(), this.Socks5LocalPort());
        await using (var fileStream = new FileStream(configPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(fileStream, config, Global.NewCustomJsonSerializerOptions());
        }

        await StartGuardAsync($"run -c \"{configPath}\"");
        return new SocksServer(this.LocalAddress(), this.Socks5LocalPort(), server.Address);
    }
}

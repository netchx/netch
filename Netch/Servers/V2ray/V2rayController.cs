using System.Net;
using System.Text.Json;
using Netch.Controllers;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class V2rayController : Guard, IServerController
{
    private IServerController? _delegatedController;

    public V2rayController() : base("xray.exe")
    {
    }

    protected override IEnumerable<string> StartedKeywords => new[] { "started" };

    protected override IEnumerable<string> FailedKeywords => new[] { "config file not readable", "failed to" };

    public override string Name => "Xray";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public virtual async Task<SocksServer> StartAsync(Server s)
    {
        // TUIC, AnyTLS, Naive and MASQUE are sing-box protocols. Keep the
        // existing controller entry point for compatibility with MainController,
        // but delegate these profiles to sing-box and forward StopAsync below.
        if (s.ConfigType is EConfigType.TUIC or EConfigType.AnyTLS or EConfigType.Naive)
        {
            _delegatedController = new SingboxController
            {
                Socks5LocalPort = Socks5LocalPort,
                LocalAddress = LocalAddress
            };
            return await _delegatedController.StartAsync(s);
        }

        await using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(fileStream, await V2rayConfigUtils.GenerateClientConfigAsync(s), Global.NewCustomJsonSerializerOptions());
        }

        await StartGuardAsync("run -c ..\\data\\last.json");
        return new SocksServer(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), s.Address);
    }

    public override async Task StopAsync()
    {
        if (_delegatedController != null)
        {
            await _delegatedController.StopAsync();
            _delegatedController = null;
        }

        await base.StopAsync();
    }
}

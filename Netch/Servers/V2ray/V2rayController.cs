using System.Net;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class V2rayController : Guard, IServerController
{
    private readonly bool _useXray;

    public V2rayController(bool useXray = false) : base(useXray ? "xray.exe" : "v2ray-sn.exe")
    {
        _useXray = useXray;
        if (useXray && !Global.Settings.V2RayConfig.XrayCone)
            Instance.StartInfo.Environment["XRAY_CONE_DISABLED"] = "true";
    }

    protected override IEnumerable<string> StartedKeywords => new[] { "started" };

    protected override IEnumerable<string> FailedKeywords => new[] { "config file not readable", "failed to", "Failed to start" };

    public override string Name => _useXray ? "Xray" : "V2Ray (SagerNet)";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public virtual async Task<Socks5Server> StartAsync(Server s)
    {
        await using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(fileStream, await V2rayConfigUtils.GenerateClientConfigAsync(s, _useXray), Global.NewCustomJsonSerializerOptions());
        }

        await StartGuardAsync("run -c ..\\data\\last.json");
        return new Socks5Server(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), s.Hostname);
    }
}

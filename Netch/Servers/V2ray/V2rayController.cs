using System.Net;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class V2rayController : Guard, IServerController
{
    private string CoreName;
    public V2rayController(string core) : base(core)
    {
        if (core == "xray.exe")
        {
            CoreName = "Xray";
            if (!Global.Settings.V2RayConfig.XrayCone)
                Instance.StartInfo.Environment["XRAY_CONE_DISABLED"] = "true";
        }
        else
        {
            CoreName = "V2Ray";
        }
    }

    protected override IEnumerable<string> StartedKeywords => new[] { "started" };

    protected override IEnumerable<string> FailedKeywords => new[] { "config file not readable", "failed to" };

    public override string Name => CoreName;

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public virtual async Task<Socks5Server> StartAsync(Server s)
    {
        if (s.Type != "VLite")
        {
            await using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await JsonSerializer.SerializeAsync(fileStream, await V2rayConfigUtils.GenerateClientConfigAsync(s), Global.NewCustomJsonSerializerOptions());
            }

            await StartGuardAsync("-config ..\\data\\last.json");
        }
        else
        {
            await using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await JsonSerializer.SerializeAsync(fileStream, await VLiteConfigUtils.GenerateClientConfigAsync(s), Global.NewCustomJsonSerializerOptions());
            }

            // ./v2ray.exe run -c last.json -format=jsonv5
            await StartGuardAsync("run -c ..\\data\\last.json -format=jsonv5");
        }

        return new Socks5Server(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), s.Hostname);
    }
}
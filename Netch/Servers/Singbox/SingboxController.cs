using System.Diagnostics;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

/// <summary>
/// Runs a small sing-box client configuration for protocols that Xray does not
/// implement in the same way as v2rayN (TUIC, AnyTLS, NaiveProxy and MASQUE).
/// </summary>
public sealed class SingboxController : Guard, IServerController
{
    public SingboxController() : base(FindExecutable())
    {
    }

    public override string Name => "sing-box";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public async Task<SocksServer> StartAsync(Server server)
    {
        if (server.ConfigType is not (Enums.EConfigType.TUIC or Enums.EConfigType.AnyTLS or Enums.EConfigType.Naive or Enums.EConfigType.MASQUE))
            throw new MessageException($"sing-box 不支持节点类型 {server.ConfigType}");

        var config = BuildConfig(server);
        Directory.CreateDirectory(Path.Combine(Global.NetchDir, "data"));
        await using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(fileStream, config, Global.NewCustomJsonSerializerOptions());
        }

        Log.Information("Starting sing-box for {ConfigType}: {Address}:{Port}", server.ConfigType, server.Address, server.Port);
        await StartGuardAsync("run -c ..\\data\\last.json");

        await Task.Delay(250);
        if (Instance.HasExited)
        {
            throw new MessageException("sing-box 启动失败，请查看 logging\\sing-box.log");
        }

        return new SocksServer(this.LocalAddress ?? Global.Settings.LocalAddress, this.Socks5LocalPort ?? Global.Settings.Socks5LocalPort, server.Address);
    }

    private Dictionary<string, object?> BuildConfig(Server server)
    {
        var proxy = BuildProxy(server);
        var config = new Dictionary<string, object?>
        {
            ["log"] = new Dictionary<string, object?>
            {
                ["level"] = "debug",
                ["timestamp"] = true
            },
            ["inbounds"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "mixed",
                    ["tag"] = "socks-in",
                    ["listen"] = this.LocalAddress ?? Global.Settings.LocalAddress,
                    ["listen_port"] = this.Socks5LocalPort ?? Global.Settings.Socks5LocalPort,
                    ["sniff"] = true
                }
            },
            ["outbounds"] = new object[]
            {
                server.ConfigType == Enums.EConfigType.MASQUE
                    ? new Dictionary<string, object?> { ["type"] = "direct", ["tag"] = "direct" }
                    : proxy,
                new Dictionary<string, object?> { ["type"] = "direct", ["tag"] = "direct" }
            },
            ["route"] = new Dictionary<string, object?>
            {
                ["auto_detect_interface"] = true,
                ["final"] = "proxy"
            }
        };

        if (server.ConfigType == Enums.EConfigType.MASQUE)
        {
            config["endpoints"] = new[] { proxy };
        }

        return config;
    }

    private Dictionary<string, object?> BuildProxy(Server server)
    {
        var tls = BuildTls(server);
        var extra = server.ProtoExtra;

        if (server.ConfigType == Enums.EConfigType.MASQUE)
        {
            var endpoint = new Dictionary<string, object?>
            {
                ["type"] = "masque-client",
                ["tag"] = "proxy",
                ["server"] = server.Address,
                ["server_port"] = server.Port,
                ["username"] = server.Username,
                ["password"] = server.Password,
                ["path"] = extra.MasquePath.IsNullOrEmpty() ? "/" : extra.MasquePath,
                ["tls"] = tls
            };
            var headers = BuildHeaders(extra.MasqueHeaders);
            if (headers.Count > 0) endpoint["headers"] = headers;
            return endpoint;
        }

        var outbound = new Dictionary<string, object?>
        {
            ["tag"] = "proxy",
            ["server"] = server.Address,
            ["server_port"] = server.Port,
            ["tls"] = tls
        };

        switch (server.ConfigType)
        {
            case Enums.EConfigType.TUIC:
                outbound["type"] = "tuic";
                outbound["uuid"] = server.Username;
                outbound["password"] = server.Password;
                outbound["congestion_control"] = extra.CongestionControl.IsNullOrEmpty() ? "bbr" : extra.CongestionControl;
                break;
            case Enums.EConfigType.AnyTLS:
                outbound["type"] = "anytls";
                outbound["password"] = server.Password;
                break;
            case Enums.EConfigType.Naive:
                outbound["type"] = "naive";
                outbound["username"] = server.Username;
                outbound["password"] = server.Password;
                if (extra.NaiveQuic == true)
                {
                    outbound["quic"] = true;
                    if (extra.CongestionControl.IsNotEmpty()) outbound["quic_congestion_control"] = extra.CongestionControl;
                }
                if (extra.InsecureConcurrency is > 0) outbound["insecure_concurrency"] = extra.InsecureConcurrency;
                if (extra.Uot == true) outbound["udp_over_tcp"] = true;
                break;
            default:
                throw new MessageException($"未知 sing-box 节点类型 {server.ConfigType}");
        }

        return outbound;
    }

    private static Dictionary<string, object?> BuildTls(Server server)
    {
        var tls = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["server_name"] = server.Sni.IsNullOrEmpty() ? server.Address : server.Sni
        };
        if (server.ConfigType != Enums.EConfigType.Naive)
            tls["insecure"] = server.AllowInsecure == true;
        var alpn = server.GetAlpn();
        if (alpn is { Count: > 0 }) tls["alpn"] = alpn;
        if (server.Fingerprint.IsNotEmpty())
        {
            tls["utls"] = new Dictionary<string, object?>
            {
                ["enabled"] = true,
                ["fingerprint"] = server.Fingerprint
            };
        }
        return tls;
    }

    private static Dictionary<string, List<string>> BuildHeaders(string? value)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (value.IsNullOrWhiteSpace()) return result;

        foreach (var line in value.Split(['\r', '\n', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0 || separator == line.Length - 1) continue;
            var name = line[..separator].Trim();
            var headerValue = line[(separator + 1)..].Trim();
            if (!result.TryGetValue(name, out var values))
            {
                values = [];
                result[name] = values;
            }
            values.Add(headerValue);
        }
        return result;
    }

    private static string FindExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Global.NetchDir, "bin", "sing-box.exe"),
            Path.Combine(Global.NetchDir, "bin", "sing_box", "sing-box.exe")
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}

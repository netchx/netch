using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class MihomoController : Guard, IServerController
{
    public MihomoController() : base("mihomo.exe", encoding: Encoding.UTF8) { }

    public override string Name => "mihomo";
    protected override IEnumerable<string> StartedKeywords => ["Mixed(http+socks)[netch] proxy listening at:"];
    protected override IEnumerable<string> FailedKeywords => ["level=fatal", "Listener netch listen err:"];

    public ushort? Socks5LocalPort { get; set; }
    public string? LocalAddress { get; set; }

    public async Task<SocksServer> StartAsync(Server server)
    {
        var address = this.LocalAddress();
        var port = this.Socks5LocalPort();
        // MainController primes the system DNS cache before starting the proxy.
        var upstream = (IPAddress.TryParse(Global.Settings.OutboundDNS, out var dns)
                ? await DnsUtils.LookupAsync(server.Address, dns: dns.ToString(), useCache: false)
                : null)
            ?? await DnsUtils.LookupAsync(server.Address)
            ?? throw new MessageException(i18N.Translate("Lookup Server hostname failed"));
        var config = MihomoConfig.Build(server, upstream.ToString(), address, port);
        var dataDirectory = Path.Combine(Global.NetchDir, "data");
        var configPath = Path.Combine(Global.NetchDir, Constants.TempConfig);
        Directory.CreateDirectory(dataDirectory);
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, Global.NewCustomJsonSerializerOptions()));

        var arguments = $"-d \"{dataDirectory}\" -f \"{configPath}\"";
        await CheckConfigAsync(arguments);
        try
        {
            await StartGuardAsync(arguments);
        }
        catch (MessageException error)
        {
            throw new MessageException($"{error.Message} 请查看 logging\\mihomo.log。");
        }

        var localIp = IPAddress.Parse(address);
        if (localIp.Equals(IPAddress.Any)) localIp = IPAddress.Loopback;
        if (localIp.Equals(IPAddress.IPv6Any)) localIp = IPAddress.IPv6Loopback;
        try
        {
            await CheckSocksAsync(localIp, port);
            if (Instance.HasExited) throw new MessageException("Mihomo 已退出。");
        }
        catch (Exception error) when (error is MessageException or IOException or SocketException or OperationCanceledException)
        {
            await StopAsync();
            throw new MessageException($"Mihomo 启动失败。{error.Message} 请查看 logging\\mihomo.log。");
        }

        return new SocksServer(localIp.ToString(), port, upstream.ToString());
    }

    protected override void OnStartFailed() { }

    private async Task CheckConfigAsync(string arguments)
    {
        using var check = Process.Start(new ProcessStartInfo(Instance.StartInfo.FileName, $"-t {arguments}")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Instance.StartInfo.WorkingDirectory
        }) ?? throw new MessageException("Mihomo 配置检查进程无法启动。");
        var stdout = check.StandardOutput.ReadToEndAsync();
        var stderr = check.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await check.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!check.HasExited) check.Kill();
            throw new MessageException("Mihomo 配置检查超时。");
        }

        if (check.ExitCode != 0)
        {
            await File.WriteAllTextAsync(LogPath, (await stdout) + (await stderr));
            throw new MessageException("Mihomo 配置检查失败，请查看 logging\\mihomo.log。");
        }
    }

    private static async Task CheckSocksAsync(IPAddress ip, ushort port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var client = new TcpClient(ip.AddressFamily);
        await client.ConnectAsync(ip, port, timeout.Token);
        var stream = client.GetStream();
        await stream.WriteAsync(new byte[] { 5, 1, 0 }, timeout.Token);
        var reply = new byte[2];
        await stream.ReadExactlyAsync(reply, timeout.Token);
        if (reply[0] != 5 || reply[1] != 0)
            throw new MessageException("本地 SOCKS5 握手失败。");
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class ExternalCliController : Guard, IServerController
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(15);

    private readonly CustomCore? _customCore;
    private MihomoController? _httpBridge;
    private string? _tempConfigPath;

    public ExternalCliController(ExternalCliServer server)
        : this(
            ResolveExecutablePath((server ?? throw new ArgumentNullException(nameof(server))).ProtoExtra.CliExecutable),
            ValidatePort(server.Port),
            null)
    {
    }

    public ExternalCliController(CustomCore customCore)
        : this(
            ResolveExecutablePath((customCore ?? throw new ArgumentNullException(nameof(customCore))).ExecutablePath),
            null,
            customCore)
    {
    }

    private ExternalCliController(string resolvedExecutablePath, ushort? fixedLocalPort, CustomCore? customCore)
        : base(resolvedExecutablePath, encoding: Encoding.UTF8)
    {
        _customCore = customCore;
        if (fixedLocalPort.HasValue)
            Socks5LocalPort = fixedLocalPort.Value;

        var workDir = Path.GetDirectoryName(ExecutablePath);
        if (!string.IsNullOrEmpty(workDir))
            Instance.StartInfo.WorkingDirectory = workDir;
    }

    public override string Name => "external-cli";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public bool RequireLocalHttpProxy { get; set; }

    public static string NormalizeExecutableInput(string? raw) => CustomCoreHelper.NormalizeExecutableInput(raw);

    public static string ResolveExecutablePath(string? raw) => CustomCoreHelper.ResolveExecutablePath(raw);

    public static ushort ValidatePort(int port)
    {
        if (!ServerValidator.IsValidPort(port))
            throw new MessageException(i18N.Translate("Enter a port between 1 and 65535."));

        return (ushort)port;
    }

    public async Task<SocksServer> StartAsync(Server server)
    {
        ArgumentNullException.ThrowIfNull(server);
        Directory.CreateDirectory(Path.Combine(Global.NetchDir, "logging"));

        string? upstreamIp = null;
        string cliListenAddress;
        ushort cliPort;
        ushort bridgePort = 0;
        string arguments;
        string? renderedConfig = null;
        string configExt = ".json";

        if (_customCore == null)
        {
            cliPort = this.Socks5LocalPort();
            cliListenAddress = IPAddress.Loopback.ToString();
            arguments = CustomCoreHelper.NormalizeArgumentsInput(server.ProtoExtra.CliArguments);
            if (RequireLocalHttpProxy)
            {
                bridgePort = Global.Settings.Socks5LocalPort != cliPort
                    ? Global.Settings.Socks5LocalPort
                    : AllocateLoopbackPort();
            }
        }
        else
        {
            if (CustomCoreHelper.Validate(_customCore) is { } validationError)
                throw new MessageException(i18N.Translate(validationError));

            var upstream = (IPAddress.TryParse(Global.Settings.OutboundDNS, out var dns)
                    ? await DnsUtils.LookupAsync(server.Address, dns: dns.ToString(), useCache: false)
                    : null)
                ?? await DnsUtils.LookupAsync(server.Address)
                ?? throw new MessageException(i18N.Translate("Lookup Server hostname failed"));

            upstreamIp = upstream.ToString();
            if (RequireLocalHttpProxy)
            {
                cliListenAddress = IPAddress.Loopback.ToString();
                cliPort = AllocateLoopbackPort();
                bridgePort = this.Socks5LocalPort();
            }
            else
            {
                cliListenAddress = this.LocalAddress();
                cliPort = this.Socks5LocalPort();
            }

            if (!string.IsNullOrWhiteSpace(_customCore.ConfigTemplate))
            {
                configExt = CustomCoreHelper.NormalizeConfigExtension(_customCore.ConfigFileExtension);
                var dataDir = Path.Combine(Global.NetchDir, "data");
                Directory.CreateDirectory(dataDir);
                _tempConfigPath = Path.Combine(dataDir, $"custom-core-{Guid.NewGuid():N}{configExt}");
                renderedConfig = CustomCoreHelper.RenderConfigTemplate(_customCore.ConfigTemplate, configExt, server, upstreamIp, cliListenAddress, cliPort);
            }

            arguments = CustomCoreHelper.RenderArguments(_customCore.ArgumentsTemplate, server, upstreamIp, cliListenAddress, cliPort, _tempConfigPath);
        }

        var probeIp = IPAddress.TryParse(cliListenAddress, out var parsed)
            ? (parsed.Equals(IPAddress.Any) ? IPAddress.Loopback : parsed.Equals(IPAddress.IPv6Any) ? IPAddress.IPv6Loopback : parsed)
            : IPAddress.Loopback;

        try
        {
            if (renderedConfig != null && _tempConfigPath != null)
                await File.WriteAllTextAsync(_tempConfigPath, renderedConfig, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            await StartGuardAsync(arguments);
            await WaitForSocksReadyAsync(probeIp, cliPort);

            if (RequireLocalHttpProxy)
            {
                if (_customCore == null && bridgePort == Global.Settings.Socks5LocalPort)
                    MainController.TryReleaseTcpPort(bridgePort, "HTTP");

                var bridge = new MihomoController
                {
                    Socks5LocalPort = bridgePort,
                    LocalAddress = this.LocalAddress()
                };
                _httpBridge = bridge;
                var bridged = await bridge.StartAsync(new SocksServer(probeIp.ToString(), cliPort));
                return upstreamIp != null
                    ? new SocksServer(bridged.Address, (ushort)bridged.Port, upstreamIp)
                    : bridged;
            }

            var connectIp = probeIp.ToString();

            return upstreamIp != null
                ? new SocksServer(connectIp, cliPort, upstreamIp)
                : new SocksServer(connectIp, cliPort);
        }
        catch
        {
            await StopAsync();
            throw;
        }
    }

    public override async Task StopAsync()
    {
        var bridge = Interlocked.Exchange(ref _httpBridge, null);
        try
        {
            if (bridge != null)
                await bridge.StopAsync();
        }
        finally
        {
            try
            {
                if (!Instance.HasExited)
                    Instance.Kill(entireProcessTree: true);
            }
            catch (ObjectDisposedException)
            {
                // Process already disposed by an earlier StopAsync call.
            }
            catch (InvalidOperationException)
            {
                // Process was never started, already exited, or already disposed.
            }
            catch (Win32Exception)
            {
                // Process is terminating or inaccessible.
            }

            await StopGuardAsync();

            var tempConfig = Interlocked.Exchange(ref _tempConfigPath, null);
            if (!string.IsNullOrEmpty(tempConfig))
            {
                try
                {
                    if (File.Exists(tempConfig))
                        File.Delete(tempConfig);
                }
                catch (IOException)
                {
                    // Best-effort cleanup of temp config file.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup of temp config file.
                }
            }
        }
    }

    protected override void OnStartFailed() { }

    private async Task WaitForSocksReadyAsync(IPAddress ip, ushort port)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < ReadyTimeout)
        {
            if (Instance.HasExited)
                throw new MessageException(i18N.TranslateFormat("External CLI exited with code {0}. See logging\\external-cli.log.", Instance.ExitCode));

            try
            {
                await ProbeSocksHandshakeAsync(ip, port);
                if (Instance.HasExited)
                    throw new MessageException(i18N.TranslateFormat("External CLI exited with code {0}. See logging\\external-cli.log.", Instance.ExitCode));

                return;
            }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException or IOException)
            {
                if (Instance.HasExited)
                    throw new MessageException(i18N.TranslateFormat("External CLI exited with code {0}. See logging\\external-cli.log.", Instance.ExitCode));
            }

            await Task.Delay(100);
        }

        if (Instance.HasExited)
            throw new MessageException(i18N.TranslateFormat("External CLI exited with code {0}. See logging\\external-cli.log.", Instance.ExitCode));

        throw new MessageException(i18N.TranslateFormat("External CLI did not open SOCKS5 port {0} in time. See logging\\external-cli.log.", port));
    }

    private static async Task ProbeSocksHandshakeAsync(IPAddress ip, ushort port)
    {
        using var timeout = new CancellationTokenSource(500);
        using var client = new TcpClient(ip.AddressFamily);
        await client.ConnectAsync(ip, port, timeout.Token);
        var stream = client.GetStream();
        await stream.WriteAsync(new byte[] { 5, 1, 0 }, timeout.Token);
        var reply = new byte[2];
        await stream.ReadExactlyAsync(reply, timeout.Token);
        if (reply[0] != 5 || reply[1] != 0)
            throw new IOException("SOCKS5 handshake rejected.");
    }

    private static ushort AllocateLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}

using Netch.Interfaces;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.WebProxyMode;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Netch.Controllers;

public sealed class WebProxyController : IModeController
{
    private readonly ISystemProxyService _systemProxyService;
    private readonly string _stateFilePath;
    private readonly string? _baseDir;

    private PacHttpServer? _pacServer;
    private SystemProxyRecoveryState? _recoveryState;

    public WebProxyController()
        : this(WinInetSystemProxyService.Instance, SystemProxyStateStore.DefaultStatePath, null)
    {
    }

    public WebProxyController(ISystemProxyService systemProxyService, string stateFilePath, string? baseDir = null)
    {
        _systemProxyService = systemProxyService ?? throw new ArgumentNullException(nameof(systemProxyService));
        _stateFilePath = string.IsNullOrWhiteSpace(stateFilePath) ? SystemProxyStateStore.DefaultStatePath : stateFilePath;
        _baseDir = baseDir;
    }

    public string Name => "WebProxy";

    public ModeFeature Features => ModeFeature.SupportIPv6 | ModeFeature.SupportSocks5Auth | ModeFeature.RequireLocalHttpProxy;

    public string? ActivePacUrl { get; private set; }

    public Task StartAsync(SocksServer server, Mode mode)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (mode is not WebProxyMode webMode)
            throw new InvalidOperationException("Mode must be WebProxyMode.");

        try
        {
            PacScriptBuilder.ValidateMode(webMode, _baseDir);
        }
        catch (InvalidDataException ex)
        {
            throw new MessageException(ex.Message);
        }

        var localHost = NormalizeLoopbackHost(server.Address);
        var mixedPort = checked((ushort)server.Port);
        StatusPortInfoText.HttpPort = mixedPort;

        switch (webMode.Strategy)
        {
            case WebProxyStrategy.Manual:
                Log.Information("WebProxy Manual strategy active on http://{Endpoint} (system proxy unchanged)", FormatHostPort(localHost, mixedPort));
                return Task.CompletedTask;

            case WebProxyStrategy.Global:
            {
                var endpoint = FormatHostPort(localHost, mixedPort);
                var bypass = BuildGlobalBypass(webMode);
                var previous = _systemProxyService.Query();
                var state = new SystemProxyRecoveryState(previous, "Global", endpoint);
                SystemProxyStateStore.SaveState(state, _stateFilePath);
                try
                {
                    _systemProxyService.SetGlobal(endpoint, bypass);
                    _recoveryState = state;
                }
                catch
                {
                    SystemProxyStateStore.ClearState(_stateFilePath);
                    throw;
                }

                Log.Information("System proxy set to Global: {Endpoint}", endpoint);
                return Task.CompletedTask;
            }

            case WebProxyStrategy.Pac:
                return StartPacAsync(webMode, mixedPort);

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), "Unsupported web proxy strategy.");
        }
    }

    private async Task StartPacAsync(WebProxyMode webMode, ushort mixedPort)
    {
        var pacTemplate = PacScriptBuilder.BuildPacScript(webMode, "__PROXY__", _baseDir);
        var pacServer = new PacHttpServer();
        pacServer.Start(pacTemplate, mixedPort, Global.Settings.LocalAddress, Global.Settings.WebProxy.PacPort);
        _pacServer = pacServer;
        ActivePacUrl = pacServer.PacUrl;

        try
        {
            var previous = _systemProxyService.Query();
            var state = new SystemProxyRecoveryState(previous, "Pac", pacServer.PacUrl);
            SystemProxyStateStore.SaveState(state, _stateFilePath);
            _systemProxyService.SetPac(pacServer.PacUrl);
            _recoveryState = state;
        }
        catch
        {
            ActivePacUrl = null;
            _pacServer = null;
            await pacServer.StopAsync();
            pacServer.Dispose();
            SystemProxyStateStore.ClearState(_stateFilePath);
            throw;
        }

        Log.Information("System proxy set to PAC: {Url}", ActivePacUrl);
    }

    public async Task StopAsync()
    {
        var pacServer = Interlocked.Exchange(ref _pacServer, null);
        ActivePacUrl = null;
        if (pacServer != null)
        {
            try
            {
                await pacServer.StopAsync();
            }
            finally
            {
                pacServer.Dispose();
            }
        }

        var recoveryState = Interlocked.Exchange(ref _recoveryState, null);
        if (recoveryState != null)
        {
            SystemProxyStateStore.RestoreSafely(_systemProxyService, recoveryState, _stateFilePath);
        }
    }

    internal static string NormalizeLoopbackHost(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) ||
            address.Trim() is "0.0.0.0" or "::" or "[::]" or "::0" or "[::0]")
        {
            return "127.0.0.1";
        }

        return address.Trim();
    }

    internal static string FormatHostPort(string host, ushort port) =>
        host.Contains(':') && !host.StartsWith('[') ? $"[{host}]:{port}" : $"{host}:{port}";

    internal static string BuildGlobalBypass(WebProxyMode mode)
    {
        var entries = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddTokens(string? raw, params char[] separators)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;
            foreach (var part in raw.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (seen.Add(part))
                    entries.Add(part);
            }
        }

        if (mode.BypassLan)
        {
            var defaultBypass = string.IsNullOrWhiteSpace(Global.Settings.WebProxy?.DefaultBypass)
                ? WebProxyConfig.DefaultBypassValue
                : Global.Settings.WebProxy.DefaultBypass;
            AddTokens(defaultBypass, ';');
        }

        foreach (var item in mode.Bypass)
            AddTokens(item, ';');

        return string.Join(';', entries);
    }
}

using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public static class CoreSelector
{
    internal enum CoreKind
    {
        Mihomo,
        Singbox,
        Xray,
        ExternalCli
    }

    internal static CoreKind SelectCore(Server server)
    {
        return ResolveProbeCore(server).Kind;
    }

    internal static (CoreKind Kind, string? CustomExecutablePath) ResolveProbeCore(Server server)
    {
        if (server.ConfigType == EConfigType.ExternalCli)
            return (CoreKind.ExternalCli, null);

        if (CustomCoreHelper.ResolveForServer(server) is { } customCore)
        {
            var kind = customCore.Mode switch
            {
                CustomCoreMode.CliArguments => CoreKind.ExternalCli,
                CustomCoreMode.SingboxConfig => CoreKind.Singbox,
                CustomCoreMode.XrayConfig => CoreKind.Xray,
                _ => CoreKind.Mihomo
            };
            return (kind, CustomCoreHelper.NormalizeExecutableInput(customCore.ExecutablePath));
        }

        if (UsesXhttp(server))
            return (CanUseMihomoXhttp(server) ? CoreKind.Mihomo : CoreKind.Xray, null);

        return (UsesSingbox(server) ? CoreKind.Singbox : CoreKind.Mihomo, null);
    }

    public static IServerController CreateController(Server server)
    {
        if (server.ConfigType == EConfigType.ExternalCli)
        {
            return server is ExternalCliServer externalCli
                ? new ExternalCliController(externalCli)
                : throw new MessageException("Invalid External CLI server configuration.");
        }

        if (CustomCoreHelper.ResolveForServer(server) is { } customCore)
        {
            return customCore.Mode switch
            {
                CustomCoreMode.CliArguments => new ExternalCliController(customCore),
                CustomCoreMode.SingboxConfig => new SingboxController(customCore.ExecutablePath),
                CustomCoreMode.XrayConfig => new XrayController(customCore.ExecutablePath),
                _ => new MihomoController(customCore.ExecutablePath)
            };
        }

        return SelectCore(server) switch
        {
            CoreKind.Xray => new XrayController(),
            CoreKind.Singbox => new SingboxController(),
            _ => new MihomoController()
        };
    }

    private static bool CanUseMihomoXhttp(Server server)
    {
        return server.ConfigType == EConfigType.VLESS &&
            (server.Extra.IsNullOrWhiteSpace() || server.Extra.Trim() == "{}") &&
            (server.HeaderType is null or "" or "none" || Constants.XhttpMode.Contains(server.HeaderType)) &&
            server.StreamSecurity?.Trim().ToLowerInvariant() is null or "" or "none" or "tls" &&
            server.Cert.IsNullOrEmpty() && server.CertSha.IsNullOrEmpty() &&
            server.EchConfigList.IsNullOrEmpty() && server.EchForceQuery is null or "" or "none" &&
            server.GetAlpn()?.All(value => value is "h2" or "http/1.1") != false &&
            server.ProtoExtra.Flow is null or "" or "none" &&
            server.ProtoExtra.VlessEncryption is null or "" or "none" &&
            !(server.MuxEnabled ?? Global.Settings.V2RayConfig.CoreBasicItem.MuxEnabled);
    }

    public static bool UsesXhttp(Server server)
    {
        return server.ConfigType is EConfigType.VMess or EConfigType.VLESS or EConfigType.Trojan or EConfigType.Shadowsocks
            && string.Equals(server.Network?.Trim(), nameof(ETransport.xhttp), StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesSingbox(Server server)
    {
        if (server.ConfigType is EConfigType.TrustTunnel or EConfigType.Mieru or EConfigType.ShadowQUIC or EConfigType.AmneziaWG or EConfigType.ExternalCli)
            return false;

        var network = server.Network?.Trim().ToLowerInvariant();
        var security = server.StreamSecurity?.Trim().ToLowerInvariant();
        if (server.ConfigType is EConfigType.Naive or EConfigType.MASQUE ||
            (server.Cert.IsNotEmpty() && server is not (SSHServer or ShadowsocksRServer or SocksServer or WireGuardServer)) ||
            (server is SocksServer && server.ProtoExtra.Uot == true) ||
            (network == "quic" && server is VLESSServer or VMessServer or TrojanServer))
            return true;

        return server switch
        {
            TrojanServer => (network is "http" or "h2") || (security is null or "" or "none"),
            VMessServer => (network is "http" or "h2") && (security is null or "" or "none"),
            HttpServer => SingboxController.BuildHeaders(server.ProtoExtra.HttpHeaders).Values.Any(values => values.Count > 1)
                || server.GetAlpn() is { Count: > 0 } || server.Fingerprint.IsNotEmpty() && server.Fingerprint != "none",
            Hysteria2Server => server.ProtoExtra.HopIntervalMax.IsNotEmpty()
                || server.ProtoExtra.BbrProfile.IsNotEmpty()
                || (server.ProtoExtra.Ports.IsNotEmpty() && server.ProtoExtra.HopInterval.IsNotEmpty() &&
                    (!Hysteria2Util.TryParseHopSeconds(server.ProtoExtra.HopInterval, out var seconds) || seconds < 5)),
            WireGuardServer => (Utils.Utils.String2List(server.ProtoExtra.WgInterfaceAddress) ?? [])
                .GroupBy(address => address.Contains(':')).Any(family => family.Count() > 1),
            ShadowsocksServer => server.ProtoExtra.SsPluginOptions?.Split(';', StringSplitOptions.TrimEntries)
                .Contains("mode=quic") == true,
            ShadowTLSServer => server.ProtoExtra.ShadowTlsVersion == 1,
            SSHServer => server.ProtoExtra.SshClientVersion.IsNotEmpty(),
            _ => false
        };
    }
}

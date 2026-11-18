using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public static class CoreSelector
{
    public static IServerController CreateController(Server server)
    {
        if (UsesXhttp(server))
            return CanUseMihomoXhttp(server) ? new MihomoController() : new XrayController();
        return UsesSingbox(server) ? new SingboxController() : new MihomoController();
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
        var network = server.Network?.Trim().ToLowerInvariant();
        var security = server.StreamSecurity?.Trim().ToLowerInvariant();
        if (server.ConfigType is EConfigType.Naive or EConfigType.MASQUE ||
            server.Cert.IsNotEmpty() ||
            server is SocksServer && server.ProtoExtra.Uot == true ||
            network == "quic")
            return true;

        return server switch
        {
            TrojanServer => (network is "http" or "h2") || (security is null or "" or "none"),
            VMessServer => (network is "http" or "h2") && (security is null or "" or "none"),
            HttpServer => SingboxController.BuildHeaders(server.ProtoExtra.HttpHeaders).Values.Any(values => values.Count > 1)
                || server.GetAlpn() is { Count: > 0 } || server.Fingerprint.IsNotEmpty() && server.Fingerprint != "none",
            Hysteria2Server => server.ProtoExtra.Ports.IsNotEmpty() && server.ProtoExtra.HopInterval.IsNotEmpty() &&
                (!uint.TryParse(server.ProtoExtra.HopInterval.TrimEnd('s'), out var seconds) || seconds < 5),
            WireGuardServer => (Utils.Utils.String2List(server.ProtoExtra.WgInterfaceAddress) ?? [])
                .GroupBy(address => address.Contains(':')).Any(family => family.Count() > 1),
            ShadowsocksServer => server.ProtoExtra.SsPluginOptions?.Split(';', StringSplitOptions.TrimEntries)
                .Contains("mode=quic") == true,
            _ => false
        };
    }
}

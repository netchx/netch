using System.Globalization;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Utils;

/// <summary>
///     Shared node-editor defaults, normalization, and validation rules.
/// </summary>
public static class ServerEditorHelper
{
    private static readonly string[] WizardFeaturedProtocolNames =
    [
        nameof(EConfigType.SOCKS),
        nameof(EConfigType.Shadowsocks),
        nameof(EConfigType.ShadowsocksR),
        nameof(EConfigType.AnyTLS),
        nameof(EConfigType.VLESS)
    ];

    public static IReadOnlyList<IServerUtil> GetOrderedProtocols() =>
        ServerHelper.ServerUtilDictionary.Values
            .OrderBy(util => util.Priority)
            .ThenBy(util => util.TypeName, StringComparer.Ordinal)
            .ToArray();

    public static (IReadOnlyList<IServerUtil> Featured, IReadOnlyList<IServerUtil> More) GetWizardManualProtocols()
    {
        var ordered = GetOrderedProtocols();
        var byName = ordered.ToDictionary(util => util.TypeName, StringComparer.OrdinalIgnoreCase);
        var featured = new List<IServerUtil>(WizardFeaturedProtocolNames.Length);
        var featuredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in WizardFeaturedProtocolNames)
        {
            if (byName.TryGetValue(name, out var util) && featuredSet.Add(util.TypeName))
                featured.Add(util);
        }

        var more = ordered.Where(util => !featuredSet.Contains(util.TypeName)).ToArray();
        return (featured, more);
    }

    public static Server CreateDefault(IServerUtil util)
    {
        ArgumentNullException.ThrowIfNull(util);
        if (Activator.CreateInstance(util.ServerType) is not Server server)
            throw new InvalidOperationException($"Could not create server for {util.TypeName}.");

        switch (server.ConfigType)
        {
            case EConfigType.Trojan:
                server.StreamSecurity = Constants.StreamSecurity;
                server.Network = Constants.DefaultNetwork;
                break;
            case EConfigType.VLESS:
                server.MuxEnabled = false;
                server.Network = Constants.DefaultNetwork;
                server.ProtoExtra.VlessEncryption = Constants.None;
                break;
            case EConfigType.VMess:
                server.MuxEnabled = false;
                server.Network = Constants.DefaultNetwork;
                server.ProtoExtra.VmessSecurity = Constants.DefaultSecurity;
                server.ProtoExtra.AlterId = 0;
                break;
            case EConfigType.Hysteria2:
                server.StreamSecurity = Constants.StreamSecurity;
                server.ProtoExtra.Obfs = Constants.None;
                break;
            case EConfigType.Shadowsocks:
                server.ProtoExtra.SsMethod = Constants.SsCiphers[0];
                break;
            case EConfigType.ShadowTLS:
                server.StreamSecurity = Constants.StreamSecurity;
                server.ProtoExtra.SsMethod = Constants.SsCiphers[0];
                server.ProtoExtra.ShadowTlsVersion = 3;
                break;
            case EConfigType.SSH:
                server.Port = 22;
                break;
            case EConfigType.TrustTunnel:
                server.Port = 443;
                server.StreamSecurity = Constants.StreamSecurity;
                server.ProtoExtra.TrustTunnelHealthCheck = true;
                break;
            case EConfigType.Mieru:
                server.ProtoExtra.MieruTransport = "TCP";
                server.ProtoExtra.MieruMultiplexing = "MULTIPLEXING_LOW";
                server.ProtoExtra.MieruHandshakeMode = "HANDSHAKE_STANDARD";
                break;
            case EConfigType.ShadowQUIC:
                server.Port = 443;
                server.Alpn = "h3";
                server.ProtoExtra.ShadowQuicVersions = "v1";
                server.ProtoExtra.CongestionControl = "cubic";
                break;
            case EConfigType.AmneziaWG:
                server.ProtoExtra.WgMtu = 1280;
                break;
            case EConfigType.ExternalCli:
                server.Address = "127.0.0.1";
                server.Port = 1080;
                break;
        }

        return server;
    }

    public static void ResetShadowsocksTransport(Server server)
    {
        ArgumentNullException.ThrowIfNull(server);
        server.Network = Constants.DefaultNetwork;
        server.HeaderType = string.Empty;
        server.RequestHost = string.Empty;
        server.Path = string.Empty;
        server.StreamSecurity = Constants.None;
    }

    public static void PrepareForSave(Server server, bool resetShadowsocksTransport = false)
    {
        ArgumentNullException.ThrowIfNull(server);
        var extra = server.ProtoExtra;
        var protocol = server.ConfigType;

        switch (protocol)
        {
            case EConfigType.Hysteria2:
                server.StreamSecurity = Constants.StreamSecurity;
                if (extra.Obfs == Constants.None)
                    extra.SalamanderPass = string.Empty;
                if (extra.Obfs != "gecko")
                {
                    extra.GeckoMinPacketSize = string.Empty;
                    extra.GeckoMaxPacketSize = string.Empty;
                }
                if (string.IsNullOrWhiteSpace(extra.Ports))
                {
                    extra.HopInterval = string.Empty;
                    extra.HopIntervalMax = string.Empty;
                }
                extra.BbrProfile = extra.BbrProfile?.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(server.Cert))
                    server.CertSha = string.Empty;
                break;

            case EConfigType.Naive:
                server.StreamSecurity = Constants.StreamSecurity;
                if (extra.NaiveQuic != true)
                    extra.CongestionControl = string.Empty;
                break;

            case EConfigType.TUIC:
            case EConfigType.AnyTLS:
                server.StreamSecurity = Constants.StreamSecurity;
                break;

            case EConfigType.MASQUE:
                server.StreamSecurity = Constants.StreamSecurity;
                NormalizeMultilineHeaders(extra.MasqueHeaders, value => extra.MasqueHeaders = value);
                break;

            case EConfigType.ShadowTLS:
                server.StreamSecurity = Constants.StreamSecurity;
                if (extra.ShadowTlsVersion is not (1 or 2 or 3))
                    extra.ShadowTlsVersion = 3;
                if (extra.ShadowTlsVersion == 1)
                    extra.ShadowTlsPassword = string.Empty;
                if (!string.IsNullOrEmpty(server.Cert))
                    server.CertSha = string.Empty;
                break;

            case EConfigType.SSH:
                server.StreamSecurity = string.Empty;
                server.Cert = string.Empty;
                server.CertSha = string.Empty;
                server.Sni = string.Empty;
                server.Alpn = string.Empty;
                server.Fingerprint = string.Empty;
                server.AllowInsecure = null;
                if (!string.IsNullOrWhiteSpace(extra.SshPrivateKey))
                    extra.SshPrivateKey = ServerValidator.NormalizeSshPrivateKey(extra.SshPrivateKey);
                break;

            case EConfigType.HTTP:
                NormalizeMultilineHeaders(extra.HttpHeaders, value => extra.HttpHeaders = value);
                break;

            case EConfigType.Shadowsocks:
                if (extra.SsPlugin != "v2ray-plugin")
                    server.AllowInsecure = null;
                if (resetShadowsocksTransport)
                    ResetShadowsocksTransport(server);
                break;

            case EConfigType.VLESS:
                if (string.IsNullOrWhiteSpace(extra.VlessEncryption))
                    extra.VlessEncryption = Constants.None;
                if (server.StreamSecurity is not (Constants.StreamSecurity or Constants.StreamSecurityReality) ||
                    server.GetNetwork() is not nameof(ETransport.tcp))
                    extra.Flow = string.Empty;
                break;

            case EConfigType.TrustTunnel:
                server.StreamSecurity = Constants.StreamSecurity;
                if (extra.TrustTunnelQuic != true)
                {
                    extra.CongestionControl = string.Empty;
                    extra.BbrProfile = string.Empty;
                }
                else
                {
                    extra.CongestionControl = extra.CongestionControl?.Trim().ToLowerInvariant();
                    extra.BbrProfile = extra.BbrProfile?.Trim().ToLowerInvariant();
                    if (extra.CongestionControl != "bbr")
                        extra.BbrProfile = string.Empty;
                }
                if (!string.IsNullOrWhiteSpace(server.Cert))
                    server.CertSha = string.Empty;
                break;

            case EConfigType.Mieru:
                ClearTlsFields(server);
                extra.MieruTransport = string.IsNullOrWhiteSpace(extra.MieruTransport)
                    ? "TCP"
                    : extra.MieruTransport.Trim().ToUpperInvariant();
                extra.MieruMultiplexing = extra.MieruMultiplexing?.Trim().ToUpperInvariant();
                extra.MieruHandshakeMode = extra.MieruHandshakeMode?.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(extra.MieruPortRange))
                {
                    extra.MieruPortRange = extra.MieruPortRange.Trim().Replace(':', '-');
                    if (ServerValidator.IsValidSinglePortRange(extra.MieruPortRange, out var startPort, out var endPort))
                    {
                        extra.MieruPortRange = $"{startPort.ToString(CultureInfo.InvariantCulture)}-{endPort.ToString(CultureInfo.InvariantCulture)}";
                        server.Port = startPort;
                    }
                }
                break;

            case EConfigType.ShadowQUIC:
                server.StreamSecurity = string.Empty;
                server.Cert = string.Empty;
                server.CertSha = string.Empty;
                server.Fingerprint = string.Empty;
                server.AllowInsecure = null;
                server.EchConfigList = string.Empty;
                server.EchForceQuery = string.Empty;
                if (!string.IsNullOrWhiteSpace(extra.ShadowQuicVersions))
                {
                    extra.ShadowQuicVersions = string.Join(",",
                        extra.ShadowQuicVersions
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(v => v.ToLowerInvariant())
                            .Distinct(StringComparer.Ordinal));
                }
                extra.CongestionControl = extra.CongestionControl?.Trim().ToLowerInvariant();
                extra.BbrProfile = extra.BbrProfile?.Trim().ToLowerInvariant();
                if (extra.CongestionControl != "bbr")
                    extra.BbrProfile = string.Empty;
                break;

            case EConfigType.AmneziaWG:
                ClearTlsFields(server);
                if (ServerValidator.TryNormalizeWireGuardReserved(extra.WgReserved, out var normalizedAwgReserved))
                    extra.WgReserved = normalizedAwgReserved;
                if (extra.AwgVersion == null && HasAwgV3Options(extra))
                    extra.AwgVersion = 3;
                if (extra.AwgVersion != 3)
                    ClearAwgV3Options(extra);
                break;

            case EConfigType.ExternalCli:
                ClearTlsFields(server);
                if (string.IsNullOrWhiteSpace(server.Address))
                    server.Address = "127.0.0.1";
                extra.CliExecutable = CustomCoreHelper.NormalizeExecutableInput(extra.CliExecutable);
                extra.CliArguments = CustomCoreHelper.NormalizeArgumentsInput(extra.CliArguments);
                extra.CustomCoreId = null;
                break;
        }

        if (protocol is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan &&
            server.GetNetwork() != nameof(ETransport.xhttp))
            server.Extra = string.Empty;

        if (protocol is not (EConfigType.WireGuard or EConfigType.AmneziaWG or EConfigType.ShadowsocksR or EConfigType.SOCKS or EConfigType.Shadowsocks or EConfigType.SSH or EConfigType.Mieru or EConfigType.ShadowQUIC or EConfigType.ExternalCli) &&
            server.StreamSecurity != Constants.StreamSecurity)
        {
            server.Cert = string.Empty;
            server.CertSha = string.Empty;
            server.EchConfigList = string.Empty;
            server.EchForceQuery = string.Empty;
        }

        if (protocol is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan &&
            server.StreamSecurity != Constants.StreamSecurityReality)
        {
            server.PublicKey = string.Empty;
            server.ShortId = string.Empty;
            server.SpiderX = string.Empty;
            server.Mldsa65Verify = string.Empty;
        }

        if (protocol is EConfigType.HTTP or EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan &&
            server.StreamSecurity is not (Constants.StreamSecurity or Constants.StreamSecurityReality))
        {
            server.Sni = string.Empty;
            server.Alpn = string.Empty;
            server.Fingerprint = string.Empty;
            server.AllowInsecure = null;
        }
    }

    public static bool HasAwgV3Options(ProtocolExtraItem extra) =>
        !string.IsNullOrWhiteSpace(extra.AwgHeaderProtectionKey) ||
        !string.IsNullOrWhiteSpace(extra.AwgContentPaddingAddition) ||
        !string.IsNullOrWhiteSpace(extra.AwgRekeyAfterTime) ||
        !string.IsNullOrWhiteSpace(extra.AwgRekeyTimeout) ||
        !string.IsNullOrWhiteSpace(extra.AwgRejectAfterTime) ||
        !string.IsNullOrWhiteSpace(extra.AwgKeepaliveTimeout) ||
        !string.IsNullOrWhiteSpace(extra.AwgMaxHandshakeAttempts) ||
        extra.AwgRandomTrailers == true ||
        extra.AwgDisableCookies == true;

    public static void ClearAwgV3Options(ProtocolExtraItem extra)
    {
        extra.AwgHeaderProtectionKey = string.Empty;
        extra.AwgContentPaddingAddition = string.Empty;
        extra.AwgRekeyAfterTime = string.Empty;
        extra.AwgRekeyTimeout = string.Empty;
        extra.AwgRejectAfterTime = string.Empty;
        extra.AwgKeepaliveTimeout = string.Empty;
        extra.AwgMaxHandshakeAttempts = string.Empty;
        extra.AwgRandomTrailers = null;
        extra.AwgDisableCookies = null;
    }

    private static void ClearTlsFields(Server server)
    {
        server.StreamSecurity = string.Empty;
        server.Cert = string.Empty;
        server.CertSha = string.Empty;
        server.Sni = string.Empty;
        server.Alpn = string.Empty;
        server.Fingerprint = string.Empty;
        server.AllowInsecure = null;
        server.EchConfigList = string.Empty;
        server.EchForceQuery = string.Empty;
    }

    public static string? ValidateForSave(Server server, IServerUtil util)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(util);
        if (ServerValidator.Validate(server) is { } validationError)
            return validationError;

        return util.CheckServer(server) ? null : "Check the required protocol fields.";
    }

    private static void NormalizeMultilineHeaders(string? headers, Action<string> assign)
    {
        if (!string.IsNullOrWhiteSpace(headers) && !headers.Contains('\n') && !headers.Contains('\r'))
            assign(headers.Trim() + "\n");
    }
}

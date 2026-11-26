using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Netch.Enums;
using Netch.Manager;
using Netch.Models;
using Netch.Servers;

namespace Netch.Utils;

public static class ServerValidator
{
    public static bool IsValidPort(int port) => port is > 0 and <= ushort.MaxValue;

    public static string NormalizeSshPrivateKey(string rawKey)
    {
        var trimmed = rawKey.Trim();
        if (!trimmed.Contains('\n') && !trimmed.Contains('\r') && trimmed.Contains("\\n", StringComparison.Ordinal))
            trimmed = trimmed.Replace("\\n", "\n", StringComparison.Ordinal);
        return trimmed.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    public static bool IsValidSshPrivateKey(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = NormalizeSshPrivateKey(text);
        var beginIdx = normalized.IndexOf("-----BEGIN ", StringComparison.Ordinal);
        if (beginIdx < 0)
            return false;
        return normalized.IndexOf("-----END ", beginIdx, StringComparison.Ordinal) > beginIdx;
    }

    internal static List<string>? SplitSshValues(string? rawValues)
    {
        if (string.IsNullOrWhiteSpace(rawValues))
            return null;

        var list = rawValues
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
        return list.Count > 0 ? list : null;
    }

    public static bool IsValidWireGuardKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        Span<byte> buffer = stackalloc byte[32];
        return Convert.TryFromBase64String(value.Trim(), buffer, out var length) && length == 32;
    }

    public static bool IsValidWireGuardAddresses(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var items = value.Split(',');
        return items.All(address =>
        {
            var trimmed = address.Trim();
            return trimmed.Length > 0 &&
                (IPAddress.TryParse(trimmed, out _) || RouteUtils.TryParseIPNetwork(trimmed, out _, out _));
        });
    }

    public static bool IsValidWireGuardReserved(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var values = value.Split(',');
        return values.Length == 3 && values.All(item => byte.TryParse(item.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
    }

    public static bool TryNormalizeWireGuardReserved(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        var parts = trimmed.Split(',');
        if (parts.Length == 3 && parts.All(item => byte.TryParse(item.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
        {
            normalized = string.Join(",", parts.Select(item => item.Trim()));
            return true;
        }

        Span<byte> bytes = stackalloc byte[3];
        if (Convert.TryFromBase64String(trimmed, bytes, out var length) && length == 3)
        {
            normalized = $"{bytes[0]},{bytes[1]},{bytes[2]}";
            return true;
        }

        return false;
    }

    public static bool IsValidShadowsocksPassword(string? cipher, string? password)
    {
        if (cipher is "none" or "plain")
            return true;
        if (string.IsNullOrWhiteSpace(password))
            return false;
        if (cipher == null || !cipher.StartsWith("2022-", StringComparison.OrdinalIgnoreCase))
            return true;

        var length = cipher.Equals("2022-blake3-aes-128-gcm", StringComparison.OrdinalIgnoreCase) ? 16 : 32;
        Span<byte> buffer = stackalloc byte[32];
        foreach (var key in password.Split(':'))
        {
            if (!Convert.TryFromBase64String(key, buffer, out var count) || count != length)
                return false;
        }
        return true;
    }

    public static bool IsValidPortRange(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        return text.Split(',').All(entry =>
        {
            var range = entry.Trim().Replace(':', '-').Split('-');
            return ushort.TryParse(range[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var start) && start > 0 &&
                (range.Length == 1 ||
                    (range.Length == 2 &&
                        ushort.TryParse(range[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var end) &&
                        end >= start));
        });
    }

    public static bool IsValidSinglePortRange(string? text, out int startPort, out int endPort)
    {
        startPort = 0;
        endPort = 0;
        if (string.IsNullOrWhiteSpace(text) || text.Contains(','))
            return false;

        var parts = text.Trim().Replace(':', '-').Split('-');
        if (parts.Length != 2)
            return false;

        if (!ushort.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var start) ||
            !ushort.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var end) ||
            start == 0 || end < start)
        {
            return false;
        }

        startPort = start;
        endPort = end;
        return true;
    }

    public static bool IsValidBase64(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var trimmed = text.Trim();
        var buffer = new byte[(trimmed.Length * 3 / 4) + 4];
        return Convert.TryFromBase64String(trimmed, buffer, out _);
    }

    public static bool IsValidAwgHeaderValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var trimmed = text.Trim();
        var dash = trimmed.IndexOf('-');
        if (dash < 0)
            return uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

        var minStr = trimmed[..dash].Trim();
        var maxStr = trimmed[(dash + 1)..].Trim();
        return uint.TryParse(minStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var min)
            && uint.TryParse(maxStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var max)
            && min <= max;
    }

    public static bool IsValidAwgPaddingRange(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var trimmed = text.Trim();
        var dash = trimmed.IndexOf('-');
        if (dash < 0)
            return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var single) && single >= 0;

        var minStr = trimmed[..dash].Trim();
        var maxStr = trimmed[(dash + 1)..].Trim();
        return int.TryParse(minStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var min)
            && int.TryParse(maxStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var max)
            && min >= 0 && min <= max;
    }

    public static bool HasDuplicateWireGuardAddressFamily(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var ipv4Count = 0;
        var ipv6Count = 0;
        foreach (var entry in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var ipPart = entry.Split('/')[0].Trim();
            if (!IPAddress.TryParse(ipPart, out var ip))
                continue;
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                ipv4Count++;
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                ipv6Count++;
        }

        return ipv4Count > 1 || ipv6Count > 1;
    }

    public static bool IsValidHeaders(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        foreach (var line in text.GetLines())
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var colon = line.IndexOf(':');
            var name = colon > 0 ? line[..colon].Trim() : string.Empty;
            if (name.Length == 0 || !name.All(c => char.IsAsciiLetterOrDigit(c) || "!#$%&'*+-.^_`|~".Contains(c)))
                return false;
        }

        return true;
    }

    public static bool IsValidPemChain(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var certificates = CertPemManager.ParsePemChain(text);
        if (certificates.Count == 0)
            return false;

        try
        {
            foreach (var pem in certificates)
            {
                using var certificate = X509Certificate2.CreateFromPem(pem);
            }
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public static bool IsValidJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool SupportsReality(EConfigType type, string? network)
    {
        var normalized = string.IsNullOrWhiteSpace(network) ? Constants.DefaultNetwork : network.Trim().ToLowerInvariant();
        return normalized is "tcp" or "raw" or "grpc" or "xhttp" ||
            (normalized is "http" or "h2" && type is EConfigType.VLESS or EConfigType.VMess);
    }

    public static string? Validate(Server server)
    {
        if (server.ConfigType == EConfigType.Mieru &&
            !string.IsNullOrWhiteSpace(server.ProtoExtra.MieruPortRange) &&
            !IsValidSinglePortRange(server.ProtoExtra.MieruPortRange, out _, out _))
            return "Mieru port range must be a single start-end range.";

        if (string.IsNullOrWhiteSpace(server.Address) || !IsValidPort(server.Port))
            return "Enter an address and a valid port.";

        var extra = server.ProtoExtra;
        switch (server.ConfigType)
        {
            case EConfigType.SOCKS:
                if (string.IsNullOrWhiteSpace(server.Username) != string.IsNullOrWhiteSpace(server.Password))
                    return "Enter both a SOCKS username and password, or leave both empty.";
                break;

            case EConfigType.HTTP:
                if (!IsValidHeaders(extra.HttpHeaders))
                    return "HTTP headers must use 'Name: Value' format.";
                break;

            case EConfigType.VLESS:
                if (string.IsNullOrWhiteSpace(server.Password))
                    return "Check the required protocol fields.";
                if (!string.IsNullOrWhiteSpace(extra.VlessEncryption))
                {
                    var enc = extra.VlessEncryption.Trim();
                    if (enc != Constants.None && enc.Split('.') is not ["mlkem768x25519plus", _, _, _, ..])
                        return "Invalid VLESS encryption setting.";
                }
                break;

            case EConfigType.VMess:
                if (string.IsNullOrWhiteSpace(server.Password))
                    return "Check the required protocol fields.";
                if (extra.AlterId is < 0 or > ushort.MaxValue)
                    return "Check the required protocol fields.";
                break;

            case EConfigType.Trojan:
                if (string.IsNullOrWhiteSpace(server.Password))
                    return "Check the required protocol fields.";
                break;

            case EConfigType.Shadowsocks:
                if (!ShadowsocksUtil.IsSupportedCipher(extra.SsMethod) ||
                    !IsValidShadowsocksPassword(extra.SsMethod, server.Password))
                    return "Check the Shadowsocks encryption method and password.";
                break;

            case EConfigType.ShadowsocksR:
                if (server is not ShadowsocksRServer ssr ||
                    string.IsNullOrWhiteSpace(ssr.Password) ||
                    string.IsNullOrWhiteSpace(ssr.EncryptMethod) ||
                    string.IsNullOrWhiteSpace(ssr.Protocol) ||
                    string.IsNullOrWhiteSpace(ssr.OBFS))
                    return "Check the required protocol fields.";
                break;

            case EConfigType.WireGuard:
                if (ValidateWireGuardCore(server) is { } wgError)
                    return wgError;
                break;

            case EConfigType.AmneziaWG:
                if (ValidateWireGuardCore(server) is { } awgCoreError)
                    return awgCoreError;
                if (HasDuplicateWireGuardAddressFamily(extra.WgInterfaceAddress))
                    return "AmneziaWG supports at most one IPv4 and one IPv6 local address.";
                if (extra.AwgVersion is not (null or 1 or 2 or 3))
                    return "Check the required protocol fields.";
                if (extra.AwgJc is < 0 || extra.AwgJmin is < 0 || extra.AwgJmax is < 0 ||
                    extra.AwgS1 is < 0 || extra.AwgS2 is < 0 || extra.AwgS3 is < 0 || extra.AwgS4 is < 0)
                    return "Check the required protocol fields.";
                if (extra.AwgJmin is { } jmin && extra.AwgJmax is { } jmax && jmin > jmax)
                    return "AmneziaWG Jmin cannot exceed Jmax.";
                if (!IsValidAwgHeaderValue(extra.AwgH1) || !IsValidAwgHeaderValue(extra.AwgH2) ||
                    !IsValidAwgHeaderValue(extra.AwgH3) || !IsValidAwgHeaderValue(extra.AwgH4))
                    return "AmneziaWG H1-H4 must be unsigned integers or min-max ranges.";
                if (!string.IsNullOrWhiteSpace(extra.AwgHeaderProtectionKey) &&
                    !IsValidWireGuardKey(extra.AwgHeaderProtectionKey))
                    return "Enter valid Base64 WireGuard keys.";
                if (!IsValidAwgPaddingRange(extra.AwgContentPaddingAddition))
                    return "AmneziaWG content padding addition must be a non-negative integer or min-max range.";
                break;

            case EConfigType.Hysteria2:
                if (extra.Obfs is "salamander" or "gecko" && string.IsNullOrEmpty(extra.SalamanderPass))
                    return "Enter an obfuscation password.";
                if (extra.Obfs == "gecko")
                {
                    int? minSize = null;
                    int? maxSize = null;
                    if (!string.IsNullOrWhiteSpace(extra.GeckoMinPacketSize))
                    {
                        if (!int.TryParse(extra.GeckoMinPacketSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out var min) || min <= 0)
                            return "Gecko packet sizes must be positive integers.";
                        minSize = min;
                    }
                    if (!string.IsNullOrWhiteSpace(extra.GeckoMaxPacketSize))
                    {
                        if (!int.TryParse(extra.GeckoMaxPacketSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out var max) || max <= 0)
                            return "Gecko packet sizes must be positive integers.";
                        maxSize = max;
                    }
                    if (minSize.HasValue && maxSize.HasValue && minSize > maxSize)
                        return "Gecko min packet size cannot exceed max packet size.";
                }
                if (!IsValidPortRange(extra.Ports))
                    return "Port hopping range is invalid.";
                uint parsedHopMin = 0;
                var hasHopMin = !string.IsNullOrWhiteSpace(extra.Ports) && !string.IsNullOrWhiteSpace(extra.HopInterval);
                if (hasHopMin && (!Hysteria2Util.TryParseHopSeconds(extra.HopInterval, out parsedHopMin) || parsedHopMin < 5))
                    return "Hop interval must be at least 5 seconds.";
                if (!string.IsNullOrWhiteSpace(extra.HopIntervalMax))
                {
                    if (string.IsNullOrWhiteSpace(extra.Ports))
                        return "Maximum hop interval requires a port hopping range.";
                    if (!Hysteria2Util.TryParseHopSeconds(extra.HopIntervalMax, out var maxInterval) || maxInterval < 5)
                        return "Hop interval must be at least 5 seconds.";
                    if (hasHopMin && maxInterval < parsedHopMin)
                        return "Maximum hop interval cannot be smaller than minimum hop interval.";
                }
                if (!string.IsNullOrWhiteSpace(extra.BbrProfile) &&
                    !Constants.Hysteria2BbrProfiles.Contains(extra.BbrProfile.Trim().ToLowerInvariant()))
                    return "BBR profile must be standard, conservative, or aggressive.";
                if (extra.UpMbps is < 0 || extra.DownMbps is < 0)
                    return "Check the required protocol fields.";
                break;

            case EConfigType.ShadowTLS:
                if (server.StreamSecurity == Constants.StreamSecurityReality)
                    return "ShadowTLS does not support REALITY.";
                if (!ShadowsocksUtil.IsSupportedCipher(extra.SsMethod) ||
                    !IsValidShadowsocksPassword(extra.SsMethod, server.Password))
                    return "Check the Shadowsocks encryption method and password.";
                if (extra.ShadowTlsVersion is not (1 or 2 or 3) ||
                    (extra.ShadowTlsVersion >= 2 && string.IsNullOrEmpty(extra.ShadowTlsPassword)))
                    return "Check the required protocol fields.";
                break;

            case EConfigType.SSH:
                if (string.IsNullOrWhiteSpace(server.Username) ||
                    (string.IsNullOrEmpty(server.Password) && string.IsNullOrWhiteSpace(extra.SshPrivateKey)))
                    return "Enter an SSH username and either a password or private key.";
                if (!string.IsNullOrWhiteSpace(extra.SshPrivateKey) && !IsValidSshPrivateKey(extra.SshPrivateKey))
                    return "SSH private key must be inline PEM text.";
                break;

            case EConfigType.TUIC:
                if (!Guid.TryParse(server.Username, out var tuicUuid) || tuicUuid == Guid.Empty || string.IsNullOrEmpty(server.Password))
                    return "Enter a valid TUIC UUID and password.";
                break;

            case EConfigType.AnyTLS:
                if (string.IsNullOrEmpty(server.Password) ||
                    extra.AnyTlsIdleSessionCheckIntervalSeconds is < 0 ||
                    extra.AnyTlsIdleSessionTimeoutSeconds is < 0 ||
                    extra.AnyTlsMinIdleSession is < 0)
                    return "Check the required protocol fields.";
                break;

            case EConfigType.Naive:
                if (string.IsNullOrEmpty(server.Password) || extra.InsecureConcurrency is < 0)
                    return "Check the required protocol fields.";
                break;

            case EConfigType.MASQUE:
                if (!string.IsNullOrWhiteSpace(extra.MasquePath) && !extra.MasquePath.Trim().StartsWith('/'))
                    return "URI template must start with '/'.";
                if (!IsValidHeaders(extra.MasqueHeaders))
                    return "HTTP headers must use 'Name: Value' format.";
                break;

            case EConfigType.TrustTunnel:
                var trustTunnelSecurity = server.StreamSecurity?.Trim().ToLowerInvariant();
                if (trustTunnelSecurity == Constants.StreamSecurityReality)
                    return "TrustTunnel does not support REALITY.";
                if (trustTunnelSecurity is not (null or "" or Constants.None or Constants.StreamSecurity))
                    return "Check the required protocol fields.";
                if (!string.IsNullOrWhiteSpace(server.EchForceQuery) &&
                    !string.Equals(server.EchForceQuery.Trim(), Constants.None, StringComparison.OrdinalIgnoreCase))
                    return "Mihomo cannot preserve this custom ECH configuration.";
                if (extra.TrustTunnelMaxConnections is < 0 || extra.TrustTunnelMinStreams is < 0 || extra.TrustTunnelMaxStreams is < 0)
                    return "Check the required protocol fields.";
                if (extra.TrustTunnelMaxConnections is > 0 && extra.TrustTunnelMaxStreams is > 0)
                    return "Max connections and max streams cannot both be set.";
                if (!string.IsNullOrWhiteSpace(extra.CongestionControl) &&
                    !Constants.TrustTunnelCongestionControls.Contains(extra.CongestionControl.Trim().ToLowerInvariant()))
                    return "Check the required protocol fields.";
                if (!string.IsNullOrWhiteSpace(extra.BbrProfile) &&
                    !Constants.Hysteria2BbrProfiles.Contains(extra.BbrProfile.Trim().ToLowerInvariant()))
                    return "BBR profile must be standard, conservative, or aggressive.";
                if (!string.IsNullOrWhiteSpace(server.EchConfigList) && !IsValidBase64(server.EchConfigList))
                    return "ECH configuration must be valid Base64.";
                break;

            case EConfigType.Mieru:
                if (string.IsNullOrWhiteSpace(server.Username) || string.IsNullOrEmpty(server.Password))
                    return "Check the required protocol fields.";
                if (string.IsNullOrWhiteSpace(extra.MieruTransport) ||
                    !Constants.MieruTransports.Contains(extra.MieruTransport.Trim(), StringComparer.OrdinalIgnoreCase))
                    return "Mieru transport must be TCP or UDP.";
                if (!string.IsNullOrWhiteSpace(extra.MieruMultiplexing) &&
                    !Constants.MieruMultiplexingModes.Contains(extra.MieruMultiplexing.Trim(), StringComparer.OrdinalIgnoreCase))
                    return "Check the required protocol fields.";
                if (!string.IsNullOrWhiteSpace(extra.MieruHandshakeMode) &&
                    !Constants.MieruHandshakeModes.Contains(extra.MieruHandshakeMode.Trim(), StringComparer.OrdinalIgnoreCase))
                    return "Check the required protocol fields.";
                if (!string.IsNullOrWhiteSpace(extra.MieruTrafficPattern) &&
                    !IsValidBase64(extra.MieruTrafficPattern))
                    return "Mieru traffic pattern must be valid Base64.";
                break;

            case EConfigType.ShadowQUIC:
                if (string.IsNullOrWhiteSpace(server.Username) || string.IsNullOrEmpty(server.Password))
                    return "Check the required protocol fields.";
                if (!string.IsNullOrWhiteSpace(extra.ShadowQuicVersions) &&
                    extra.ShadowQuicVersions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(v => !v.Equals("v1", StringComparison.OrdinalIgnoreCase) && !v.Equals("v2", StringComparison.OrdinalIgnoreCase)))
                    return "ShadowQUIC versions must be v1 or v2.";
                if (!string.IsNullOrWhiteSpace(extra.CongestionControl) &&
                    !Constants.ShadowQuicCongestionControls.Contains(extra.CongestionControl.Trim().ToLowerInvariant()))
                    return "Check the required protocol fields.";
                if (!string.IsNullOrWhiteSpace(extra.BbrProfile) &&
                    !Constants.Hysteria2BbrProfiles.Contains(extra.BbrProfile.Trim().ToLowerInvariant()))
                    return "BBR profile must be standard, conservative, or aggressive.";
                if (extra.UpMbps is < 0 || extra.DownMbps is < 0 ||
                    extra.ShadowQuicKeepAliveInterval is < 0 || extra.ShadowQuicCwnd is < 0 ||
                    extra.ShadowQuicMaxDatagramFrameSize is < 0 || extra.ShadowQuicMaxOpenStreams is < 0 ||
                    extra.ShadowQuicRecvWindowConn is < 0 || extra.ShadowQuicRecvWindow is < 0)
                    return "Check the required protocol fields.";
                break;

            case EConfigType.ExternalCli:
                if (string.IsNullOrWhiteSpace(CustomCoreHelper.NormalizeExecutableInput(extra.CliExecutable)))
                    return "Enter the CLI executable path.";
                break;
        }

        if (server.ConfigType is not (EConfigType.WireGuard or EConfigType.AmneziaWG or EConfigType.ShadowsocksR or EConfigType.SOCKS or EConfigType.SSH or EConfigType.Mieru or EConfigType.ShadowQUIC or EConfigType.ExternalCli))
        {
            if (!IsValidPemChain(server.Cert))
                return "Invalid PEM certificate.";

            if (server.StreamSecurity == Constants.StreamSecurityReality)
            {
                if (string.IsNullOrWhiteSpace(server.PublicKey))
                    return "Enter a Reality public key.";
                if (!string.IsNullOrEmpty(server.ShortId) &&
                    (server.ShortId.Length > 16 || server.ShortId.Length % 2 != 0 || !server.ShortId.All(Uri.IsHexDigit)))
                    return "Short ID must be an even-length hex string up to 16 characters.";
                if (server.ConfigType is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan &&
                    !SupportsReality(server.ConfigType, server.Network))
                    return "Choose a transport that supports REALITY or change the TLS security mode.";
            }

            if (server.ConfigType is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan &&
                server.GetNetwork() == nameof(ETransport.xhttp) &&
                !IsValidJsonObject(server.Extra))
                return "XHTTP extra must be a valid JSON object.";
        }

        return null;
    }

    private static string? ValidateWireGuardCore(Server server)
    {
        var extra = server.ProtoExtra;
        var peerKey = string.IsNullOrWhiteSpace(extra.WgPublicKey) ? server.PublicKey : extra.WgPublicKey;
        if (!IsValidWireGuardKey(server.Password) || !IsValidWireGuardKey(peerKey))
            return "Enter valid Base64 WireGuard keys.";
        if (!string.IsNullOrWhiteSpace(extra.WgPresharedKey) && !IsValidWireGuardKey(extra.WgPresharedKey))
            return "Enter valid Base64 WireGuard keys.";
        if (!IsValidWireGuardAddresses(extra.WgInterfaceAddress))
            return "The WireGuard local interface address is invalid.";
        if (!IsValidWireGuardReserved(extra.WgReserved))
            return "WireGuard reserved must contain three values between 0 and 255.";
        if (extra.WgMtu is <= 0)
            return "Check the required protocol fields.";
        return null;
    }
}

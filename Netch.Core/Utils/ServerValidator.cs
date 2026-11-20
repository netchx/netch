using System.Globalization;
using System.Net;
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
                if (!string.IsNullOrWhiteSpace(extra.Ports) && !string.IsNullOrWhiteSpace(extra.HopInterval) &&
                    (!uint.TryParse(extra.HopInterval.Trim().TrimEnd('s'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval) || interval < 5))
                    return "Hop interval must be at least 5 seconds.";
                if (extra.UpMbps is < 0 || extra.DownMbps is < 0)
                    return "Check the required protocol fields.";
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
        }

        if (server.ConfigType is not (EConfigType.WireGuard or EConfigType.ShadowsocksR or EConfigType.SOCKS))
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
}

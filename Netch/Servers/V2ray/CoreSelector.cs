using System.Text.Json;
using Netch.Models;

namespace Netch.Servers;

public static class CoreSelector
{
    public static bool ValidReality(string? publicKey, string? shortId)
    {
        if (publicKey?.Length != 43 ||
            publicKey.Any(ch => !(ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')) ||
            (!string.IsNullOrEmpty(shortId) &&
             (shortId.Length > 16 || shortId.Length % 2 != 0 || !shortId.All(Uri.IsHexDigit))))
            return false;

        Span<byte> key = stackalloc byte[32];
        return Convert.TryFromBase64String(publicKey.Replace('-', '+').Replace('_', '/') + "=", key, out var length) &&
               length == 32;
    }

    public static bool ValidXhttpExtra(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool UseXray(Server server)
    {
        switch (server)
        {
            case VLESSServer vless:
                return vless.TLSSecureType is "tls" or "reality" &&
                       vless.PacketEncoding != "packet" &&
                       vless.TransferProtocol is not ("h2" or "quic" or "kcp") &&
                       (vless.Flow is "xtls-rprx-direct" or "xtls-rprx-vision") &&
                       (vless.TLSSecureType != "tls" || !Global.Settings.V2RayConfig.AllowInsecure ||
                        vless.Flow == "xtls-rprx-vision" || !string.IsNullOrWhiteSpace(vless.Fingerprint));
            case VMessServer vmess:
                return vmess.AlterID == 0 && vmess.EncryptMethod is not ("none" or "zero") &&
                       vmess.PacketEncoding != "packet" &&
                       vmess.TLSSecureType != "xtls" &&
                       vmess.TransferProtocol is not ("h2" or "quic" or "kcp") &&
                       (vmess.TLSSecureType != "tls" || !Global.Settings.V2RayConfig.AllowInsecure ||
                        !string.IsNullOrWhiteSpace(vmess.Fingerprint));
            case TrojanServer trojan:
                return trojan.TLSSecureType is "tls" or "reality" &&
                       trojan.TransferProtocol is not "h2" &&
                       (trojan.TLSSecureType != "tls" || !Global.Settings.V2RayConfig.AllowInsecure ||
                        !string.IsNullOrWhiteSpace(trojan.Fingerprint));
            case WireGuardServer:
                return true;
            case Socks5Server socks:
                return socks.Version == "5";
            case ShadowsocksServer ss:
                return string.IsNullOrEmpty(ss.Plugin) && ss.EncryptMethod is (
                    "2022-blake3-aes-128-gcm" or "2022-blake3-aes-256-gcm" or
                    "2022-blake3-chacha20-poly1305" or "aes-128-gcm" or
                    "aes-256-gcm" or "chacha20-ietf-poly1305" or "xchacha20-ietf-poly1305");
            default:
                return false;
        }
    }
}

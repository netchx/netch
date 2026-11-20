using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Utils;

/// <summary>
///     Shared node-editor defaults, normalization, and validation rules.
/// </summary>
public static class ServerEditorHelper
{
    public static IReadOnlyList<IServerUtil> GetOrderedProtocols() =>
        ServerHelper.ServerUtilDictionary.Values
            .OrderBy(util => util.Priority)
            .ThenBy(util => util.TypeName, StringComparer.Ordinal)
            .ToArray();

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
                    extra.HopInterval = string.Empty;
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
        }

        if (protocol is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan &&
            server.GetNetwork() != nameof(ETransport.xhttp))
            server.Extra = string.Empty;

        if (protocol is not (EConfigType.WireGuard or EConfigType.ShadowsocksR or EConfigType.SOCKS or EConfigType.Shadowsocks) &&
            server.StreamSecurity != Constants.StreamSecurity)
        {
            server.Cert = string.Empty;
            server.CertSha = string.Empty;
            server.EchConfigList = string.Empty;
            server.EchForceQuery = string.Empty;
        }

        if (protocol == EConfigType.HTTP &&
            server.StreamSecurity is not (Constants.StreamSecurity or Constants.StreamSecurityReality))
        {
            server.Sni = string.Empty;
            server.Alpn = string.Empty;
            server.Fingerprint = string.Empty;
            server.AllowInsecure = null;
        }
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

using Netch.Enums;
using Netch.Servers;
using Netch.Utils;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace Netch.Models;

public abstract class Server : ICloneable
{
    /// <summary>
    ///     延迟
    /// </summary>
    [JsonIgnore]
    public int Delay { get; private set; } = DelayStatus.Untested;

    internal void SetDelay(int delay) => Delay = delay;

    /// <summary>
    ///     稳定标识，场景按它引用节点；订阅更新时由 <see cref="SubscriptionUtil" /> 沿用
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     组
    /// </summary>
    public string Group { get; set; } = Constants.DefaultGroup;

    public int ConfigVersion { get; set; }
    public string Subid { get; set; }
    public bool IsSub { get; set; } = true;
    public int? PreSocksPort { get; set; }
    public bool DisplayLog { get; set; } = true;
    public string Remarks { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }
    public string Network { get; set; }
    public string HeaderType { get; set; }
    public string RequestHost { get; set; }
    public string Path { get; set; }
    public string StreamSecurity { get; set; }
    public bool? AllowInsecure { get; set; }
    public string Sni { get; set; }
    public string Alpn { get; set; } = string.Empty;
    public string Fingerprint { get; set; }
    public string PublicKey { get; set; }
    public string ShortId { get; set; }
    public string SpiderX { get; set; }
    public string Mldsa65Verify { get; set; }
    public string Extra { get; set; }
    public bool? MuxEnabled { get; set; }
    public string Cert { get; set; }
    public string CertSha { get; set; }
    public string EchConfigList { get; set; }
    public string EchForceQuery { get; set; }

    public ProtocolExtraItem ProtoExtra { get; set; } = new ProtocolExtraItem();

    /// <summary>
    ///     倍率
    /// </summary>
    public double Rate { get; } = 1.0;

    /// <summary>
    ///     代理类型
    /// </summary>
    [JsonPropertyOrder(int.MinValue)]
    public abstract EConfigType ConfigType { get; }

    public object Clone()
    {
        return MemberwiseClone();
    }

    /// <summary>
    ///     获取备注
    /// </summary>
    /// <returns>备注</returns>
    public override string ToString()
    {
        var remark = string.IsNullOrWhiteSpace(Remarks) ? $"{Address}:{Port}" : Remarks;

        var shortName = ServerHelper.GetUtilByTypeName(ConfigType.ToString()).ShortName;

        return $"[{shortName}][{Group}] {remark}";
    }

    public abstract string MaskedData();

    public bool SupportsTcpPreScreen()
    {
        if (ConfigType is EConfigType.Hysteria2 or EConfigType.TUIC or EConfigType.WireGuard or EConfigType.MASQUE)
            return false;

        if (ConfigType == EConfigType.Naive && ProtoExtra.NaiveQuic == true)
            return false;

        if (GetNetwork() is "quic" or "kcp" or "hysteria")
            return false;

        if (ConfigType == EConfigType.Shadowsocks &&
            ProtoExtra.SsPluginOptions?.Split(';', StringSplitOptions.TrimEntries)
                .Contains("mode=quic", StringComparer.OrdinalIgnoreCase) == true)
            return false;

        return true;
    }

    /// <summary>
    ///     测试延迟
    /// </summary>
    /// <returns>延迟</returns>
    public Task<int> PingAsync() => PingAsync(CancellationToken.None);

    public async Task<int> PingAsync(CancellationToken ct)
    {
        try
        {
            var timeout = Math.Clamp(Global.Settings.DelayTestTimeout, Constants.MinDelayTestTimeout, Constants.MaxDelayTestTimeout);
            var ips = await DnsUtils.LookupAllAsync(Address, dns: Global.Settings.OutboundDNS, timeout: Math.Min(timeout, 3000), ct: ct);
            if (ips.Length == 0)
                return Delay = DelayStatus.DnsFailed;

            var useIcmp = Global.Settings.DelayTestMode == DelayTestMode.ICMPing;
            return Delay = await DelayTestHelper.ProbeRawEndpointAsync(ips, Port, timeout, useIcmp, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Delay = DelayStatus.Unreachable;
        }
    }

    public List<string>? GetAlpn()
    {
        return Alpn.IsNullOrEmpty() ? null : Utils.Utils.String2List(Alpn);
    }

    public string GetNetwork()
    {
        if (Network.IsNullOrEmpty())
        {
            return Constants.DefaultNetwork;
        }

        var network = Network.TrimEx().ToLowerInvariant() switch
        {
            "raw" => nameof(ETransport.tcp),
            "splithttp" => nameof(ETransport.xhttp),
            var value => value
        };

        return Constants.Networks.Contains(network) ? network : Constants.DefaultNetwork;
    }
}

public record ProtocolExtraItem
{
    // sing-box common
    public bool? Uot { get; set; }
    public string? CongestionControl { get; set; }

    // HTTP / MASQUE headers are stored as one header per line: Name: Value
    public string? HttpHeaders { get; set; }

    // vmess
    public int? AlterId { get; set; }
    public string? VmessSecurity { get; set; }

    // vless
    public string? Flow { get; set; }
    public string? VlessEncryption { get; set; }
    
    // SS
    public string? SsMethod { get; set; }
    public string? SsPlugin { get; set; }
    public string? SsPluginOptions { get; set; }

    // wireguard
    public string? WgPublicKey { get; set; }
    public string? WgPresharedKey { get; set; }
    public string? WgInterfaceAddress { get; set; }
    public string? WgReserved { get; set; }
    public int? WgMtu { get; set; }

    // hysteria2
    /// <summary>
    /// 混淆类型,支持 none、salamander、gecko
    /// </summary>
    public string Obfs { get; set; } = Constants.None;
    public string? SalamanderPass { get; set; }
    public int? UpMbps { get; set; }
    public int? DownMbps { get; set; }
    public string? Ports { get; set; }
    public string? HopInterval { get; set; }
    public string? Hy2RealmUrl { get; set; }
    public string? GeckoMinPacketSize { get; set; }
    public string? GeckoMaxPacketSize { get; set; }

    // tuic
    public string? TuicUdpRelayMode { get; set; }
    public bool? TuicReduceRtt { get; set; }
    public bool? TuicDisableSni { get; set; }

    // AnyTLS
    public int? AnyTlsIdleSessionCheckIntervalSeconds { get; set; }
    public int? AnyTlsIdleSessionTimeoutSeconds { get; set; }
    public int? AnyTlsMinIdleSession { get; set; }

    // NaiveProxy
    public int? InsecureConcurrency { get; set; }
    public bool? NaiveQuic { get; set; }

    // MASQUE
    public string? MasquePath { get; set; }
    public string? MasqueHeaders { get; set; }

    // Custom outbound compatibility marker used by v2rayN profiles.
    public bool? IsSingboxEndpoint { get; set; }

    // group profile
    public string? GroupType { get; set; }
    public string? ChildItems { get; set; }
    public string? SubChildItems { get; set; }
    public string? Filter { get; set; }
}

public static class ServerExtension
{
    public static async Task<string> AutoResolveHostnameAsync(this Server server, AddressFamily inet = AddressFamily.Unspecified)
    {
        // ! MainController cached
        return (await DnsUtils.LookupAsync(server.Address, inet))!.ToString();
    }

    public static bool IsInGroup(this Server server)
    {
        return server.Group is not Constants.DefaultGroup;
    }
}

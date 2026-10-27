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
    public int Delay { get; private set; } = -1;

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

    /// <summary>
    ///     测试延迟
    /// </summary>
    /// <returns>延迟</returns>
    public async Task<int> PingAsync()
    {
        try
        {
            var destination = await DnsUtils.LookupAsync(hostname: Address, dns: Global.Settings.OutboundDNS);
            if (destination == null)
                return Delay = -2;

            const int timeout = 1000;

            async Task<int> PingCoreAsync()
            {
                try
                {
                    return Global.Settings.ServerTCPing
                        ? await Utils.Utils.TCPingAsync(destination, Port, timeout)
                        : await Utils.Utils.ICMPingAsync(destination, timeout);
                }
                catch (Exception)
                {
                    return -4;
                }
            }

            // Take the best of three probes. Waiting for the first one to finish
            // reported failures (refused/unreachable return immediately) even when
            // the other probes succeeded.
            var results = await Task.WhenAll(PingCoreAsync(), PingCoreAsync(), PingCoreAsync());
            var succeeded = results.Where(r => r >= 0 && r < timeout).ToArray();
            if (succeeded.Length > 0)
                return Delay = succeeded.Min();

            return Delay = results.Contains(timeout) ? timeout : results.Max();
        }
        catch (Exception)
        {
            return Delay = -4;
        }
    }

    public List<string>? GetAlpn()
    {
        return Alpn.IsNullOrEmpty() ? null : Utils.Utils.String2List(Alpn);
    }

    public string GetNetwork()
    {
        if (Network.IsNullOrEmpty() || !Constants.Networks.Contains(Network))
        {
            return Constants.DefaultNetwork;
        }
        return Network.TrimEx();
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

    // wireguard
    public string? WgPublicKey { get; set; }
    public string? WgPresharedKey { get; set; }
    public string? WgInterfaceAddress { get; set; }
    public string? WgReserved { get; set; }
    public int? WgMtu { get; set; }

    // hysteria2
    /// <summary>
    /// 混淆类型,目前只支持 salamander
    /// </summary>
    public string Obfs { get; set; } = Constants.Hysteria2Obfs[0];
    public string? SalamanderPass { get; set; }
    public int? UpMbps { get; set; }
    public int? DownMbps { get; set; }
    public string? Ports { get; set; }
    public string? HopInterval { get; set; }
    public string? Hy2RealmUrl { get; set; }
    public string? GeckoMinPacketSize { get; set; }
    public string? GeckoMaxPacketSize { get; set; }

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

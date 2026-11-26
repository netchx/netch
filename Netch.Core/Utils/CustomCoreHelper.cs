using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Netch.Enums;
using Netch.Models;
using Netch.Servers;
using Netch.Services;

namespace Netch.Utils;

public sealed record CustomCorePreset(string Name, string ArgumentsTemplate, string ConfigTemplate = "", string ConfigFileExtension = ".json");

public static class CustomCoreHelper
{
    public const string BuiltInCoreId = "__builtin__";

    public static readonly string[] SupportedConfigExtensions = [".json", ".yaml", ".toml", ".conf", ".txt"];

    public static string NormalizeConfigExtension(string? raw)
    {
        var ext = (raw ?? string.Empty).Trim().ToLowerInvariant();
        if (ext.Length > 0 && ext[0] != '.')
            ext = "." + ext;
        if (ext == ".yml")
            ext = ".yaml";

        return Array.IndexOf(SupportedConfigExtensions, ext) >= 0 ? ext : ".json";
    }

    public static bool SupportsMode(EConfigType protocol, CustomCoreMode mode)
    {
        if (protocol == EConfigType.ExternalCli || !Enum.IsDefined(protocol))
            return false;

        return mode switch
        {
            CustomCoreMode.CliArguments => true,
            CustomCoreMode.MihomoConfig => protocol is not (EConfigType.Naive or EConfigType.MASQUE),
            CustomCoreMode.SingboxConfig => protocol is not (EConfigType.ShadowsocksR or EConfigType.TrustTunnel
                or EConfigType.Mieru or EConfigType.ShadowQUIC or EConfigType.AmneziaWG),
            CustomCoreMode.XrayConfig => protocol is EConfigType.SOCKS or EConfigType.HTTP or EConfigType.VLESS
                or EConfigType.VMess or EConfigType.Shadowsocks or EConfigType.Trojan
                or EConfigType.WireGuard or EConfigType.Hysteria2 or EConfigType.MASQUE,
            _ => false
        };
    }

    public static IReadOnlyList<CustomCoreMode> GetSupportedModes(EConfigType protocol) =>
        Enum.GetValues<CustomCoreMode>().Where(mode => SupportsMode(protocol, mode)).ToArray();

    public static bool IsBuiltInCoreId(string? id) =>
        string.Equals(id?.Trim(), BuiltInCoreId, StringComparison.OrdinalIgnoreCase);

    public static string NormalizeExecutableInput(string? raw)
    {
        var trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Trim();

        return trimmed;
    }

    public static string NormalizeArgumentsInput(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return raw
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }

    public static string NormalizeConfigTemplate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return raw
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    public static string ResolveExecutablePath(string? raw)
    {
        var normalized = NormalizeExecutableInput(raw);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new MessageException(i18N.Translate("Enter the CLI executable path."));

        if (Path.IsPathRooted(normalized))
        {
            var full = Path.GetFullPath(normalized);
            if (File.Exists(full))
                return full;
        }
        else
        {
            var fromRoot = Path.GetFullPath(Path.Combine(Global.NetchDir, normalized));
            if (File.Exists(fromRoot))
                return fromRoot;

            var fromBin = Path.GetFullPath(Path.Combine(Global.NetchDir, "bin", normalized));
            if (File.Exists(fromBin))
                return fromBin;
        }

        throw new MessageException(i18N.TranslateFormat("External CLI executable not found: {0}", normalized));
    }

    public static IReadOnlyList<CustomCore> GetAll() =>
        Global.Settings.CustomCores ?? [];

    public static IReadOnlyList<CustomCore> GetForProtocol(EConfigType protocol)
    {
        if (protocol == EConfigType.ExternalCli)
            return [];

        return GetAll()
            .Where(c => c.Protocol == protocol)
            .ToArray();
    }

    public static CustomCore? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var trimmed = id.Trim();
        return GetAll().FirstOrDefault(c => string.Equals(c.Id, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public static CustomCore? ResolveForServer(Server server)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (server.ConfigType == EConfigType.ExternalCli)
            return null;

        var nodeCoreId = server.ProtoExtra.CustomCoreId?.Trim();
        if (!string.IsNullOrEmpty(nodeCoreId))
        {
            if (IsBuiltInCoreId(nodeCoreId))
                return null;

            var nodeCore = FindById(nodeCoreId);
            if (nodeCore != null && nodeCore.Protocol == server.ConfigType)
                return nodeCore;
        }

        if (server.IsSub)
        {
            var sub = (Global.Settings.Subscription ?? []).FirstOrDefault(s =>
                NodeGroupHelper.GroupEquals(server.Group, s.Remark));
            var subCoreId = sub?.CustomCoreId?.Trim();
            if (!string.IsNullOrEmpty(subCoreId))
            {
                if (IsBuiltInCoreId(subCoreId))
                    return null;

                var subCore = FindById(subCoreId);
                if (subCore != null && subCore.Protocol == server.ConfigType)
                    return subCore;
            }
        }

        return GetAll().FirstOrDefault(c => c.Protocol == server.ConfigType && c.IsProtocolDefault);
    }

    public static string? Validate(CustomCore core)
    {
        ArgumentNullException.ThrowIfNull(core);
        if (string.IsNullOrWhiteSpace(core.Name))
            return "Enter a custom core name.";
        if (core.Protocol == EConfigType.ExternalCli || !Enum.IsDefined(core.Protocol))
            return "Choose a protocol for the custom core.";
        if (string.IsNullOrWhiteSpace(NormalizeExecutableInput(core.ExecutablePath)))
            return "Enter the CLI executable path.";
        if (!Enum.IsDefined(core.Mode))
            return "Choose a launch mode for the custom core.";
        if (!SupportsMode(core.Protocol, core.Mode))
            return "The selected launch mode does not support this protocol.";
        if (core.Mode == CustomCoreMode.CliArguments)
        {
            if (string.IsNullOrWhiteSpace(core.ArgumentsTemplate))
                return "Enter the CLI arguments template.";

            var usesConfigPlaceholder = core.ArgumentsTemplate.Contains("{config_file}", StringComparison.OrdinalIgnoreCase);
            var hasConfigTemplate = !string.IsNullOrWhiteSpace(core.ConfigTemplate);
            if (usesConfigPlaceholder && !hasConfigTemplate)
                return "Enter the config file template.";
            if (hasConfigTemplate && !usesConfigPlaceholder)
                return "The CLI arguments template must reference the config file placeholder.";
        }

        return null;
    }

    public static IReadOnlyList<string> GetPlaceholdersForProtocol(EConfigType protocol)
    {
        var list = new List<string> { "address", "host", "port", "local_addr", "local_port", "remarks" };
        switch (protocol)
        {
            case EConfigType.Shadowsocks:
                list.AddRange(["method", "password", "plugin", "plugin_opts", "uot"]);
                break;
            case EConfigType.ShadowsocksR:
                list.AddRange(["method", "password", "ssr_protocol", "ssr_protocol_param", "ssr_obfs", "ssr_obfs_param"]);
                break;
            case EConfigType.VLESS:
                list.AddRange(["uuid", "network", "security", "sni", "flow", "alpn", "fingerprint", "request_host", "path", "public_key", "short_id", "spider_x", "vless_encryption", "insecure"]);
                break;
            case EConfigType.VMess:
                list.AddRange(["uuid", "alter_id", "vmess_security", "network", "security", "sni", "alpn", "fingerprint", "request_host", "path", "insecure"]);
                break;
            case EConfigType.Trojan:
                list.AddRange(["password", "network", "security", "sni", "flow", "alpn", "fingerprint", "request_host", "path", "public_key", "short_id", "insecure"]);
                break;
            case EConfigType.Hysteria2:
                list.AddRange(["password", "sni", "alpn", "obfs", "obfs_password", "up_mbps", "down_mbps", "ports", "hop_interval", "insecure"]);
                break;
            case EConfigType.TUIC:
                list.AddRange(["uuid", "password", "sni", "alpn", "congestion", "udp_relay_mode", "insecure"]);
                break;
            case EConfigType.AnyTLS:
                list.AddRange(["password", "sni", "alpn", "fingerprint", "insecure"]);
                break;
            case EConfigType.Naive:
                list.AddRange(["username", "password", "sni", "naive_quic", "congestion", "uot"]);
                break;
            case EConfigType.MASQUE:
                list.AddRange(["username", "password", "sni", "masque_path", "insecure"]);
                break;
            case EConfigType.ShadowTLS:
                list.AddRange(["method", "password", "sni", "shadowtls_version", "shadowtls_password", "uot"]);
                break;
            case EConfigType.TrustTunnel:
                list.AddRange(["username", "password", "sni", "trusttunnel_quic", "insecure"]);
                break;
            case EConfigType.Mieru:
                list.AddRange(["username", "password", "mieru_transport", "mieru_port_range"]);
                break;
            case EConfigType.ShadowQUIC:
                list.AddRange(["username", "password", "sni", "alpn", "congestion", "shadowquic_versions"]);
                break;
            case EConfigType.WireGuard or EConfigType.AmneziaWG:
                list.AddRange(["private_key", "public_key", "preshared_key", "interface_addr", "reserved", "mtu"]);
                break;
            case EConfigType.SSH:
                list.AddRange(["username", "password", "private_key"]);
                break;
            case EConfigType.HTTP:
                list.AddRange(["username", "password", "security", "sni", "alpn", "insecure"]);
                break;
            case EConfigType.SOCKS:
                list.AddRange(["username", "password", "uot"]);
                break;
        }

        list.Add("config_file");
        return list;
    }

    public static IReadOnlyList<CustomCorePreset> GetPresetsForProtocol(EConfigType protocol) => protocol switch
    {
        EConfigType.Shadowsocks =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -b {local_addr} -l {local_port} -m {method} -k {password} {?plugin:--plugin {plugin}} {?plugin_opts:--plugin-opts {plugin_opts}}"),
            new CustomCorePreset("shadowsocks-rust (JSON)", "-c {config_file}",
                "{\n  \"server\": \"{address}\",\n  \"server_port\": {port},\n  \"method\": \"{method}\",\n  \"password\": \"{password}\",\n  \"local_address\": \"{local_addr}\",\n  \"local_port\": {local_port}{?plugin:,\n  \"plugin\": \"{plugin}\"}{?plugin_opts:,\n  \"plugin_opts\": \"{plugin_opts}\"}\n}",
                ".json")
        ],
        EConfigType.ShadowsocksR =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -b {local_addr} -l {local_port} -m {method} -k {password} -O {ssr_protocol} {?ssr_protocol_param:-G {ssr_protocol_param}} -o {ssr_obfs} {?ssr_obfs_param:-g {ssr_obfs_param}}")
        ],
        EConfigType.Naive =>
        [
            new CustomCorePreset("NaiveProxy (CLI)", "--listen=socks://{local_addr_port} --proxy=https://{username}:{password}@{host_port}"),
            new CustomCorePreset("NaiveProxy (JSON)", "{config_file}",
                "{\n  \"listen\": \"socks://{local_addr_port}\",\n  \"proxy\": \"https://{username}:{password}@{host_port}\"\n}",
                ".json")
        ],
        EConfigType.Hysteria2 =>
        [
            new CustomCorePreset("CLI", "-s {address_port} -k {password} -b {local_addr_port} {?sni:--sni {sni}} {?insecure:--insecure}"),
            new CustomCorePreset("Hysteria2 (JSON)", "client -c {config_file}",
                "{\n  \"server\": \"{address_port}\",\n  \"auth\": \"{password}\",\n  \"tls\": {\n    \"sni\": \"{sni|host}\",\n    \"insecure\": {insecure|false}\n  }{?obfs:,\n  \"obfs\": {\n    \"type\": \"{obfs}\",\n    \"{obfs}\": {\n      \"password\": \"{obfs_password}\"\n    }\n  }},\n  \"socks5\": {\n    \"listen\": \"{local_addr_port}\"\n  }\n}",
                ".json")
        ],
        EConfigType.TUIC =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -u {uuid} -k {password} -b {local_addr} -l {local_port} {?sni:--sni {sni}}"),
            new CustomCorePreset("tuic-client (JSON)", "-c {config_file}",
                "{\n  \"relay\": {\n    \"server\": \"{host_port}\",\n    \"ip\": \"{address}\",\n    \"uuid\": \"{uuid}\",\n    \"password\": \"{password}\",\n    \"congestion_control\": \"{congestion|bbr}\",\n    \"alpn\": [\"{alpn|h3}\"]\n  },\n  \"local\": {\n    \"server\": \"{local_addr_port}\"\n  }\n}",
                ".json")
        ],
        EConfigType.VLESS =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -u {uuid} -b {local_addr} -l {local_port} {?sni:--sni {sni}} {?flow:--flow {flow}}"),
            new CustomCorePreset("JSON", "-c {config_file}",
                "{\n  \"listen\": \"{local_addr_port}\",\n  \"server\": \"{address}\",\n  \"port\": {port},\n  \"uuid\": \"{uuid}\",\n  \"network\": \"{network|tcp}\",\n  \"sni\": \"{sni|host}\",\n  \"flow\": \"{flow}\"\n}",
                ".json")
        ],
        EConfigType.VMess =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -u {uuid} -b {local_addr} -l {local_port} {?sni:--sni {sni}}"),
            new CustomCorePreset("JSON", "-c {config_file}",
                "{\n  \"listen\": \"{local_addr_port}\",\n  \"server\": \"{address}\",\n  \"port\": {port},\n  \"uuid\": \"{uuid}\",\n  \"security\": \"{vmess_security|auto}\",\n  \"network\": \"{network|tcp}\",\n  \"sni\": \"{sni|host}\"\n}",
                ".json")
        ],
        EConfigType.Trojan or EConfigType.AnyTLS =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -k {password} -b {local_addr} -l {local_port} {?sni:--sni {sni}} {?insecure:--insecure}"),
            new CustomCorePreset("JSON", "-c {config_file}",
                "{\n  \"listen\": \"{local_addr_port}\",\n  \"server\": \"{address}\",\n  \"port\": {port},\n  \"password\": \"{password}\",\n  \"sni\": \"{sni|host}\",\n  \"insecure\": {insecure|false}\n}",
                ".json")
        ],
        EConfigType.WireGuard or EConfigType.AmneziaWG =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -k {private_key} --peer-key {public_key} -b {local_addr} -l {local_port}"),
            new CustomCorePreset("JSON", "-c {config_file}",
                "{\n  \"listen\": \"{local_addr_port}\",\n  \"endpoint\": \"{address_port}\",\n  \"private_key\": \"{private_key}\",\n  \"public_key\": \"{public_key}\",\n  \"address\": \"{interface_addr}\",\n  \"mtu\": {mtu|1280}\n}",
                ".json")
        ],
        _ =>
        [
            new CustomCorePreset("CLI", "-s {address} -p {port} -b {local_addr} -l {local_port} {?username:-u {username}} {?password:-k {password}} {?sni:--sni {sni}}"),
            new CustomCorePreset("JSON", "-c {config_file}",
                "{\n  \"listen\": \"{local_addr_port}\",\n  \"server\": \"{address}\",\n  \"port\": {port},\n  \"username\": \"{username}\",\n  \"password\": \"{password}\",\n  \"sni\": \"{sni|host}\"\n}",
                ".json")
        ]
    };

    public static string GetDefaultCliTemplate(EConfigType protocol) =>
        GetPresetsForProtocol(protocol)[0].ArgumentsTemplate;

    public static Server CreateSampleServer(EConfigType protocol)
    {
        var server = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(protocol.ToString()));
        server.Remarks = "Example";
        server.Address = "proxy.example.com";
        if (server.Port <= 0)
            server.Port = 443;

        switch (protocol)
        {
            case EConfigType.Shadowsocks:
                server.Port = 8388;
                server.Password = "secret";
                server.ProtoExtra.SsMethod = "aes-256-gcm";
                break;
            case EConfigType.ShadowsocksR:
                server.Port = 8388;
                server.Password = "secret";
                if (server is ShadowsocksRServer ssr)
                {
                    ssr.EncryptMethod = "aes-256-cfb";
                    ssr.Protocol = "origin";
                    ssr.OBFS = "plain";
                }
                break;
            case EConfigType.SOCKS:
                server.Port = 1080;
                server.Username = "user";
                server.Password = "pass";
                break;
            case EConfigType.HTTP:
                server.Username = "user";
                server.Password = "pass";
                server.Sni = "proxy.example.com";
                break;
            case EConfigType.SSH:
                server.Username = "root";
                server.Password = "secret";
                break;
            case EConfigType.VMess or EConfigType.VLESS:
                server.Password = "00000000-0000-0000-0000-000000000000";
                server.Sni = "proxy.example.com";
                break;
            case EConfigType.Trojan or EConfigType.AnyTLS or EConfigType.Hysteria2:
                server.Password = "secret";
                server.Sni = "proxy.example.com";
                if (protocol == EConfigType.Hysteria2)
                    server.ProtoExtra.Ports = "443,20000-30000";
                break;
            case EConfigType.TUIC:
                server.Username = "00000000-0000-0000-0000-000000000000";
                server.Password = "secret";
                server.Sni = "proxy.example.com";
                server.Alpn = "h3";
                server.ProtoExtra.CongestionControl = "bbr";
                server.ProtoExtra.TuicUdpRelayMode = "native";
                break;
            case EConfigType.Naive or EConfigType.TrustTunnel or EConfigType.MASQUE or EConfigType.ShadowQUIC or EConfigType.Mieru:
                server.Username = "user";
                server.Password = "secret";
                server.Sni = "proxy.example.com";
                break;
            case EConfigType.WireGuard or EConfigType.AmneziaWG:
                server.Port = 51820;
                server.Password = "<private-key>";
                server.ProtoExtra.WgPublicKey = "<public-key>";
                server.ProtoExtra.WgInterfaceAddress = "10.0.0.2/32";
                server.ProtoExtra.WgMtu = 1280;
                break;
            case EConfigType.ShadowTLS:
                server.Password = "ss-secret";
                server.Sni = "cdn.example.com";
                server.ProtoExtra.SsMethod = "aes-256-gcm";
                server.ProtoExtra.ShadowTlsPassword = "tls-secret";
                server.ProtoExtra.ShadowTlsVersion = 3;
                break;
        }

        return server;
    }

    public static string RenderArguments(string? template, Server server, string resolvedAddress, string localAddress, ushort localPort, string? configFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(server);
        var input = NormalizeArgumentsInput(template);
        if (input.Length == 0)
            return string.Empty;

        var tokens = BuildTokenMap(server, resolvedAddress, localAddress, localPort, configFilePath);
        var sb = new StringBuilder(input.Length + 32);
        var inQuotes = false;
        var precedingBackslashes = 0;

        RenderCliSegment(input, tokens, sb, ref inQuotes, ref precedingBackslashes);
        return sb.ToString().Trim();
    }

    public static string RenderConfigTemplate(string? template, string? fileExtension, Server server, string resolvedAddress, string localAddress, ushort localPort)
    {
        ArgumentNullException.ThrowIfNull(server);
        var normalized = NormalizeConfigTemplate(template);
        if (normalized.Length == 0)
            return string.Empty;

        var ext = NormalizeConfigExtension(fileExtension);
        var tokens = BuildTokenMap(server, resolvedAddress, localAddress, localPort);
        var sb = new StringBuilder(normalized.Length + 64);
        var inDoubleQuotes = false;
        var inSingleQuotes = false;
        var precedingBackslashes = 0;

        RenderConfigSegment(normalized, ext, tokens, sb, ref inDoubleQuotes, ref inSingleQuotes, ref precedingBackslashes);
        return sb.ToString();
    }

    private static Dictionary<string, string> BuildTokenMap(
        Server server,
        string resolvedAddress,
        string localAddress,
        ushort localPort,
        string? configFilePath = null)
    {
        var extra = server.ProtoExtra;
        var ssr = server as ShadowsocksRServer;
        var method = ssr?.EncryptMethod ?? extra.SsMethod ?? string.Empty;
        var uuid = server.ConfigType == EConfigType.TUIC ? server.Username ?? string.Empty : server.Password ?? string.Empty;
        var publicKey = !string.IsNullOrWhiteSpace(extra.WgPublicKey) ? extra.WgPublicKey : server.PublicKey ?? string.Empty;
        var privateKey = server.ConfigType == EConfigType.SSH
            ? (extra.SshPrivateKey ?? string.Empty)
            : (server.Password ?? string.Empty);
        var obfs = string.Equals(extra.Obfs, Constants.None, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : extra.Obfs ?? string.Empty;
        var host = server.Address ?? string.Empty;
        var portText = server.Port.ToString(CultureInfo.InvariantCulture);
        var localPortText = localPort.ToString(CultureInfo.InvariantCulture);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["address"] = resolvedAddress,
            ["address_port"] = $"{ProxyUri.Host(resolvedAddress)}:{portText}",
            ["host"] = host,
            ["host_port"] = $"{ProxyUri.Host(host)}:{portText}",
            ["port"] = portText,
            ["local_addr"] = localAddress,
            ["local_addr_port"] = $"{ProxyUri.Host(localAddress)}:{localPortText}",
            ["local_port"] = localPortText,
            ["remarks"] = server.Remarks ?? string.Empty,
            ["username"] = server.Username ?? string.Empty,
            ["password"] = server.Password ?? string.Empty,
            ["uuid"] = uuid,
            ["method"] = method,
            ["plugin"] = extra.SsPlugin ?? string.Empty,
            ["plugin_opts"] = extra.SsPluginOptions ?? string.Empty,
            ["ssr_protocol"] = ssr?.Protocol ?? string.Empty,
            ["ssr_protocol_param"] = ssr?.ProtocolParam ?? string.Empty,
            ["ssr_obfs"] = ssr?.OBFS ?? string.Empty,
            ["ssr_obfs_param"] = ssr?.OBFSParam ?? string.Empty,
            ["network"] = server.Network ?? string.Empty,
            ["header_type"] = server.HeaderType ?? string.Empty,
            ["request_host"] = server.RequestHost ?? string.Empty,
            ["path"] = server.Path ?? string.Empty,
            ["security"] = server.StreamSecurity ?? string.Empty,
            ["sni"] = server.Sni ?? string.Empty,
            ["alpn"] = server.Alpn ?? string.Empty,
            ["fingerprint"] = server.Fingerprint ?? string.Empty,
            ["insecure"] = server.AllowInsecure == true ? "true" : string.Empty,
            ["flow"] = extra.Flow ?? string.Empty,
            ["alter_id"] = extra.AlterId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["vmess_security"] = extra.VmessSecurity ?? string.Empty,
            ["vless_encryption"] = extra.VlessEncryption ?? string.Empty,
            ["public_key"] = publicKey,
            ["short_id"] = server.ShortId ?? string.Empty,
            ["spider_x"] = server.SpiderX ?? string.Empty,
            ["obfs"] = obfs,
            ["obfs_password"] = extra.SalamanderPass ?? string.Empty,
            ["up_mbps"] = extra.UpMbps?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["down_mbps"] = extra.DownMbps?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["ports"] = extra.Ports ?? string.Empty,
            ["hop_interval"] = extra.HopInterval ?? string.Empty,
            ["congestion"] = extra.CongestionControl ?? string.Empty,
            ["udp_relay_mode"] = extra.TuicUdpRelayMode ?? string.Empty,
            ["naive_quic"] = extra.NaiveQuic == true ? "true" : string.Empty,
            ["uot"] = extra.Uot == true ? "true" : string.Empty,
            ["private_key"] = privateKey,
            ["preshared_key"] = extra.WgPresharedKey ?? string.Empty,
            ["interface_addr"] = extra.WgInterfaceAddress ?? string.Empty,
            ["reserved"] = extra.WgReserved ?? string.Empty,
            ["mtu"] = extra.WgMtu?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["shadowtls_version"] = extra.ShadowTlsVersion?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["shadowtls_password"] = extra.ShadowTlsPassword ?? string.Empty,
            ["masque_path"] = extra.MasquePath ?? string.Empty,
            ["mieru_transport"] = extra.MieruTransport ?? string.Empty,
            ["mieru_port_range"] = extra.MieruPortRange ?? string.Empty,
            ["shadowquic_versions"] = extra.ShadowQuicVersions ?? string.Empty,
            ["trusttunnel_quic"] = extra.TrustTunnelQuic == true ? "true" : string.Empty,
            ["config_file"] = configFilePath ?? string.Empty
        };
    }

    private static void RenderCliSegment(
        string segment,
        IReadOnlyDictionary<string, string> tokens,
        StringBuilder sb,
        ref bool inQuotes,
        ref int precedingBackslashes)
    {
        for (var i = 0; i < segment.Length; i++)
        {
            var ch = segment[i];
            if (ch == '\\')
            {
                precedingBackslashes++;
                sb.Append(ch);
                continue;
            }

            if (ch == '"')
            {
                if (precedingBackslashes % 2 == 0)
                    inQuotes = !inQuotes;
                precedingBackslashes = 0;
                sb.Append(ch);
                continue;
            }

            precedingBackslashes = 0;

            if (ch == '{')
            {
                if (TryParseConditional(segment, i, tokens, out var condBody, out var condMatched, out var condClose))
                {
                    if (condMatched)
                    {
                        RenderCliSegment(condBody, tokens, sb, ref inQuotes, ref precedingBackslashes);
                    }
                    else if (!inQuotes && sb.Length > 0 && char.IsWhiteSpace(sb[^1]) &&
                             condClose + 1 < segment.Length && char.IsWhiteSpace(segment[condClose + 1]))
                    {
                        i = condClose + 1;
                        continue;
                    }

                    i = condClose;
                    continue;
                }

                if (TryResolvePlaceholder(segment, i, tokens, out var val, out var close))
                {
                    var followedByQuote = inQuotes && close + 1 < segment.Length && segment[close + 1] == '"';
                    sb.Append(FormatWindowsArgValue(val, inQuotes, followedByQuote));
                    i = close;
                    continue;
                }
            }

            sb.Append(ch);
        }
    }

    private static void RenderConfigSegment(
        string segment,
        string ext,
        IReadOnlyDictionary<string, string> tokens,
        StringBuilder sb,
        ref bool inDoubleQuotes,
        ref bool inSingleQuotes,
        ref int precedingBackslashes)
    {
        for (var i = 0; i < segment.Length; i++)
        {
            var ch = segment[i];
            if (ch is '\r' or '\n')
            {
                if (ext != ".json")
                {
                    inDoubleQuotes = false;
                    inSingleQuotes = false;
                }
                precedingBackslashes = 0;
                sb.Append(ch);
                continue;
            }

            if (ch == '\\' && !inSingleQuotes)
            {
                precedingBackslashes++;
                sb.Append(ch);
                continue;
            }

            if (ch == '"' && !inSingleQuotes)
            {
                if (precedingBackslashes % 2 == 0)
                    inDoubleQuotes = !inDoubleQuotes;
                precedingBackslashes = 0;
                sb.Append(ch);
                continue;
            }

            if (ch == '\'' && ext != ".json" && !inDoubleQuotes)
            {
                precedingBackslashes = 0;
                if (inSingleQuotes && i + 1 < segment.Length && segment[i + 1] == '\'')
                {
                    sb.Append("''");
                    i++;
                    continue;
                }

                inSingleQuotes = !inSingleQuotes;
                sb.Append(ch);
                continue;
            }

            precedingBackslashes = 0;

            if (ch == '{')
            {
                if (TryParseConditional(segment, i, tokens, out var condBody, out var condMatched, out var condClose))
                {
                    if (condMatched)
                        RenderConfigSegment(condBody, ext, tokens, sb, ref inDoubleQuotes, ref inSingleQuotes, ref precedingBackslashes);

                    i = condClose;
                    continue;
                }

                if (TryResolvePlaceholder(segment, i, tokens, out var val, out var close))
                {
                    sb.Append(FormatConfigValue(val, ext, inDoubleQuotes, inSingleQuotes));
                    i = close;
                    continue;
                }
            }

            sb.Append(ch);
        }
    }

    private static bool TryParseConditional(
        string text,
        int startIndex,
        IReadOnlyDictionary<string, string> tokens,
        out string body,
        out bool matched,
        out int closeIndex)
    {
        body = string.Empty;
        matched = false;
        closeIndex = -1;

        if (startIndex + 2 >= text.Length || text[startIndex + 1] != '?')
            return false;

        var colon = text.IndexOf(':', startIndex + 2);
        if (colon <= startIndex + 2)
            return false;

        var key = text[(startIndex + 2)..colon].Trim();
        if (!IsValidTokenIdentifier(key) || !tokens.TryGetValue(key, out var rawVal))
            return false;

        var depth = 1;
        for (var pos = colon + 1; pos < text.Length; pos++)
        {
            if (text[pos] == '{')
            {
                depth++;
            }
            else if (text[pos] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    body = text[(colon + 1)..pos];
                    matched = !string.IsNullOrEmpty(rawVal);
                    closeIndex = pos;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryResolvePlaceholder(
        string text,
        int startIndex,
        IReadOnlyDictionary<string, string> tokens,
        out string value,
        out int closeIndex)
    {
        value = string.Empty;
        closeIndex = -1;

        var close = text.IndexOf('}', startIndex + 1);
        if (close <= startIndex + 1)
            return false;

        var expr = text[(startIndex + 1)..close];
        if (expr.IndexOfAny(['{', '\r', '\n', '"', ':']) >= 0)
            return false;

        var parts = expr.Split('|');
        var primaryKey = parts[0].Trim();
        if (!IsValidTokenIdentifier(primaryKey) || !tokens.ContainsKey(primaryKey))
            return false;

        for (var p = 0; p < parts.Length; p++)
        {
            var part = parts[p].Trim();
            if (IsValidTokenIdentifier(part) && tokens.TryGetValue(part, out var candidate))
            {
                if (!string.IsNullOrEmpty(candidate))
                {
                    value = candidate;
                    break;
                }
            }
            else if (p == parts.Length - 1)
            {
                value = part;
                break;
            }
        }

        closeIndex = close;
        return true;
    }

    private static bool IsValidTokenIdentifier(string key) =>
        key.Length > 0 && key.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    private static string FormatConfigValue(string value, string ext, bool inDoubleQuotes, bool inSingleQuotes)
    {
        if (ext == ".json")
            return JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping).ToString();

        if (inSingleQuotes)
            return NormalizeArgumentsInput(value).Replace("'", "''", StringComparison.Ordinal);

        if (inDoubleQuotes)
        {
            return ext is ".yaml" or ".toml"
                ? JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping).ToString()
                : value.Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("\"", "\\\"", StringComparison.Ordinal)
                    .Replace("\r\n", "\\n", StringComparison.Ordinal)
                    .Replace("\n", "\\n", StringComparison.Ordinal)
                    .Replace("\r", "\\r", StringComparison.Ordinal);
        }

        return NormalizeArgumentsInput(value);
    }

    public static IReadOnlyList<(EConfigType Protocol, IReadOnlyList<Server> Servers, IReadOnlyList<CustomCore> Cores)> GetImportCustomCoreMatches(
        IEnumerable<Server> importedServers)
    {
        ArgumentNullException.ThrowIfNull(importedServers);
        var matches = new List<(EConfigType Protocol, IReadOnlyList<Server> Servers, IReadOnlyList<CustomCore> Cores)>();
        foreach (var group in importedServers
            .Where(s => s != null && s.ConfigType != EConfigType.ExternalCli)
            .GroupBy(s => s.ConfigType)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key))
        {
            var cores = GetForProtocol(group.Key);
            if (cores.Count > 0)
                matches.Add((group.Key, group.ToArray(), cores));
        }
        return matches;
    }

    public static int ApplyCustomCoreSelections(
        IEnumerable<Server> servers,
        IReadOnlyDictionary<EConfigType, string?> selections)
    {
        ArgumentNullException.ThrowIfNull(servers);
        ArgumentNullException.ThrowIfNull(selections);

        var assigned = 0;
        foreach (var server in servers)
        {
            if (server == null || server.ConfigType == EConfigType.ExternalCli)
                continue;
            if (!selections.TryGetValue(server.ConfigType, out var selectedId))
                continue;

            var trimmed = selectedId?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                server.ProtoExtra.CustomCoreId = null;
            }
            else if (IsBuiltInCoreId(trimmed))
            {
                server.ProtoExtra.CustomCoreId = BuiltInCoreId;
                assigned++;
            }
            else if (FindById(trimmed) is { } core && core.Protocol == server.ConfigType)
            {
                server.ProtoExtra.CustomCoreId = core.Id;
                assigned++;
            }
        }
        return assigned;
    }

    public static void Save(CustomCore core)
    {
        ArgumentNullException.ThrowIfNull(core);
        var cores = Global.Settings.CustomCores ??= [];

        core.Id = string.IsNullOrWhiteSpace(core.Id) || IsBuiltInCoreId(core.Id)
            ? Guid.NewGuid().ToString("N")
            : core.Id.Trim();
        core.Name = (core.Name ?? string.Empty).Trim();
        core.ExecutablePath = NormalizeExecutableInput(core.ExecutablePath);
        core.ArgumentsTemplate = NormalizeArgumentsInput(core.ArgumentsTemplate);
        core.ConfigFileExtension = NormalizeConfigExtension(core.ConfigFileExtension);
        core.ConfigTemplate = core.Mode == CustomCoreMode.CliArguments ? NormalizeConfigTemplate(core.ConfigTemplate) : string.Empty;

        foreach (var server in Global.Settings.Server ?? [])
        {
            if (string.Equals(server.ProtoExtra.CustomCoreId, core.Id, StringComparison.OrdinalIgnoreCase) &&
                server.ConfigType != core.Protocol)
            {
                server.ProtoExtra.CustomCoreId = null;
            }
        }

        if (core.IsProtocolDefault)
        {
            foreach (var other in cores)
            {
                if (!string.Equals(other.Id, core.Id, StringComparison.OrdinalIgnoreCase) && other.Protocol == core.Protocol)
                    other.IsProtocolDefault = false;
            }
        }

        var existingIndex = cores.FindIndex(c => string.Equals(c.Id, core.Id, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            cores[existingIndex] = core;
        else
            cores.Add(core);
    }

    public static void Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var trimmed = id.Trim();
        Global.Settings.CustomCores?.RemoveAll(c => string.Equals(c.Id, trimmed, StringComparison.OrdinalIgnoreCase));

        foreach (var server in Global.Settings.Server ?? [])
        {
            if (string.Equals(server.ProtoExtra.CustomCoreId, trimmed, StringComparison.OrdinalIgnoreCase))
                server.ProtoExtra.CustomCoreId = null;
        }

        foreach (var sub in Global.Settings.Subscription ?? [])
        {
            if (string.Equals(sub.CustomCoreId, trimmed, StringComparison.OrdinalIgnoreCase))
                sub.CustomCoreId = null;
        }
    }

    public static void NormalizeAll(Setting settings)
    {
        settings.CustomCores ??= [];
        settings.CustomCores.RemoveAll(c => c == null || c.Protocol == EConfigType.ExternalCli || !Enum.IsDefined(c.Protocol));

        var defaultProtocols = new HashSet<EConfigType>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var core in settings.CustomCores)
        {
            var trimmedId = core.Id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedId) ||
                IsBuiltInCoreId(trimmedId) ||
                !seenIds.Add(trimmedId))
            {
                core.Id = Guid.NewGuid().ToString("N");
                seenIds.Add(core.Id);
            }
            else
            {
                core.Id = trimmedId;
            }

            core.Name = (core.Name ?? string.Empty).Trim();
            core.ExecutablePath = NormalizeExecutableInput(core.ExecutablePath);
            if (!Enum.IsDefined(core.Mode))
                core.Mode = CustomCoreMode.CliArguments;
            core.ArgumentsTemplate = NormalizeArgumentsInput(core.ArgumentsTemplate);
            core.ConfigFileExtension = NormalizeConfigExtension(core.ConfigFileExtension);
            core.ConfigTemplate = core.Mode == CustomCoreMode.CliArguments ? NormalizeConfigTemplate(core.ConfigTemplate) : string.Empty;

            if (core.IsProtocolDefault && !defaultProtocols.Add(core.Protocol))
                core.IsProtocolDefault = false;
        }
    }

    private static string FormatWindowsArgValue(string value, bool alreadyQuoted, bool followedByQuote)
    {
        if (alreadyQuoted)
            return EscapeEmbeddedQuotes(value, doubleTrailingBackslashes: followedByQuote);

        if (value.Length > 0 && !value.Any(c => char.IsWhiteSpace(c) || c == '"'))
            return value;

        return "\"" + EscapeEmbeddedQuotes(value, doubleTrailingBackslashes: true) + "\"";
    }

    private static string EscapeEmbeddedQuotes(string value, bool doubleTrailingBackslashes)
    {
        var sb = new StringBuilder(value.Length);
        var backslashes = 0;
        foreach (var c in value)
        {
            if (c == '\\')
            {
                backslashes++;
            }
            else if (c == '"')
            {
                sb.Append('\\', backslashes * 2 + 1);
                sb.Append('"');
                backslashes = 0;
            }
            else
            {
                if (backslashes > 0)
                {
                    sb.Append('\\', backslashes);
                    backslashes = 0;
                }
                sb.Append(c);
            }
        }

        if (backslashes > 0)
            sb.Append('\\', doubleTrailingBackslashes ? backslashes * 2 : backslashes);

        return sb.ToString();
    }
}

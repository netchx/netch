using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public static class V2rayUtils
{
    public static IEnumerable<Server> ParseVUri(string text)
    {
        var uri = ProxyUri.Parse(text, "vmess", "vless");
        var scheme = uri.Scheme.ToLowerInvariant();
        var parameter = ProxyUri.Query(uri);
        VMessServer server = scheme == "vless" ? new VLESSServer() : new VMessServer();
        server.UserID = ProxyUri.UserInfo(uri);
        server.Hostname = ProxyUri.HostName(uri);
        server.Port = (ushort)uri.Port;
        server.Remark = ProxyUri.Remark(uri);
        server.TransferProtocol = (parameter["type"] ?? "tcp").ToLowerInvariant();
        server.PacketEncoding = parameter["packetEncoding"] ?? "xudp";
        server.EncryptMethod = parameter["encryption"] ?? (scheme == "vless" ? "none" : "auto");
        server.TLSSecureType = (parameter["security"] ?? "none").ToLowerInvariant();
        server.ServerName = parameter["sni"];
        server.Alpn = parameter["alpn"];

        if (!string.IsNullOrEmpty(parameter["fp"]) || !string.IsNullOrEmpty(parameter["pbk"]) ||
            !string.IsNullOrEmpty(parameter["sid"]) || server.TLSSecureType == "reality")
            throw new NotSupportedException("The bundled core does not support TLS fingerprints or REALITY.");

        if (string.IsNullOrWhiteSpace(server.UserID) ||
            !VMessGlobal.TransferProtocols.Contains(server.TransferProtocol) ||
            !VMessGlobal.PacketEncodings.Contains(server.PacketEncoding) ||
            (scheme == "vless" && server.EncryptMethod != "none") ||
            (scheme == "vmess" && !VMessGlobal.EncryptMethods.Contains(server.EncryptMethod)) ||
            server.TLSSecureType is not ("none" or "tls" or "xtls") ||
            (scheme == "vmess" && server.TLSSecureType == "xtls"))
            throw new NotSupportedException("Unsupported VMess/VLESS link settings");

        var flow = parameter["flow"];
        if (!string.IsNullOrEmpty(flow) && (scheme != "vless" || server.TLSSecureType != "xtls" || !VLESSGlobal.Flows.Contains(flow)))
            throw new NotSupportedException($"Unsupported VLESS flow: {flow}");
        if (server is VLESSServer vless && !string.IsNullOrEmpty(flow))
            vless.Flow = flow;
        if (server.TLSSecureType == "xtls" && server.TransferProtocol != "tcp")
            throw new NotSupportedException("XTLS requires TCP transport.");

        if (scheme == "vmess" && !string.IsNullOrEmpty(parameter["aid"]) &&
            (!int.TryParse(parameter["aid"], out var alterId) || alterId < 0))
            throw new FormatException("Invalid VMess alter ID");
        if (scheme == "vmess" && int.TryParse(parameter["aid"], out var parsedAlterId))
            server.AlterID = parsedAlterId;

        switch (server.TransferProtocol)
        {
            case "tcp":
                server.FakeType = parameter["headerType"] ?? "none";
                server.Path = parameter["path"];
                server.Host = parameter["host"];
                break;
            case "kcp":
                server.FakeType = parameter["headerType"] ?? "none";
                server.Path = parameter["seed"];
                break;
            case "ws":
            case "h2":
                server.Path = parameter["path"] ?? "/";
                server.Host = parameter["host"];
                break;
            case "quic":
                server.QUICSecure = parameter["quicSecurity"] ?? "none";
                server.QUICSecret = parameter["key"];
                server.FakeType = parameter["headerType"] ?? "none";
                break;
            case "grpc":
                server.FakeType = parameter["mode"] ?? "gun";
                server.Path = parameter["serviceName"];
                if (server.FakeType is not ("gun" or "multi"))
                    throw new NotSupportedException($"Unsupported gRPC mode: {server.FakeType}");
                break;
        }

        if (server is VLESSServer parsedVless ? !new VLESSUtil().CheckServer(parsedVless) : !new VMessUtil().CheckServer(server))
            throw new NotSupportedException("Unsupported VMess/VLESS transport settings");

        return new[] { server };
    }

    public static string GetVShareLink(Server s, string scheme = "vmess")
    {
        // https://github.com/XTLS/Xray-core/issues/91
        var server = (VMessServer)s;
        var parameter = new Dictionary<string, string>();
        // protocol-specific fields
        parameter.Add("type", server.TransferProtocol);
        parameter.Add("encryption", server.EncryptMethod);
        parameter.Add("packetEncoding", server.PacketEncoding);
        if (scheme == "vmess" && server.AlterID > 0)
            parameter.Add("aid", server.AlterID.ToString());

        // transport-specific fields
        switch (server.TransferProtocol)
        {
            case "tcp":
                if (server.FakeType != "none")
                    parameter.Add("headerType", server.FakeType);
                if (!server.Path.IsNullOrWhiteSpace())
                    parameter.Add("path", server.Path!);
                if (!server.Host.IsNullOrWhiteSpace())
                    parameter.Add("host", server.Host!);
                break;
            case "kcp":
                if (server.FakeType != "none")
                    parameter.Add("headerType", server.FakeType);

                if (!server.Path.IsNullOrWhiteSpace())
                    parameter.Add("seed", server.Path!);

                break;
            case "ws":
                parameter.Add("path", server.Path.ValueOrDefault() ?? "/");
                if (!server.Host.IsNullOrWhiteSpace())
                    parameter.Add("host", server.Host!);

                break;
            case "h2":
                parameter.Add("path", server.Path.ValueOrDefault() ?? "/");
                if (!server.Host.IsNullOrWhiteSpace())
                    parameter.Add("host", server.Host!);

                break;
            case "quic":
                if (server.QUICSecure is not (null or "none"))
                {
                    parameter.Add("quicSecurity", server.QUICSecure);
                    parameter.Add("key", server.QUICSecret ?? "");
                }

                if (server.FakeType != "none")
                    parameter.Add("headerType", server.FakeType);

                break;
            case "grpc":
                if (!string.IsNullOrEmpty(server.Path))
                    parameter.Add("serviceName", server.Path);

                if (server.FakeType is "gun" or "multi")
                    parameter.Add("mode", server.FakeType);

                break;
        }

        if (server.TLSSecureType != "none")
        {
            parameter.Add("security", server.TLSSecureType);

            var serverName = server.ServerName.ValueOrDefault() ?? server.Host.ValueOrDefault();
            if (serverName != null)
                parameter.Add("sni", serverName);
            if (!server.Alpn.IsNullOrWhiteSpace())
                parameter.Add("alpn", server.Alpn!);

            if (server.TLSSecureType == "xtls")
            {
                parameter.Add("flow", server is VLESSServer vless ? vless.Flow : "xtls-rprx-direct");
            }
        }

        return
            $"{scheme}://{Uri.EscapeDataString(server.UserID)}@{ProxyUri.Host(server.Hostname)}:{server.Port}?{string.Join("&", parameter.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))}{(!server.Remark.IsNullOrWhiteSpace() ? $"#{Uri.EscapeDataString(server.Remark)}" : "")}";
    }
}

using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class TrojanUtil : IServerUtil
{
    public ushort Priority { get; } = 3;

    public string TypeName { get; } = "Trojan";

    public string FullName { get; } = "Trojan";

    public string ShortName { get; } = "TR";

    public string[] UriScheme { get; } = { "trojan" };

    public Type ServerType { get; } = typeof(TrojanServer);

    public void Edit(Server s)
    {
        new TrojanForm((TrojanServer)s).ShowDialog();
    }

    public void Create()
    {
        new TrojanForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        var server = (TrojanServer)s;
        var parameters = new Dictionary<string, string>
        {
            ["security"] = server.TLSSecureType,
            ["type"] = server.TransferProtocol
        };
        if (!string.IsNullOrWhiteSpace(server.Host))
            parameters["sni"] = server.Host;
        if (!string.IsNullOrWhiteSpace(server.Alpn))
            parameters["alpn"] = server.Alpn;
        if (!string.IsNullOrWhiteSpace(server.TransportHost))
            parameters["host"] = server.TransportHost;
        if (!string.IsNullOrWhiteSpace(server.Path))
            parameters[server.TransferProtocol == "grpc" ? "serviceName" : "path"] = server.Path;
        if (server.TLSSecureType == "xtls")
            parameters["flow"] = server.Flow;

        var query = string.Join("&", parameters.Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value)}"));
        var remark = string.IsNullOrEmpty(server.Remark) ? "" : "#" + Uri.EscapeDataString(server.Remark);
        return $"trojan://{Uri.EscapeDataString(server.Password)}@{ProxyUri.Host(server.Hostname)}:{server.Port}?{query}{remark}";
    }

    public IServerController GetController()
    {
        return new TrojanController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var uri = ProxyUri.Parse(text, "trojan");
        var parameters = ProxyUri.Query(uri);
        var data = new TrojanServer
        {
            Password = ProxyUri.UserInfo(uri),
            Hostname = ProxyUri.HostName(uri),
            Port = (ushort)uri.Port,
            Remark = ProxyUri.Remark(uri),
            Host = parameters["sni"] ?? parameters["peer"],
            TLSSecureType = parameters["security"] ?? "tls",
            TransferProtocol = parameters["type"] ?? "tcp",
            TransportHost = parameters["host"],
            Path = parameters["serviceName"] ?? parameters["path"],
            Alpn = parameters["alpn"],
            Flow = parameters["flow"] ?? "xtls-rprx-direct"
        };

        if (!string.IsNullOrEmpty(parameters["fp"]) || !string.IsNullOrEmpty(parameters["pbk"]) ||
            !string.IsNullOrEmpty(parameters["sid"]))
            throw new NotSupportedException("The bundled core does not support TLS fingerprints or REALITY.");
        if (!string.IsNullOrEmpty(parameters["flow"]) && data.TLSSecureType != "xtls")
            throw new NotSupportedException($"Unsupported Trojan flow: {parameters["flow"]}");
        if (!CheckServer(data))
            throw new NotSupportedException("Unsupported Trojan link settings");

        return new[] { data };
    }

    public bool CheckServer(Server s)
    {
        var server = (TrojanServer)s;
        return !string.IsNullOrWhiteSpace(server.Hostname) && server.Port != 0 &&
               !string.IsNullOrWhiteSpace(server.Password) &&
               server.TLSSecureType is "none" or "tls" or "xtls" &&
               server.TransferProtocol is "tcp" or "ws" or "grpc" or "h2" &&
               (server.TLSSecureType != "xtls" ||
                server.TransferProtocol == "tcp" && VLESSGlobal.Flows.Contains(server.Flow));
    }
}

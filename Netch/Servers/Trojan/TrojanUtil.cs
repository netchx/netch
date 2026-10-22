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
        if (!string.IsNullOrWhiteSpace(server.Fingerprint))
            parameters["fp"] = server.Fingerprint;
        if (server.TLSSecureType == "reality")
        {
            parameters["pbk"] = server.PublicKey ?? "";
            if (!string.IsNullOrWhiteSpace(server.ShortId))
                parameters["sid"] = server.ShortId;
            if (!string.IsNullOrWhiteSpace(server.SpiderX))
                parameters["spx"] = server.SpiderX;
        }
        if (!string.IsNullOrWhiteSpace(server.TransportHost))
            parameters["host"] = server.TransportHost;
        if (!string.IsNullOrWhiteSpace(server.Path))
            parameters[server.TransferProtocol == "grpc" ? "serviceName" : "path"] = server.Path;
        if (server.TransferProtocol == "xhttp" && !string.IsNullOrWhiteSpace(server.TransportMode))
            parameters["mode"] = server.TransportMode;
        if (server.TransferProtocol == "xhttp" && !string.IsNullOrWhiteSpace(server.TransportExtra))
            parameters["extra"] = server.TransportExtra;
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
            TLSSecureType = (parameters["security"] ?? "tls").ToLowerInvariant(),
            TransferProtocol = (parameters["type"] ?? "tcp").ToLowerInvariant(),
            TransportHost = parameters["host"],
            Path = parameters["serviceName"] ?? parameters["path"],
            Alpn = parameters["alpn"],
            Fingerprint = parameters["fp"],
            PublicKey = parameters["pbk"],
            ShortId = parameters["sid"],
            SpiderX = parameters["spx"],
            TransportMode = parameters["mode"],
            TransportExtra = parameters["extra"],
            Flow = parameters["flow"] ?? "xtls-rprx-direct"
        };

        data.TransferProtocol = data.TransferProtocol switch
        {
            "raw" => "tcp",
            "websocket" => "ws",
            "splithttp" => "xhttp",
            _ => data.TransferProtocol
        };

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
               server.TLSSecureType is "none" or "tls" or "xtls" or "reality" &&
               server.TransferProtocol is "tcp" or "ws" or "grpc" or "h2" or "xhttp" or "httpupgrade" &&
               (server.TLSSecureType != "reality" ||
                CoreSelector.ValidReality(server.PublicKey, server.ShortId) && server.TransferProtocol is ("tcp" or "grpc" or "xhttp")) &&
               (server.TLSSecureType == "reality" || string.IsNullOrWhiteSpace(server.PublicKey) &&
                string.IsNullOrWhiteSpace(server.ShortId) && string.IsNullOrWhiteSpace(server.SpiderX)) &&
               (server.TLSSecureType != "reality" || string.IsNullOrWhiteSpace(server.Alpn)) &&
               (server.TLSSecureType != "none" || string.IsNullOrWhiteSpace(server.Fingerprint)) &&
               (string.IsNullOrWhiteSpace(server.Fingerprint) || CoreSelector.UseXray(server)) &&
               (server.TransferProtocol is not ("xhttp" or "httpupgrade") || CoreSelector.UseXray(server)) &&
               (server.TransferProtocol == "xhttp" ? CoreSelector.ValidXhttpExtra(server.TransportExtra) :
                string.IsNullOrWhiteSpace(server.TransportExtra)) &&
               (server.TLSSecureType != "xtls" ||
                server.TransferProtocol == "tcp" && server.Flow is ("xtls-rprx-direct" or "xtls-rprx-origin" or "xtls-rprx-splice"));
    }
}

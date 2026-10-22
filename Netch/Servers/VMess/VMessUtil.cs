using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class VMessUtil : IServerUtil
{
    public ushort Priority { get; } = 3;

    public string TypeName { get; } = "VMess";

    public string FullName { get; } = "VMess";

    public string ShortName { get; } = "V2";

    public string[] UriScheme { get; } = { "vmess" };

    public Type ServerType { get; } = typeof(VMessServer);

    public void Edit(Server s)
    {
        new VMessForm((VMessServer)s).ShowDialog();
    }

    public void Create()
    {
        new VMessForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        if (Global.Settings.V2RayConfig.V2rayNShareLink)
        {
            var server = (VMessServer)s;
            JsonElement? extra = null;
            if (!string.IsNullOrWhiteSpace(server.TransportExtra))
            {
                using var document = JsonDocument.Parse(server.TransportExtra);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    throw new FormatException("XHTTP extra must be a JSON object");
                extra = document.RootElement.Clone();
            }

            var vmessJson = JsonSerializer.Serialize(new V2rayNJObject
                {
                    v = 2,
                    ps = server.Remark,
                    add = server.Hostname,
                    port = server.Port,
                    scy = server.EncryptMethod,
                    id = server.UserID,
                    aid = server.AlterID,
                    net = server.TransferProtocol,
                    type = server.FakeType,
                    host = server.Host ?? "",
                    path = server.Path ?? "",
                    tls = server.TLSSecureType,
                    sni = server.ServerName ?? "",
                    alpn = server.Alpn ?? "",
                    fp = server.Fingerprint ?? "",
                    pbk = server.PublicKey ?? "",
                    sid = server.ShortId ?? "",
                    spx = server.SpiderX ?? "",
                    mode = server.TransportMode ?? "",
                    extra = extra
                },
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

            return "vmess://" + ShareLink.URLSafeBase64Encode(vmessJson);
        }

        return V2rayUtils.GetVShareLink(s);
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        V2rayNJObject vmess;
        try
        {
            var json = ShareLink.URLSafeBase64Decode(text["vmess://".Length..]);
            vmess = JsonSerializer.Deserialize<V2rayNJObject>(json,
                new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString })
                ?? throw new FormatException();
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return V2rayUtils.ParseVUri(text);
        }

        var data = new VMessServer();

        data.Remark = vmess.ps;
        data.Hostname = vmess.add;
        data.EncryptMethod = string.IsNullOrWhiteSpace(vmess.scy) ? "auto" : vmess.scy;
        data.Port = vmess.port;
        data.UserID = vmess.id;
        data.AlterID = vmess.aid;
        data.TransferProtocol = string.IsNullOrWhiteSpace(vmess.net) ? "tcp" : vmess.net;
        data.TransferProtocol = data.TransferProtocol.ToLowerInvariant() switch
        {
            "raw" => "tcp",
            "websocket" => "ws",
            "splithttp" => "xhttp",
            var transport => transport
        };
        data.FakeType = string.IsNullOrWhiteSpace(vmess.type) ? "none" : vmess.type;
        data.ServerName = vmess.sni;
        data.Alpn = vmess.alpn;
        data.Fingerprint = vmess.fp;
        data.PublicKey = vmess.pbk;
        data.ShortId = vmess.sid;
        data.SpiderX = vmess.spx;
        data.TransportMode = vmess.mode;
        data.TransportExtra = vmess.extra?.ValueKind switch
        {
            JsonValueKind.Object => vmess.extra.Value.GetRawText(),
            JsonValueKind.String => vmess.extra.Value.GetString(),
            JsonValueKind.Null or null => null,
            _ => throw new FormatException("Invalid XHTTP extra")
        };

        if (data.TransferProtocol == "quic")
        {
            data.QUICSecure = string.IsNullOrWhiteSpace(vmess.host) ? "none" : vmess.host;
            data.QUICSecret = vmess.path;
        }
        else
        {
            data.Host = vmess.host;
            data.Path = vmess.path;
        }

        data.TLSSecureType = vmess.tls;

        return CheckServer(data) ? new[] { data } : throw new NotSupportedException("Unsupported VMess link settings");
    }

    public bool CheckServer(Server s)
    {
        var server = (VMessServer)s;
        return !string.IsNullOrWhiteSpace(server.Hostname) && server.Port != 0 &&
               !string.IsNullOrWhiteSpace(server.UserID) &&
               VMessGlobal.EncryptMethods.Contains(server.EncryptMethod) &&
               VMessGlobal.TransferProtocols.Contains(server.TransferProtocol) &&
               VMessGlobal.TLSSecure.Contains(server.TLSSecureType) &&
               (server.TLSSecureType != "reality" || CoreSelector.ValidReality(server.PublicKey, server.ShortId) &&
                server.TransferProtocol is ("tcp" or "grpc" or "xhttp")) &&
               (server.TLSSecureType == "reality" || string.IsNullOrWhiteSpace(server.PublicKey) &&
                string.IsNullOrWhiteSpace(server.ShortId) && string.IsNullOrWhiteSpace(server.SpiderX)) &&
               (server.TLSSecureType != "reality" || string.IsNullOrWhiteSpace(server.Alpn)) &&
               (server.TLSSecureType != "none" || string.IsNullOrWhiteSpace(server.Fingerprint)) &&
               (string.IsNullOrWhiteSpace(server.Fingerprint) || CoreSelector.UseXray(server)) &&
               (server.TLSSecureType != "reality" || CoreSelector.UseXray(server)) &&
               (server.TransferProtocol is not ("xhttp" or "httpupgrade") || CoreSelector.UseXray(server)) &&
               (server.TransferProtocol == "xhttp" ? CoreSelector.ValidXhttpExtra(server.TransportExtra) :
                string.IsNullOrWhiteSpace(server.TransportExtra)) &&
               VMessGlobal.PacketEncodings.Contains(server.PacketEncoding) &&
               (server.TransferProtocol != "tcp" || server.FakeType is "none" or "http") &&
               (server.TransferProtocol != "grpc" || server.FakeType is "gun" or "multi") &&
               (server.TransferProtocol != "quic" || VMessGlobal.QUIC.Contains(server.QUICSecure ?? "none"));
    }
}

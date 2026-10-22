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
                    alpn = server.Alpn ?? ""
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
            using var document = JsonDocument.Parse(json);
            if (new[] { "fp", "pbk", "sid" }.Any(name =>
                    document.RootElement.TryGetProperty(name, out var value) &&
                    value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(value.GetString())))
                throw new NotSupportedException("The bundled core does not support TLS fingerprints or REALITY.");
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
        data.FakeType = string.IsNullOrWhiteSpace(vmess.type) ? "none" : vmess.type;
        data.ServerName = vmess.sni;
        data.Alpn = vmess.alpn;

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
               VMessGlobal.PacketEncodings.Contains(server.PacketEncoding) &&
               (server.TransferProtocol != "tcp" || server.FakeType is "none" or "http") &&
               (server.TransferProtocol != "grpc" || server.FakeType is "gun" or "multi") &&
               (server.TransferProtocol != "quic" || VMessGlobal.QUIC.Contains(server.QUICSecure ?? "none"));
    }
}

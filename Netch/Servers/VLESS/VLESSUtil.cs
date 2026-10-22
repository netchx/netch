using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class VLESSUtil : IServerUtil
{
    public ushort Priority { get; } = 2;

    public string TypeName { get; } = "VLESS";

    public string FullName { get; } = "VLESS";

    public string ShortName { get; } = "VL";

    public string[] UriScheme { get; } = { "vless" };

    public Type ServerType { get; } = typeof(VLESSServer);

    public void Edit(Server s)
    {
        new VLESSForm((VLESSServer)s).ShowDialog();
    }

    public void Create()
    {
        new VLESSForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        return V2rayUtils.GetVShareLink(s, "vless");
    }

    public IServerController GetController()
    {
        return new V2rayController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        return V2rayUtils.ParseVUri(text);
    }

    public bool CheckServer(Server s)
    {
        var server = (VLESSServer)s;
        return !string.IsNullOrWhiteSpace(server.Hostname) && server.Port != 0 &&
               !string.IsNullOrWhiteSpace(server.UserID) && server.EncryptMethod == "none" &&
               VLESSGlobal.TransferProtocols.Contains(server.TransferProtocol) &&
               VLESSGlobal.TLSSecure.Contains(server.TLSSecureType) &&
               (server.TLSSecureType != "reality" || CoreSelector.ValidReality(server.PublicKey, server.ShortId) &&
                server.TransferProtocol is ("tcp" or "grpc" or "xhttp")) &&
               (server.TLSSecureType == "reality" || string.IsNullOrWhiteSpace(server.PublicKey) &&
                string.IsNullOrWhiteSpace(server.ShortId) && string.IsNullOrWhiteSpace(server.SpiderX)) &&
               (server.TLSSecureType != "reality" || string.IsNullOrWhiteSpace(server.Alpn)) &&
               (server.TLSSecureType != "none" || string.IsNullOrWhiteSpace(server.Fingerprint)) &&
               (string.IsNullOrWhiteSpace(server.Fingerprint) || CoreSelector.UseXray(server)) &&
               (server.TLSSecureType != "reality" || CoreSelector.UseXray(server)) &&
               (server.Flow != "xtls-rprx-vision" || CoreSelector.UseXray(server)) &&
               (server.TransferProtocol is not ("xhttp" or "httpupgrade") || CoreSelector.UseXray(server)) &&
               (server.TransferProtocol == "xhttp" ? CoreSelector.ValidXhttpExtra(server.TransportExtra) :
                string.IsNullOrWhiteSpace(server.TransportExtra)) &&
               VMessGlobal.PacketEncodings.Contains(server.PacketEncoding) &&
               (server.TransferProtocol != "tcp" || server.FakeType is "none" or "http") &&
               (server.TransferProtocol != "grpc" || server.FakeType is "gun" or "multi") &&
               (server.TransferProtocol != "quic" || VLESSGlobal.QUIC.Contains(server.QUICSecure ?? "none")) &&
               (server.TLSSecureType == "xtls"
                   ? server.TransferProtocol == "tcp" && server.Flow is ("xtls-rprx-direct" or "xtls-rprx-origin" or "xtls-rprx-splice")
                   : server.Flow == "xtls-rprx-direct" ||
                     server.Flow == "xtls-rprx-vision" && server.TransferProtocol == "tcp" &&
                     server.TLSSecureType is ("tls" or "reality"));
    }
}

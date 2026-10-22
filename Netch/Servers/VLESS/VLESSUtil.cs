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
               VMessGlobal.PacketEncodings.Contains(server.PacketEncoding) &&
               (server.TransferProtocol != "tcp" || server.FakeType is "none" or "http") &&
               (server.TransferProtocol != "grpc" || server.FakeType is "gun" or "multi") &&
               (server.TransferProtocol != "quic" || VLESSGlobal.QUIC.Contains(server.QUICSecure ?? "none")) &&
               (server.TLSSecureType != "xtls" ||
                server.TransferProtocol == "tcp" && VLESSGlobal.Flows.Contains(server.Flow));
    }
}

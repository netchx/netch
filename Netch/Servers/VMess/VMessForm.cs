using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class VMessForm : ServerForm
{
    public VMessForm(VMessServer? server = default)
    {
        server ??= new VMessServer();
        Server = server;
        CreateTextBox("Sni", "ServerName(Sni)", s => true, s => server.ServerName = s, server.ServerName);
        CreateTextBox("Alpn", "ALPN", s => true, s => server.Alpn = s, server.Alpn);
        CreateTextBox("Fingerprint", "TLS Fingerprint", s => true, s => server.Fingerprint = s, server.Fingerprint);
        CreateTextBox("PublicKey", "REALITY Public Key", s => true, s => server.PublicKey = s, server.PublicKey);
        CreateTextBox("ShortId", "REALITY Short ID", s => true, s => server.ShortId = s, server.ShortId);
        CreateTextBox("SpiderX", "REALITY SpiderX", s => true, s => server.SpiderX = s, server.SpiderX);
        CreateTextBox("TransportMode", "XHTTP Mode", s => true, s => server.TransportMode = s, server.TransportMode);
        CreateTextBox("TransportExtra", "XHTTP Extra (JSON)", s => true, s => server.TransportExtra = s, server.TransportExtra);
        CreateTextBox("UserId", "User ID", s => true, s => server.UserID = s, server.UserID);
        CreateTextBox("AlterId", "Alter ID", s => int.TryParse(s, out _), s => server.AlterID = int.Parse(s), server.AlterID.ToString(), 76);
        CreateComboBox("EncryptMethod", "Encrypt Method", VMessGlobal.EncryptMethods, s => server.EncryptMethod = s, server.EncryptMethod);
        CreateComboBox("TransferProtocol",
            "Transfer Protocol",
            VMessGlobal.TransferProtocols,
            s => server.TransferProtocol = s,
            server.TransferProtocol);
        CreateComboBox("PacketEncoding",
            "Packet Encoding",
            VMessGlobal.PacketEncodings,
            s => server.PacketEncoding = s,
            server.PacketEncoding);
        

        CreateComboBox("FakeType", "Fake Type", VMessGlobal.FakeTypes, s => server.FakeType = s, server.FakeType);
        CreateTextBox("Host", "Host", s => true, s => server.Host = s, server.Host);
        CreateTextBox("Path", "Path", s => true, s => server.Path = s, server.Path);
        CreateComboBox("QUICSecurity", "QUIC Security", VMessGlobal.QUIC, s => server.QUICSecure = s, server.QUICSecure);
        CreateTextBox("QUICSecret", "QUIC Secret", s => true, s => server.QUICSecret = s, server.QUICSecret);
        CreateComboBox("UseMux",
            "Use Mux",
            new List<string> { "", "true", "false" },
            s => server.UseMux = s switch { "" => null, "true" => true, "false" => false, _ => null },
            server.UseMux?.ToString().ToLower() ?? "");

        CreateComboBox("TLSSecure", "TLS Secure", VMessGlobal.TLSSecure, s => server.TLSSecureType = s, server.TLSSecureType);
    }

    protected override string TypeName { get; } = "VMess";
}

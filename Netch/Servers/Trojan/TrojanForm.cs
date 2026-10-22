using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class TrojanForm : ServerForm
{
    public TrojanForm(TrojanServer? server = default)
    {
        server ??= new TrojanServer();
        Server = server;
        CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password);
        CreateTextBox("Host", "Host", s => true, s => server.Host = s, server.Host);
        CreateComboBox("TransferProtocol", "Transfer Protocol", new List<string> { "tcp", "ws", "grpc", "h2", "httpupgrade", "xhttp" },
            s => server.TransferProtocol = s, server.TransferProtocol);
        CreateTextBox("TransportHost", "Transport Host", s => true, s => server.TransportHost = s, server.TransportHost);
        CreateTextBox("Path", "Path / Service Name", s => true, s => server.Path = s, server.Path);
        CreateTextBox("Alpn", "ALPN", s => true, s => server.Alpn = s, server.Alpn);
        CreateTextBox("Fingerprint", "TLS Fingerprint", s => true, s => server.Fingerprint = s, server.Fingerprint);
        CreateTextBox("PublicKey", "REALITY Public Key", s => true, s => server.PublicKey = s, server.PublicKey);
        CreateTextBox("ShortId", "REALITY Short ID", s => true, s => server.ShortId = s, server.ShortId);
        CreateTextBox("SpiderX", "REALITY SpiderX", s => true, s => server.SpiderX = s, server.SpiderX);
        CreateTextBox("TransportMode", "XHTTP Mode", s => true, s => server.TransportMode = s, server.TransportMode);
        CreateTextBox("TransportExtra", "XHTTP Extra (JSON)", s => true, s => server.TransportExtra = s, server.TransportExtra);
        CreateComboBox("TLSSecure", "TLS Secure", VLESSGlobal.TLSSecure, s => server.TLSSecureType = s, server.TLSSecureType);
        CreateComboBox("Flow", "XTLS Flow", VLESSGlobal.Flows, s => server.Flow = s, server.Flow);
    }

    protected override string TypeName { get; } = "Trojan";
}

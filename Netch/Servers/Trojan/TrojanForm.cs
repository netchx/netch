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
        CreateComboBox("Protocol", "Protocol", TrojanGoGlobal.TransportProtocol, s => server.Protocol = s, server.Protocol);
        CreateTextBox("WebsocketPath", "Path", s => true, s => server.WebsocketPath = s, server.WebsocketPath);
        CreateTextBox("WebsocketHost", "Websocket Host", s => true, s => server.WebsocketHost = s, server.WebsocketHost);
        CreateComboBox("Encryption", "Encryption", TrojanGoGlobal.TrojanGoEncryptMethod, s => server.Encryption = s, server.Encryption);
        CreateComboBox("ShadowsocksEncryption", "Encrypt Method", TrojanGoGlobal.ShadowsocksEncryptMethod, s => server.ShadowsocksEncryption = s, server.ShadowsocksEncryption);
        CreateTextBox("ShadowsocksPassword", "Password", s => true, s => server.ShadowsocksPassword = s, server.ShadowsocksPassword);
    }

    protected override string TypeName { get; } = "Trojan";
}
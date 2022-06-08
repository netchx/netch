using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
internal class HysteriaForm : ServerForm
{
    public HysteriaForm(HysteriaServer? server = default)
    {
        server ??= new HysteriaServer();
        Server = server;
        CreateComboBox("Protocol", "Protocol", HysteriaGlobal.Protocol, s => server.Protocol = s, server.Protocol);
        CreateTextBox("OBFS", "OBFS", s => true, s => server.OBFS = s, server.OBFS);
        CreateTextBox("ALPN", "ALPN", s => true, s => server.ALPN = s, server.ALPN);
        CreateComboBox("AuthType", "Auth Type", HysteriaGlobal.Auth_Type, s => server.AuthType = s, server.AuthType);
        CreateTextBox("AuthPayload", "Auth Payload", s => true, s => server.AuthPayload = s, server.AuthPayload);
        CreateTextBox("ServerName", "ServerName(Sni)", s => true, s => server.ServerName = s, server.ServerName);
        CreateComboBox("Insecure", "Insecure", HysteriaGlobal.Insecure, s => server.Insecure = s, server.Insecure);
        CreateTextBox("UpMbps", "UpMbps", s => int.TryParse(s, out _), s => server.UpMbps = int.Parse(s), server.UpMbps.ToString(), 76);
        CreateTextBox("DownMbps", "DownMbps", s => int.TryParse(s, out _), s => server.DownMbps = int.Parse(s), server.DownMbps.ToString(), 76);
        CreateTextBox("RecvWindowConn", "RecvWindowConn", s => int.TryParse(s, out _), s => server.RecvWindowConn = int.Parse(s), server.RecvWindowConn.ToString(), 76);
        CreateTextBox("RecvWindow", "RecvWindow", s => int.TryParse(s, out _), s => server.RecvWindow = int.Parse(s), server.RecvWindow.ToString(), 76);
        CreateComboBox("DisableMTUDiscovery", "Disable MTU\nDiscovery", HysteriaGlobal.Disable_MTU_Discovery, s => server.DisableMTUDiscovery = s, server.DisableMTUDiscovery);
    }

    protected override string TypeName { get; } = "Hysteria";
}
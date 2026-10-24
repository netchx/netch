using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class Hysteria2Form : ServerForm
{
    public Hysteria2Form(Hysteria2Server? server = default)
    {
        server ??= new Hysteria2Server();
        Server = server;

        CreateTextBox("Auth", "Auth", s => true, s => server.Password = s, server.Password);
        CreateComboBox("Obfs", "Obfs", Constants.Hysteria2Obfs, s => server.ProtoExtra.Obfs = s, server.ProtoExtra.Obfs);
        CreateTextBox("ObfsPassword", "Obfs Password", s => true, s => server.ProtoExtra.SalamanderPass = s, server.ProtoExtra.SalamanderPass);
        CreateTextBox("PortHoppingRange", "Port Hopping Range", s => true, s => server.ProtoExtra.Ports = s, server.ProtoExtra.Ports);
        CreateTextBox("CertSha", "CertSha", s => true, s => server.CertSha = s, server.CertSha);
    }

    protected override EConfigType TypeName { get; } = EConfigType.Hysteria2;
}

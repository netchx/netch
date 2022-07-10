using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class SSHForm : ServerForm
{
    public SSHForm(SSHServer? server = default)
    {
        server ??= new SSHServer();
        Server = server;
        CreateTextBox("User", "User", s => true, s => server.User = s, server.User);
        CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password);
        CreateTextBox("PrivateKey", "Private Key", s => true, s => server.PrivateKey = s, server.PrivateKey);
        CreateTextBox("PublicKey", "Public Key", s => true, s => server.PublicKey = s, server.PublicKey);
    }

    protected override string TypeName { get; } = "SSH";
}
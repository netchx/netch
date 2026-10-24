using Netch.Enums;
using Netch.Forms;
using Netch.Utils;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class SocksForm : ServerForm
{
    public SocksForm(SocksServer? server = default)
    {
        server ??= new SocksServer();
        Server = server;
        CreateTextBox("Username", "Username", s => true, s => server.Username = s, server.Username.ValueOrDefault());
        CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password.ValueOrDefault());

    }

    protected override EConfigType TypeName { get; } = EConfigType.SOCKS;
}
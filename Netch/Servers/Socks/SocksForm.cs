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
        var username = CreateTextBox("Username", "Username", _ => true,
            s => server.Username = string.IsNullOrWhiteSpace(s) ? "" : s, server.Username.ValueOrDefault()).Field;
        var password = CreateTextBox("Password", "Password", _ => true,
            s => server.Password = string.IsNullOrWhiteSpace(s) ? "" : s, server.Password.ValueOrDefault()).Field;
        CheckField(username, s => string.IsNullOrWhiteSpace(s) == string.IsNullOrWhiteSpace(password.Text));
        CheckField(password, s => string.IsNullOrWhiteSpace(s) == string.IsNullOrWhiteSpace(username.Text));
        CreateCheckBox("Uot", "UDP over TCP", s => server.ProtoExtra.Uot = s, server.ProtoExtra.Uot == true);

    }

    protected override EConfigType TypeName { get; } = EConfigType.SOCKS;
}

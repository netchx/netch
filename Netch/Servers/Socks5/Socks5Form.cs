using Netch.Forms;
using Netch.Utils;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class Socks5Form : ServerForm
{
    public Socks5Form(Socks5Server? server = default)
    {
        server ??= new Socks5Server();
        Server = server;
        CreateTextBox("Username", "Username", s => true, s => server.Username = s, server.Username.ValueOrDefault());
        CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password.ValueOrDefault());
        CreateComboBox("Version",
            "Version",
            SOCKSGlobal.Versions,
            s => server.Version = s,
            server.Version);
        (_remoteHostnameLabel, _remoteHostnameTextBox) = CreateTextBox("RemoteHostname",
            "Remote Address",
            s => true,
            s => server.RemoteHostname = s,
            server.RemoteHostname.ValueOrDefault());

        AddressTextBox.TextChanged += AddressTextBoxOnTextChanged;
        AddressTextBoxOnTextChanged(null!, null!);
    }

    private readonly Label _remoteHostnameLabel;
    private readonly TextBox _remoteHostnameTextBox;

    private void AddressTextBoxOnTextChanged(object? sender, EventArgs e)
    {
        _remoteHostnameLabel.Visible = _remoteHostnameTextBox.Visible = IsPrivateAddress(AddressTextBox.Text);
    }

    private bool IsPrivateAddress(string address)
    {
        // https://en.wikipedia.org/wiki/Private_network#Private_IPv4_addresses
        return address.StartsWith("10.") || address.StartsWith("192.168.") || address.StartsWith("172.") || address.StartsWith("127.");
    }

    protected override string TypeName { get; } = "Socks5";
}
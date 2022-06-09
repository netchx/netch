using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class WireGuardForm : ServerForm
{
    public WireGuardForm(WireGuardServer? server = default)
    {
        server ??= new WireGuardServer();
        Server = server;
        CreateTextBox("LocalAddresses", "Local Addresses", s => true, s => server.LocalAddresses = s, server.LocalAddresses);
        CreateTextBox("PeerPublicKey", "Public Key", s => true, s => server.PeerPublicKey = s, server.PeerPublicKey);
        CreateTextBox("PrivateKey", "Private Key", s => true, s => server.PrivateKey = s, server.PrivateKey);
        CreateTextBox("PreSharedKey", "PSK", s => true, s => server.PreSharedKey = s, server.PreSharedKey);
        CreateTextBox("MTU", "MTU", s => int.TryParse(s, out _), s => server.MTU = int.Parse(s), server.MTU.ToString(), 76);
    }

    protected override string TypeName { get; } = "WireGuard";
}
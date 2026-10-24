using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class WireGuardForm : ServerForm
{
    public WireGuardForm(WireGuardServer? server = default)
    {
        server ??= new WireGuardServer();
        Server = server;
        CreateTextBox("PrivateKey", "Private Key", s => true, s => server.Password = s, server.Password);
        CreateTextBox("PublicKey", "Public Key", s => true, s => server.ProtoExtra.WgPublicKey = s, server.ProtoExtra.WgPublicKey);
        CreateTextBox("Reserved", "Reserved(2,3,4)", s => true, s => server.ProtoExtra.WgReserved = s, server.ProtoExtra.WgReserved);
        CreateTextBox("LocalAddresses", "Addresses(Ipv4,Ipv6)", s => true, s => server.ProtoExtra.WgInterfaceAddress = s, server.ProtoExtra.WgInterfaceAddress);
        CreateTextBox("MTU", "MTU", s => int.TryParse(s, out _), s => server.ProtoExtra.WgMtu = int.Parse(s), server.ProtoExtra.WgMtu.ToString(), 76);
    }

    protected override EConfigType TypeName { get; } = EConfigType.WireGuard;
}
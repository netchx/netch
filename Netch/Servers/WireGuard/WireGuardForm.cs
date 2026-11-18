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
        var extra = server.ProtoExtra;
        CreateTextBox("PrivateKey", "Private Key", IsKey, s => server.Password = s.Trim(), server.Password);
        CreateTextBox("PublicKey", "Peer Public Key", IsKey, s => extra.WgPublicKey = s.Trim(),
            string.IsNullOrWhiteSpace(extra.WgPublicKey) ? server.PublicKey : extra.WgPublicKey);
        CreateTextBox("PresharedKey", "Preshared Key", s => string.IsNullOrWhiteSpace(s) || IsKey(s), s => extra.WgPresharedKey = s.Trim(), extra.WgPresharedKey);
        CreateTextBox("Reserved", "Reserved Bytes", s => string.IsNullOrWhiteSpace(s) ||
            (s.Split(',') is { Length: 3 } values && values.All(value => byte.TryParse(value, out _))),
            s => extra.WgReserved = s.Trim(), extra.WgReserved);
        CreateTextBox("LocalAddresses", "Local IP / CIDR", IsAddressList,
            s => extra.WgInterfaceAddress = string.Join(",", s.Split(',').Select(value => value.Trim())), extra.WgInterfaceAddress);
        CreateTextBox("MTU", "MTU", s => string.IsNullOrWhiteSpace(s) || int.TryParse(s, out var mtu) && mtu > 0,
            s => extra.WgMtu = int.TryParse(s, out var mtu) ? mtu : null, extra.WgMtu is > 0 ? extra.WgMtu.ToString() : "");
    }

    protected override EConfigType TypeName { get; } = EConfigType.WireGuard;

    private static bool IsKey(string value) =>
        Convert.TryFromBase64String(value, stackalloc byte[32], out var length) && length == 32;

    private static bool IsAddressList(string value) => value.Split(',').All(address =>
        System.Net.IPAddress.TryParse(address.Trim(), out _) ||
        Utils.RouteUtils.TryParseIPNetwork(address.Trim(), out _, out _));
}

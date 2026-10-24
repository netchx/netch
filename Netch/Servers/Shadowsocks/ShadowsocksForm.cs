using Netch.Enums;
using Netch.Forms;
using Netch.Utils;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class ShadowsocksForm : ServerForm
{
    public ShadowsocksForm(ShadowsocksServer? server = default)
    {
        server ??= new ShadowsocksServer();
        Server = server;
        CreateTextBox("Password", "Password", s => !s.IsNullOrWhiteSpace(), s => server.Password = s, server.Password);
        CreateComboBox("EncryptMethod", "Encrypt Method", Constants.SsSecuritiesInXray, s => server.ProtoExtra.SsMethod = s, server.ProtoExtra.SsMethod);
        CreateCheckBox("UseMux", "Use Mux", s => server.MuxEnabled = s, server.MuxEnabled ?? false);
    }

    protected override EConfigType TypeName { get; } = EConfigType.Shadowsocks;
}
using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class VMessForm : ServerForm
{
    public VMessForm(VMessServer? server = default)
    {
        server ??= new VMessServer { MuxEnabled = false };
        Server = server;

        CreateTextBox("UserId", "UUID", s => !string.IsNullOrWhiteSpace(s), s => server.Password = s.Trim(), server.Password);
        CreateTextBox("AlterId", "Alter ID", s => ushort.TryParse(s, out _),
            s => server.ProtoExtra.AlterId = ushort.Parse(s), (server.ProtoExtra.AlterId ?? 0).ToString());
        CreateComboBox("Encryption", "Encryption", Constants.VmessSecurities, s => server.ProtoExtra.VmessSecurity = s,
            server.ProtoExtra.VmessSecurity);
        if (server.MuxEnabled ?? Global.Settings.V2RayConfig.CoreBasicItem.MuxEnabled)
            CreateCheckBox("UseMux", "Use Mux", s => server.MuxEnabled = s, true);
        AddTransport();
    }

    protected override EConfigType TypeName { get; } = EConfigType.VMess;
}

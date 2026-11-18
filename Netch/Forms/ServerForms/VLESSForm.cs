using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
internal class VLESSForm : ServerForm
{
    public VLESSForm(VLESSServer? server = default)
    {
        server ??= new VLESSServer { MuxEnabled = false };
        Server = server;

        CreateTextBox("UUID", "UUID", s => !string.IsNullOrWhiteSpace(s), s => server.Password = s.Trim(), server.Password);
        var flow = CreateComboBox("Flow", "Flow", ["", "xtls-rprx-vision"], s => server.ProtoExtra.Flow = s, server.ProtoExtra.Flow);
        CreateTextBox("Encryption", "Encryption", s => s == server.ProtoExtra.VlessEncryption ||
            string.IsNullOrWhiteSpace(s) || s.Trim() == "none" ||
            s.Trim().Split('.') is [ "mlkem768x25519plus", _, _, _, .. ],
            s => server.ProtoExtra.VlessEncryption = string.IsNullOrWhiteSpace(s) ? "none" : s.Trim(),
            string.IsNullOrEmpty(server.ProtoExtra.VlessEncryption) ? "none" : server.ProtoExtra.VlessEncryption);
        if (server.MuxEnabled ?? Global.Settings.V2RayConfig.CoreBasicItem.MuxEnabled)
            CreateCheckBox("UseMux", "Use Mux", s => server.MuxEnabled = s, true);
        AddTransport(flow);
    }

    protected override EConfigType TypeName { get; } = EConfigType.VLESS;
}

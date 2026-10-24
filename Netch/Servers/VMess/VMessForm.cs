using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class VMessForm : ServerForm
{
    public VMessForm(VMessServer? server = default)
    {
        server ??= new VMessServer();
        Server = server;

        CreateTextBox("UserId", "User ID", s => true, s => server.Password = s, server.Password);
        CreateTextBox("AlterId", "Alter ID", s => int.TryParse(s, out _), s => server.ProtoExtra.AlterId = int.Parse(s), server.ProtoExtra.AlterId.ToString(), 76);
        CreateTextBox("Encryption", "Encryption", s=>true, s => server.ProtoExtra.VlessEncryption = s, server.ProtoExtra.VlessEncryption);
        CreateComboBox("UseMux",
            "Use Mux",
            Constants.AllowInsecure,
            s => server.MuxEnabled = s switch { "" => null, "true" => true, "false" => false, _ => null },
            server.MuxEnabled?.ToString().ToLower() ?? "");

        CreateComboBox("Network", "Network", Constants.Networks, s => server.Network = s, server.Network);
        CreateComboBox("FakeType", "Fake Type", Constants.AllHeaderTypes, s => server.HeaderType = s, server.HeaderType);
        CreateTextBox("Host", "Host", s => true, s => server.RequestHost = s, server.RequestHost);
        CreateTextBox("Path", "Path", s => true, s => server.Path = s, server.Path);
    }

    protected override EConfigType TypeName { get; } = EConfigType.VMess;
}
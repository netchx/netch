using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class TrojanForm : ServerForm
{
    public TrojanForm(TrojanServer? server = default)
    {
        server ??= new TrojanServer();
        Server = server;
        CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password);
        CreateComboBox("Flow", "Flow", Constants.Flow, s => server.ProtoExtra.Flow = s, server.ProtoExtra.Flow);
        CreateCheckBox("UseMux", "Use Mux", s => server.MuxEnabled = s, server.MuxEnabled ?? false);

        //底层传输方式
        CreateComboBox("Network", "Network", Constants.Networks, s => server.Network = s, server.Network);
        CreateComboBox("FakeType", "Fake Type", Constants.AllHeaderTypes, s => server.HeaderType = s, server.HeaderType);
        CreateTextBox("Host", "Host", s => true, s => server.RequestHost = s, server.RequestHost);
        CreateTextBox("Path", "Path", s => true, s => server.Path = s, server.Path);
    }

    protected override EConfigType TypeName { get; } = EConfigType.Trojan;
}
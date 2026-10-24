using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
internal class VLESSForm : ServerForm
{
    public VLESSForm(VLESSServer? server = default)
    {
        server ??= new VLESSServer();
        Server = server;

        CreateTextBox("UUID", "UUID", s => true, s => server.Password = s, server.Password);
        CreateComboBox("Flow", "Flow", Constants.Flow, s => server.ProtoExtra.Flow = s, server.ProtoExtra.Flow);
        CreateComboBox("EncryptMethod", "Encrypt Method", Constants.VmessSecurities, s => server.ProtoExtra.VmessSecurity = s, server.ProtoExtra.VmessSecurity);
        CreateCheckBox("UseMux", "Use Mux", s => server.MuxEnabled = s, server.MuxEnabled ?? false);

        //�ײ㴫�䷽ʽ
        CreateComboBox("Network", "Network", Constants.Networks, s => server.Network = s, server.Network);
        CreateComboBox("FakeType", "Fake Type", Constants.AllHeaderTypes, s => server.HeaderType = s, server.HeaderType);
        CreateTextBox("Host", "Host", s => true, s => server.RequestHost = s, server.RequestHost);
        CreateTextBox("Path", "Path", s => true, s => server.Path = s, server.Path);        
    }

    protected override EConfigType TypeName { get; } = EConfigType.VLESS;
}
using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class TrojanForm : ServerForm
{
    public TrojanForm(TrojanServer? server = default)
    {
        server ??= new TrojanServer { StreamSecurity = Constants.StreamSecurity };
        Server = server;
        CreateTextBox("Password", "Password", s => !string.IsNullOrEmpty(s), s => server.Password = s, server.Password);
        if (server.ProtoExtra.Flow is { Length: > 0 } flow && flow != "none")
            CreateComboBox("LegacyFlow", "Legacy Flow", [""], s => server.ProtoExtra.Flow = s, flow);
        AddTransport();
    }

    protected override EConfigType TypeName { get; } = EConfigType.Trojan;
}

using Netch.Forms;
using Netch.Utils;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class ShadowsocksRForm : ServerForm
{
    public ShadowsocksRForm(ShadowsocksRServer? server = default)
    {
        server ??= new ShadowsocksRServer();
        Server = server;
        CreateTextBox("Password", "Password", s => !s.IsNullOrWhiteSpace(), s => server.Password = s, server.Password);
        CreateComboBox("EncryptMethod", "Encrypt Method", SSRGlobal.EncryptMethods, s => server.EncryptMethod = s, server.EncryptMethod);
        CreateComboBox("Protocol", "Protocol", SSRGlobal.Protocols, s => server.Protocol = s, server.Protocol);
        CreateTextBox("ProtocolParam", "Protocol Param", s => true, s => server.ProtocolParam = s, server.ProtocolParam);
        CreateComboBox("OBFS", "OBFS", SSRGlobal.OBFSs, s => server.OBFS = s, server.OBFS);
        CreateTextBox("OBFSParam", "OBFS Param", s => true, s => server.OBFSParam = s, server.OBFSParam);
    }

    protected override string TypeName { get; } = "ShadowsocksR";
}
using Netch.Forms;

namespace Netch.Servers.ShadowsocksR.Form
{
    public class ShadowsocksRForm : ServerForm
    {
        public ShadowsocksRForm(ShadowsocksR? server = default)
        {
            server ??= new ShadowsocksR();
            Server = server;
            CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password);
            CreateComboBox("EncryptMethod", "Encrypt Method", SSRGlobal.EncryptMethods, s => server.EncryptMethod = s, server.EncryptMethod);
            CreateComboBox("Protocol", "Protocol", SSRGlobal.Protocols, s => server.Protocol = s, server.Protocol);
            CreateTextBox("ProtocolParam", "Protocol Param", s => true, s => server.ProtocolParam = s, server.ProtocolParam);
            CreateComboBox("OBFS", "OBFS", SSRGlobal.OBFSs, s => server.OBFS = s, server.OBFS);
            CreateTextBox("OBFSParam", "OBFS Param", s => true, s => server.OBFSParam = s, server.OBFSParam);
        }

        protected override string TypeName { get; } = "ShadowsocksR";
    }
}
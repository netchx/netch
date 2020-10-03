using Netch.Forms;

namespace Netch.Servers.ShadowsocksR.Form
{
    public class ShadowsocksRForm : ServerForm
    {
        protected override string TypeName { get; } = "ShadowsocksR";

        public ShadowsocksRForm(ShadowsocksR server = default)
        {
            server ??= new ShadowsocksR();
            Server = server;
            CreateTextBox("Password", "Password",
                s => true,
                s => server.Password = (string) s,
                server.Password);
            CreateComboBox("EncryptMethod", "Encrypt Method",
                SSRGlobal.EncryptMethods,
                s => SSRGlobal.EncryptMethods.Contains(s),
                s => server.EncryptMethod = (string) s,
                server.EncryptMethod);
            CreateComboBox("Protocol", "Protocol",
                SSRGlobal.Protocols,
                s => SSRGlobal.Protocols.Contains(s),
                s => server.Protocol = (string) s,
                server.Protocol);
            CreateTextBox("ProtocolParam", "Protocol Param",
                s => true,
                s => server.ProtocolParam = (string) s,
                server.ProtocolParam);
            CreateComboBox("OBFS", "OBFS",
                SSRGlobal.OBFSs,
                s => SSRGlobal.OBFSs.Contains(s),
                s => server.OBFS = (string) s,
                server.OBFS);
            CreateTextBox("OBFSParam", "OBFS Param",
                s => true,
                s => server.OBFSParam = (string) s,
                server.OBFSParam);
        }
    }
}
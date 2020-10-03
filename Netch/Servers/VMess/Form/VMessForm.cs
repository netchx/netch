using Netch.Forms;

namespace Netch.Servers.VMess.Form
{
    public class VMessForm : ServerForm
    {
        protected override string TypeName { get; } = "VMess";

        public VMessForm(VMess server = default)
        {
            server ??= new VMess();
            Server = server;
            CreateTextBox("UserId", "User ID",
                s => true,
                s => server.UserID = (string) s,
                server.UserID);
            CreateTextBox("AlterId", "Alter ID",
                s => int.TryParse(s, out _),
                s => server.AlterID = int.Parse((string) s),
                server.AlterID.ToString(),
                76);
            CreateComboBox("EncryptMethod", "Encrypt Method",
                VMessGlobal.EncryptMethods,
                s => VMessGlobal.EncryptMethods.Contains(s),
                s => server.EncryptMethod = (string) s,
                server.EncryptMethod);
            CreateComboBox("TransferProtocol", "Transfer Protocol",
                VMessGlobal.TransferProtocols,
                s => VMessGlobal.TransferProtocols.Contains(s),
                s => server.TransferProtocol = (string) s,
                server.TransferProtocol);
            CreateComboBox("FakeType", "Fake Type",
                VMessGlobal.FakeTypes,
                s => VMessGlobal.FakeTypes.Contains(s),
                s => server.FakeType = (string) s,
                server.FakeType);
            CreateTextBox("Host", "Host",
                s => true,
                s => server.Host = (string) s,
                server.Host);
            CreateTextBox("Path", "Path",
                s => true,
                s => server.Path = (string) s,
                server.Path);
            CreateComboBox("QUICSecurity", "QUIC Security",
                VMessGlobal.QUIC,
                s => VMessGlobal.QUIC.Contains(s),
                s => server.QUIC = (string) s,
                server.QUIC);
            CreateTextBox("QUICSecret", "QUIC Secret",
                s => true,
                s => server.QUICSecret = (string) s,
                server.QUICSecret);
            CreateCheckBox("UseMux", "Use Mux",
                s => server.UseMux = (bool) s,
                server.UseMux);
            CreateCheckBox("TLSSecure", "TLS Secure",
                s => server.TLSSecure = (bool) s,
                server.TLSSecure);
        }
    }
}
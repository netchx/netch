using System.Collections.Generic;
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
                s => server.UserID = s,
                server.UserID);
            CreateTextBox("AlterId", "Alter ID",
                s => int.TryParse(s, out _),
                s => server.AlterID = int.Parse(s),
                server.AlterID.ToString(),
                76);
            CreateComboBox("EncryptMethod", "Encrypt Method",
                VMessGlobal.EncryptMethods,
                s => server.EncryptMethod = s,
                server.EncryptMethod);
            CreateComboBox("TransferProtocol", "Transfer Protocol",
                VMessGlobal.TransferProtocols,
                s => server.TransferProtocol = s,
                server.TransferProtocol);
            CreateComboBox("FakeType", "Fake Type",
                VMessGlobal.FakeTypes,
                s => server.FakeType = s,
                server.FakeType);
            CreateTextBox("Host", "Host",
                s => true,
                s => server.Host = s,
                server.Host);
            CreateTextBox("Path", "Path",
                s => true,
                s => server.Path = s,
                server.Path);
            CreateComboBox("QUICSecurity", "QUIC Security",
                VMessGlobal.QUIC,
                s => server.QUIC = s,
                server.QUIC);
            CreateTextBox("QUICSecret", "QUIC Secret",
                s => true,
                s => server.QUICSecret = s,
                server.QUICSecret);
            CreateComboBox("UseMux", "Use Mux",
                new List<string> {"", "true", "false"},
                s => server.UseMux = s switch
                {
                    "" => null,
                    "true" => true,
                    "false" => false,
                    _ => null
                },
                server.UseMux?.ToString().ToLower() ?? "");
            CreateComboBox("TLSSecure", "TLS Secure",
                VMessGlobal.TLSSecure,
                s => server.TLSSecureType = s,
                server.TLSSecureType);
        }
    }
}
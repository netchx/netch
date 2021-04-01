using Netch.Forms;
using System.Collections.Generic;

namespace Netch.Servers.VLESS.VLESSForm
{
    internal class VLESSForm : ServerForm
    {
        public VLESSForm(VLESS? server = default)
        {
            server ??= new VLESS();
            Server = server;
            CreateTextBox("UUID", "UUID", s => true, s => server.UserID = s, server.UserID);
            CreateTextBox("EncryptMethod",
                "Encrypt Method",
                s => true,
                s => server.EncryptMethod = !string.IsNullOrWhiteSpace(s) ? s : "none",
                server.EncryptMethod);

            CreateTextBox("Flow", "Flow", s => true, s => server.Flow = s, server.Flow);
            CreateComboBox("TransferProtocol",
                "Transfer Protocol",
                VLESSGlobal.TransferProtocols,
                s => server.TransferProtocol = s,
                server.TransferProtocol);

            CreateComboBox("FakeType", "Fake Type", VLESSGlobal.FakeTypes, s => server.FakeType = s, server.FakeType);
            CreateTextBox("Host", "Host", s => true, s => server.Host = s, server.Host);
            CreateTextBox("Path", "Path", s => true, s => server.Path = s, server.Path);
            CreateComboBox("QUICSecurity", "QUIC Security", VLESSGlobal.QUIC, s => server.QUICSecure = s, server.QUICSecure);
            CreateTextBox("QUICSecret", "QUIC Secret", s => true, s => server.QUICSecret = s, server.QUICSecret);
            CreateComboBox("UseMux",
                "Use Mux",
                new List<string> { "", "true", "false" },
                s => server.UseMux = s switch { "" => null, "true" => true, "false" => false, _ => null },
                server.UseMux?.ToString().ToLower() ?? "");

            CreateComboBox("TLSSecure", "TLS Secure", VLESSGlobal.TLSSecure, s => server.TLSSecureType = s, server.TLSSecureType);
        }

        protected override string TypeName { get; } = "VLESS";
    }
}
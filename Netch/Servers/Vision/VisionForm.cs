using Netch.Forms;
namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
internal class VisionForm : ServerForm
{
    public VisionForm(VisionServer? server = default)
    {
        server ??= new VisionServer();
        Server = server;
        CreateTextBox("Sni", "ServerName(Sni)", s => true, s => server.ServerName = s, server.ServerName);
        CreateTextBox("UUID", "UUID", s => true, s => server.UserID = s, server.UserID);
        CreateComboBox("Flow", "Flow", VisionGlobal.Flows.ToList(), s => server.Flow = s, server.Flow);

        CreateTextBox("EncryptMethod",
            "Encrypt Method",
            s => true,
            s => server.EncryptMethod = !string.IsNullOrWhiteSpace(s) ? s : "none",
            server.EncryptMethod);

        CreateComboBox("TransferProtocol",
            "Transfer Protocol",
            VisionGlobal.TransferProtocols.ToList(), // ½«Collection<string>×ª»»ÎªList<string>
            s => server.TransferProtocol = s,
            server.TransferProtocol);
/*
        CreateComboBox("PacketEncoding",
            "Packet Encoding",
            VMessGlobal.PacketEncodings.ToList(),
            s => server.PacketEncoding = s,
            server.PacketEncoding); */

        CreateComboBox("TLSSecure", "TLS Secure", VisionGlobal.TLSSecure.ToList(), s => server.TLSSecureType = s, server.TLSSecureType);
        CreateComboBox("Fingerprint", "Fingerprint", VisionGlobal.Fingerprints.ToList(), s => server.Fingerprint = s, server.Fingerprint);

        CreateComboBox("Alpn", "Alpn", VisionGlobal.Alpns.ToList(), s => server.Alpn = s, server.Alpn);

        CreateTextBox("PublicKey", "PublicKey(reality)", s => true, s => server.PublicKey = s, server.PublicKey);
        CreateTextBox("SpiderX", "SpiderX(reality)", s => true, s => server.SpiderX = s, server.SpiderX);
        CreateTextBox("ShortId", "ShortId(reality)", s => true, s => server.ShortId = s, server.ShortId);

        //        CreateComboBox("FakeType", "Fake Type", VisionGlobal.FakeTypes.ToList(), s => server.FakeType = s, server.FakeType);
        //        CreateTextBox("Host", "Host", s => true, s => server.Host = s, server.Host);
        //        CreateTextBox("Path", "Path", s => true, s => server.Path = s, server.Path);
        //        CreateComboBox("QUICSecurity", "QUIC Security", VisionGlobal.QUIC.ToList(), s => server.QUICSecure = s, server.QUICSecure);
        //        CreateTextBox("QUICSecret", "QUIC Secret", s => true, s => server.QUICSecret = s, server.QUICSecret);
/*
        CreateComboBox("UseMux",
            "Use Mux",
            new List<string> { "", "true", "false" },
            s => server.UseMux = s switch { "" => null, "true" => true, "false" => false, _ => null },
            server.UseMux?.ToString().ToLower() ?? "");

        CreateComboBox("TrafficSniffing",
            "Traffic Sniffing",
            new List<string> { "true", "false" },
            s => server.Sniffing = s == "true",
            server.Sniffing.ToString().ToLower() ?? "false");

        CreateComboBox("AllowHttpProtocol",
            "Allow http protocol",
            new List<string> { "true", "false" },
            s => server.AllowHttp = s == "true",
            server.AllowHttp.ToString().ToLower() ?? "false");  */
    }

    protected override string TypeName { get; } = "Vision";
}
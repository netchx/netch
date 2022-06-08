using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
internal class VLiteForm : ServerForm
{
    public VLiteForm(VLiteServer? server = default)
    {
        server ??= new VLiteServer();
        Server = server;
        CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password);
        CreateComboBox("ScramblePacket", "Scramble Packet", VLiteGlobal.ScramblePacket, s => server.ScramblePacket = s, server.ScramblePacket);
        CreateComboBox("EnableFec", "Fec", VLiteGlobal.EnableFec, s => server.EnableFec = s, server.EnableFec);
        CreateComboBox("EnableStabilization", "Stabilization", VLiteGlobal.EnableStabilization, s => server.EnableStabilization = s, server.EnableStabilization);
        CreateComboBox("EnableRenegotiation", "Renegotiation", VLiteGlobal.EnableRenegotiation, s => server.EnableRenegotiation = s, server.EnableRenegotiation);
        CreateTextBox("HandshakeMaskingPaddingSize", "Handshake Mask-\ning Padding Size", s => int.TryParse(s, out _), s => server.HandshakeMaskingPaddingSize = int.Parse(s), server.HandshakeMaskingPaddingSize.ToString(), 76);
    }

    protected override string TypeName { get; } = "VLite";
}
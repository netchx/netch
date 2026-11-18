using Netch.Enums;
using Netch.Forms;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class Hysteria2Form : ServerForm
{
    public Hysteria2Form(Hysteria2Server? server = default)
    {
        server ??= new Hysteria2Server();
        Server = server;

        var extra = server.ProtoExtra;
        CreateTextBox("Auth", "Auth", _ => true, s => server.Password = s, server.Password);
        var obfs = CreateComboBox("Obfs", "Obfs", ["none", "salamander", "gecko"], s =>
        {
            extra.Obfs = s;
            if (s == "none") extra.SalamanderPass = "";
        }, string.IsNullOrEmpty(extra.SalamanderPass) ? "none" : extra.Obfs);
        var obfsPassword = CreateTextBox("ObfsPassword", "Obfs Password", s => !string.IsNullOrEmpty(s),
            s => extra.SalamanderPass = s, extra.SalamanderPass).Field;
        var ports = CreateTextBox("PortHoppingRange", "Port Hopping Range", ValidPortRange,
            s => extra.Ports = s.Trim(), extra.Ports).Field;
        var interval = CreateTextBox("HopInterval", "Hop Interval (s)", s => string.IsNullOrWhiteSpace(s) ||
            s == extra.HopInterval || uint.TryParse(s.Trim().TrimEnd('s'), out var result) && result >= 5,
            s => extra.HopInterval = s.Trim(), extra.HopInterval).Field;
        CreateTextBox("UpMbps", "Upload (Mbps)", NonnegativeOrEmpty, s => extra.UpMbps = ParseOptional(s), extra.UpMbps?.ToString());
        CreateTextBox("DownMbps", "Download (Mbps)", NonnegativeOrEmpty, s => extra.DownMbps = ParseOptional(s), extra.DownMbps?.ToString());
        if (!string.IsNullOrWhiteSpace(server.CertSha))
            AddNotice("Replace the legacy certificate pin with a trusted PEM certificate.");
        obfs.SelectedIndexChanged += (_, _) => UpdateFields(() => ShowField(obfsPassword, obfs.Text != "none"));
        ports.TextChanged += (_, _) => UpdateFields(() => ShowField(interval, !string.IsNullOrWhiteSpace(ports.Text)));
        ShowField(obfsPassword, obfs.Text != "none");
        ShowField(interval, !string.IsNullOrWhiteSpace(ports.Text));
    }

    protected override EConfigType TypeName { get; } = EConfigType.Hysteria2;

    private static int? ParseOptional(string text) => int.TryParse(text, out var value) ? value : null;

    private static bool NonnegativeOrEmpty(string text) => string.IsNullOrWhiteSpace(text) || ParseOptional(text) is >= 0;

    private static bool ValidPortRange(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        return text.Split(',').All(entry =>
        {
            var range = entry.Replace(':', '-').Split('-');
            return ushort.TryParse(range[0], out var start) && start > 0 &&
                (range.Length == 1 || range.Length == 2 && ushort.TryParse(range[1], out var end) && end >= start);
        });
    }
}

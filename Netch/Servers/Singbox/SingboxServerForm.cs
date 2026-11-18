using Netch.Enums;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public sealed class SingboxServerForm : ServerForm
{
    private readonly EConfigType _typeName;

    public SingboxServerForm(Server? server, EConfigType typeName)
    {
        server ??= typeName switch
        {
            EConfigType.TUIC => new TUICServer(),
            EConfigType.AnyTLS => new AnyTLSServer(),
            EConfigType.Naive => new NaiveServer(),
            EConfigType.MASQUE => new MASQUEServer(),
            _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, null)
        };
        Server = server;
        _typeName = typeName;

        switch (typeName)
        {
            case EConfigType.TUIC:
                AddNotice("TUIC v5");
                CreateTextBox("Username", "UUID", s => Guid.TryParse(s, out var id) && id != Guid.Empty,
                    s => server.Username = Guid.Parse(s).ToString(), server.Username);
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                CreateComboBox("CongestionControl", "Congestion", ["bbr", "cubic", "new_reno"],
                    s => server.ProtoExtra.CongestionControl = s, server.ProtoExtra.CongestionControl);
                break;
            case EConfigType.AnyTLS:
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                break;
            case EConfigType.Naive:
                CreateTextBox("Username", "Username", s => s.Length > 0, s => server.Username = s, server.Username);
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                CreateCheckBox("NaiveQuic", "QUIC", s => server.ProtoExtra.NaiveQuic = s, server.ProtoExtra.NaiveQuic == true);
                CreateTextBox("InsecureConcurrency", "Tunnel Connections", s => string.IsNullOrWhiteSpace(s) || int.TryParse(s, out var n) && n >= 0,
                    s => server.ProtoExtra.InsecureConcurrency = int.TryParse(s, out var n) ? n : null,
                    server.ProtoExtra.InsecureConcurrency?.ToString() ?? string.Empty);
                break;
            case EConfigType.MASQUE:
                AddNotice("MASQUE requires sing-box with CONNECT-IP support. The bundled 1.14.4 core does not support it.");
                CreateTextBox("Username", "Username", _ => true, s => server.Username = s, server.Username);
                CreateTextBox("Password", "Password", _ => true, s => server.Password = s, server.Password);
                CreateTextBox("MasquePath", "URI Template", s => string.IsNullOrWhiteSpace(s) || s.Trim().StartsWith('/'),
                    s => server.ProtoExtra.MasquePath = s.Trim(), server.ProtoExtra.MasquePath);
                CreateHeaders("MasqueHeaders", s => server.ProtoExtra.MasqueHeaders = s, server.ProtoExtra.MasqueHeaders);
                break;
        }
    }

    protected override EConfigType TypeName => _typeName;
}

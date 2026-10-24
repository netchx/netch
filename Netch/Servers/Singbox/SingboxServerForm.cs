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
                CreateTextBox("Username", "UUID", s => s.Length > 0, s => server.Username = s, server.Username);
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                CreateTextBox("CongestionControl", "Congestion", s => true, s => server.ProtoExtra.CongestionControl = s, server.ProtoExtra.CongestionControl ?? "bbr");
                break;
            case EConfigType.AnyTLS:
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                break;
            case EConfigType.Naive:
                CreateTextBox("Username", "Username", s => s.Length > 0, s => server.Username = s, server.Username);
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                CreateCheckBox("NaiveQuic", "QUIC", s => server.ProtoExtra.NaiveQuic = s, server.ProtoExtra.NaiveQuic == true);
                CreateTextBox("InsecureConcurrency", "Concurrency", s => string.IsNullOrEmpty(s) || int.TryParse(s, out var n) && n > 0,
                    s => server.ProtoExtra.InsecureConcurrency = int.TryParse(s, out var n) ? n : null,
                    server.ProtoExtra.InsecureConcurrency?.ToString() ?? string.Empty);
                break;
            case EConfigType.MASQUE:
                CreateTextBox("Username", "Username", s => true, s => server.Username = s, server.Username);
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                CreateTextBox("MasquePath", "Path", s => true, s => server.ProtoExtra.MasquePath = s, server.ProtoExtra.MasquePath ?? "/");
                CreateTextBox("MasqueHeaders", "Headers", s => true, s => server.ProtoExtra.MasqueHeaders = s, server.ProtoExtra.MasqueHeaders);
                break;
        }
    }

    protected override EConfigType TypeName => _typeName;
}

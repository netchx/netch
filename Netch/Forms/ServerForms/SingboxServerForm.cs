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
                CreateComboBox("UdpRelayMode", "UDP Relay Mode", Constants.TuicUdpRelayModes,
                    s => server.ProtoExtra.TuicUdpRelayMode = s, server.ProtoExtra.TuicUdpRelayMode);
                CreateCheckBox("ReduceRtt", "Reduce RTT", b => server.ProtoExtra.TuicReduceRtt = b, server.ProtoExtra.TuicReduceRtt == true);
                CreateCheckBox("DisableSni", "Disable SNI", b => server.ProtoExtra.TuicDisableSni = b, server.ProtoExtra.TuicDisableSni == true);
                break;
            case EConfigType.AnyTLS:
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                CreateTextBox("IdleSessionCheckInterval", "Idle Check Interval (s)", NonnegativeOrEmpty,
                    s => server.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds = ParseOptional(s),
                    server.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds?.ToString());
                CreateTextBox("IdleSessionTimeout", "Idle Session Timeout (s)", NonnegativeOrEmpty,
                    s => server.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds = ParseOptional(s),
                    server.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds?.ToString());
                CreateTextBox("MinIdleSession", "Min Idle Sessions", NonnegativeOrEmpty,
                    s => server.ProtoExtra.AnyTlsMinIdleSession = ParseOptional(s),
                    server.ProtoExtra.AnyTlsMinIdleSession?.ToString());
                break;
            case EConfigType.Naive:
                CreateTextBox("Username", "Username", s => s.Length > 0, s => server.Username = s, server.Username);
                CreateTextBox("Password", "Password", s => s.Length > 0, s => server.Password = s, server.Password);
                var quic = CreateCheckBox("NaiveQuic", "QUIC", s =>
                {
                    server.ProtoExtra.NaiveQuic = s;
                    if (!s) server.ProtoExtra.CongestionControl = "";
                }, server.ProtoExtra.NaiveQuic == true);
                var congestion = CreateComboBox("CongestionControl", "QUIC Congestion", Constants.NaiveQuicCongestionControls,
                    s => server.ProtoExtra.CongestionControl = s, server.ProtoExtra.CongestionControl);
                CreateTextBox("InsecureConcurrency", "Tunnel Connections", s => string.IsNullOrWhiteSpace(s) || int.TryParse(s, out var n) && n >= 0,
                    s => server.ProtoExtra.InsecureConcurrency = int.TryParse(s, out var n) ? n : null,
                    server.ProtoExtra.InsecureConcurrency?.ToString() ?? string.Empty);
                CreateCheckBox("Uot", "UDP over TCP", b => server.ProtoExtra.Uot = b, server.ProtoExtra.Uot == true);
                quic.CheckedChanged += (_, _) => UpdateFields(() => ShowField(congestion, quic.Checked));
                ShowField(congestion, quic.Checked);
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

    private static int? ParseOptional(string text) => int.TryParse(text, out var value) ? value : null;

    private static bool NonnegativeOrEmpty(string text) => string.IsNullOrWhiteSpace(text) || ParseOptional(text) is >= 0;
}

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Netch.Enums;
using Netch.Manager;
using Netch.Models;
using Netch.Properties;
using Netch.Servers;
using Netch.Utils;

namespace Netch.Forms;

[Fody.ConfigureAwait(true)]
public abstract class ServerForm : Form
{
    private readonly Panel _body = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly TableLayoutPanel _fields = new()
    {
        AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
        ColumnCount = 2, Padding = new Padding(12, 8, 12, 8)
    };
    private readonly Dictionary<Control, Label?> _labels = new();
    private readonly Dictionary<Control, Func<string, bool>> _checks = new();
    private readonly Dictionary<Control, Action> _saves = new();
    private readonly HashSet<Control> _hidden = [];
    private readonly TextBox _remark;
    private readonly TextBox _address;
    private readonly TextBox _port;
    private ComboBox? _network;
    private Action? _refreshTransportSecurity;
    private bool _prepared;
    private bool _updatingFields;

    protected ServerForm()
    {
        SuspendLayout();
        _body.SuspendLayout();
        _fields.SuspendLayout();
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(495, 410);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = Resources.icon;
        MaximizeBox = false;
        MinimizeBox = false;
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _body.Controls.Add(_fields);
        Controls.Add(_body);
        _remark = CreateTextBox("Remark", "Remark", _ => true, s => Server.Remarks = s.Trim(), "").Field;
        _address = CreateTextBox("Address", "Address", s => !string.IsNullOrWhiteSpace(s), s => Server.Address = s.Trim(), "").Field;
        _port = CreateTextBox("Port", "Port", s => ushort.TryParse(s, out var port) && port > 0,
            s => Server.Port = ushort.Parse(s), "").Field;
    }

    protected abstract EConfigType TypeName { get; }
    protected Server Server { get; set; }

    internal void ShowEditor()
    {
        using (this)
            ShowDialog();
    }

    protected override void OnLoad(EventArgs e)
    {
        Prepare();
        base.OnLoad(e);
    }

    private void Prepare()
    {
        if (_prepared) return;
        Text = ServerHelper.GetDisplayName(TypeName);
        _remark.Text = Server.Remarks;
        _address.Text = Server.Address;
        _port.Text = Server.Port > 0 ? Server.Port.ToString() : "";
        AddSecurityFields();

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true, Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8), WrapContents = false
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var save = new Button { Text = "Save", AutoSize = true, Name = "SaveNodeButton" };
        save.Click += (_, _) =>
        {
            if (TrySave()) DialogResult = DialogResult.OK;
        };
        buttons.Controls.AddRange([cancel, save]);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
        save.Enabled = !Server.IsInGroup();
        _fields.Enabled = save.Enabled;
        i18N.TranslateForm(this);

        _prepared = true;
        _fields.ResumeLayout(true);
        _body.ResumeLayout(true);
        ResumeLayout(true);

        var preferredHeight = _fields.PreferredSize.Height + buttons.PreferredSize.Height;
        var availableHeight = Screen.FromControl(this).WorkingArea.Height - (Height - ClientSize.Height) - 24;
        ClientSize = new Size(ClientSize.Width, Math.Min(Math.Max(ClientSize.Height, preferredHeight), availableHeight));
    }

    protected (Label Label, TextBox Field) CreateTextBox(string name, string caption, Func<string, bool> check,
        Action<string> save, string? value, bool multiline = false)
    {
        var field = new TextBox { Name = name + "TextBox", Text = value ?? "", Multiline = multiline };
        if (multiline)
        {
            field.Height = 58;
            field.ScrollBars = ScrollBars.Vertical;
        }
        var label = AddField(caption, field) ?? throw new ArgumentException(nameof(caption));
        _checks.Add(field, check);
        _saves.Add(field, () => save(field.Text));
        return (label, field);
    }

    protected ComboBox CreateComboBox(string name, string caption, IEnumerable<string> values, Action<string> save, string? value)
    {
        var field = new ComboBox { Name = name + "ComboBox", DropDownStyle = ComboBoxStyle.DropDownList };
        SetChoices(field, values, value);
        AddField(caption, field);
        _saves.Add(field, () => save(field.Text));
        return field;
    }

    protected CheckBox CreateCheckBox(string name, string caption, Action<bool> save, bool value)
    {
        var field = new CheckBox { Name = name + "CheckBox", AutoSize = true, Text = caption, Checked = value };
        AddField(null, field);
        _saves.Add(field, () => save(field.Checked));
        return field;
    }

    protected Label AddNotice(string text)
    {
        var notice = new Label { AutoSize = true, MaximumSize = new Size(330, 0), Text = text };
        AddField(null, notice);
        return notice;
    }

    protected void CreateHeaders(string name, Action<string> save, string? value)
    {
        CreateTextBox(name, "Request Headers", s => s == value || s.GetLines().Where(line => !string.IsNullOrWhiteSpace(line)).All(line =>
        {
            var colon = line.IndexOf(':');
            var name = colon > 0 ? line[..colon].Trim() : "";
            return name.Length > 0 && name.All(c => char.IsAsciiLetterOrDigit(c) || "!#$%&'*+-.^_`|~".Contains(c));
        }), s => save(s.Trim() == (value ?? "").Trim() ? value ?? "" : s.Trim() + "\n"), value, multiline: true);
    }

    private Label? AddField(string? caption, Control field)
    {
        var row = _fields.RowCount++;
        var label = caption == null ? null : new Label
        {
            AutoSize = true, Text = caption, Anchor = AnchorStyles.Left, Margin = new Padding(3, 4, 12, 4)
        };
        if (label != null) _fields.Controls.Add(label, 0, row);
        field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _fields.Controls.Add(field, 1, row);
        _labels.Add(field, label);
        return label;
    }

    protected void ShowField(Control control, bool show)
    {
        if (!(show ? _hidden.Remove(control) : _hidden.Add(control))) return;
        control.Visible = show;
        if (_labels[control] is { } label) label.Visible = show;
    }

    protected void UpdateFields(Action update)
    {
        if (_updatingFields) return;
        _updatingFields = true;
        _body.SuspendLayout();
        _fields.SuspendLayout();
        try { update(); }
        finally
        {
            _fields.ResumeLayout(true);
            _body.ResumeLayout(true);
            _updatingFields = false;
        }
    }

    protected void CheckField(Control control, Func<string, bool> check) => _checks[control] = check;

    private static void SetChoices(ComboBox field, IEnumerable<string> choices, string? value)
    {
        var items = choices.ToList();
        if (!string.IsNullOrEmpty(value) && !items.Contains(value)) items.Add(value);
        var index = items.IndexOf(value ?? "");
        var selected = index >= 0 ? index : 0;
        field.BeginUpdate();
        try
        {
            if (!field.Items.Cast<string>().SequenceEqual(items))
            {
                field.Items.Clear();
                field.Items.AddRange(items.ToArray());
            }
            field.SelectedIndex = selected;
        }
        finally { field.EndUpdate(); }
    }

    protected void AddTransport(ComboBox? flow = null)
    {
        var original = string.IsNullOrWhiteSpace(Server.Network) ? Constants.DefaultNetwork : Server.Network.Trim().ToLowerInvariant();
        var networks = Constants.Networks.Where(network => network != "kcp" || TypeName == EConfigType.VMess);
        _network = CreateComboBox("Network", "Network", networks, value => Server.Network = value, original);
        var mode = CreateComboBox("TransportMode", "Transport Mode", [""], value => Server.HeaderType = value, "");
        var host = CreateTextBox("Host", "Host", _ => true, value => Server.RequestHost = value.Trim(), Server.RequestHost);
        var path = CreateTextBox("Path", "Path", _ => true, value => Server.Path = value.Trim(), Server.Path);
        var extra = CreateTextBox("XhttpExtra", "XHTTP Extra JSON", IsJsonObject, value => Server.Extra = value.Trim(), Server.Extra, multiline: true);
        _saves[_network] = () =>
        {
            if (_network.Text != original)
            {
                if (_hidden.Contains(mode)) Server.HeaderType = "";
                if (_hidden.Contains(host.Field)) Server.RequestHost = "";
                if (_hidden.Contains(path.Field)) Server.Path = "";
                if (_hidden.Contains(extra.Field)) Server.Extra = "";
                if (flow != null && _hidden.Contains(flow)) Server.ProtoExtra.Flow = "";
            }
            Server.Network = _network.Text;
        };
        void Refresh()
        {
            var network = _network.Text;
            var choices = network switch
            {
                "xhttp" => Constants.XhttpMode,
                "kcp" => ["none", .. Constants.KcpHeaderTypes.Where(type => type != "dns")],
                _ => new List<string> { "none" }
            };
            var oldMode = network == original ? Server.HeaderType : null;
            SetChoices(mode, choices, oldMode == "none" && network == "xhttp" ? "auto" : oldMode);
            var hasLegacyMode = network == original && !string.IsNullOrEmpty(oldMode) && oldMode != "none" &&
                (network != "grpc" || oldMode != "gun");
            ShowField(mode, network is "kcp" or "xhttp" || hasLegacyMode);
            if (_labels[mode] is { } modeLabel)
                modeLabel.Text = i18N.Translate(network == "xhttp" ? "XHTTP Mode" : "Fake Type");
            var hasHost = network is "ws" or "httpupgrade" or "xhttp" or "http" or "h2";
            ShowField(host.Field, hasHost || network == original && !string.IsNullOrEmpty(Server.RequestHost));
            ShowField(path.Field, network is "ws" or "httpupgrade" or "xhttp" or "http" or "h2" or "grpc" or "kcp" ||
                network == original && !string.IsNullOrEmpty(Server.Path));
            path.Label.Text = i18N.Translate(network switch { "grpc" => "Service Name", "kcp" => "mKCP Seed", _ => "Path" });
            ShowField(extra.Field, network == "xhttp");
        }
        _network.SelectedIndexChanged += (_, _) => UpdateFields(() =>
        {
            Refresh();
            _refreshTransportSecurity?.Invoke();
        });
        Refresh();
    }

    private void AddSecurityFields()
    {
        if (TypeName is EConfigType.SOCKS or EConfigType.WireGuard or EConfigType.ShadowsocksR or EConfigType.Shadowsocks)
            return;

        var fixedTls = TypeName is EConfigType.Hysteria2 or EConfigType.TUIC or EConfigType.AnyTLS or EConfigType.Naive or EConfigType.MASQUE;
        var supportsReality = TypeName is EConfigType.VLESS or EConfigType.VMess or EConfigType.Trojan;
        var naive = TypeName == EConfigType.Naive;
        var selectedSecurity = Server.StreamSecurity;
        var initialNetwork = _network?.Text;
        var security = TypeName == EConfigType.HTTP
            ? CreateComboBox("TLSSecure", "Protocol", ["HTTP", "HTTPS"],
                value => Server.StreamSecurity = value == "HTTPS" ? "tls" : "none",
                selectedSecurity == "tls" ? "HTTPS" : "HTTP")
            : fixedTls ? null : CreateComboBox("TLSSecure", "TLSSecure", ["none", "tls"],
                value => Server.StreamSecurity = value, selectedSecurity);
        var sni = CreateTextBox("Sni", "Sni", _ => true, s => Server.Sni = s.Trim(), Server.Sni).Field;
        var alpn = naive ? null : CreateComboBox("Alpn", "Alpn", Constants.Alpn, value => Server.Alpn = value, Server.Alpn);
        var insecure = naive ? null : CreateComboBox("AllowInsecure", "allowInsecure", Constants.AllowInsecure,
            value => Server.AllowInsecure = bool.TryParse(value, out var result) ? result : null, Server.AllowInsecure?.ToString().ToLowerInvariant());
        var fingerprint = naive || TypeName is EConfigType.Hysteria2 or EConfigType.TUIC ? null :
            CreateComboBox("Fingerprint", "Fingerprint", Constants.Fingerprints, value => Server.Fingerprint = value, Server.Fingerprint);
        var cert = CreateTextBox("Cert", "Trusted PEM Certificate", IsPemChain, s =>
        {
            Server.Cert = s.Trim();
            if (TypeName == EConfigType.Hysteria2 && Server.Cert.Length > 0) Server.CertSha = "";
        }, Server.Cert, multiline: true).Field;
        if (!supportsReality || security == null || alpn == null || insecure == null || fingerprint == null || _network == null)
        {
            if (security != null)
            {
                void UpdateHttp()
                {
                    var enabled = security.Text == "HTTPS";
                    foreach (var field in new Control?[] { sni, alpn, insecure, fingerprint, cert })
                        if (field != null) ShowField(field, enabled);
                }
                security.SelectedIndexChanged += (_, _) => UpdateFields(UpdateHttp);
                _saves[security] = () =>
                {
                    if (security.Text == "HTTP" && selectedSecurity == "tls") Server.Cert = "";
                    Server.StreamSecurity = security.Text == "HTTPS" ? "tls" : "none";
                };
                UpdateHttp();
            }
            return;
        }

        var publicKey = CreateTextBox("PublicKey", "Public Key", s => !string.IsNullOrWhiteSpace(s),
            s => Server.PublicKey = s.Trim(), Server.PublicKey).Field;
        var shortId = CreateTextBox("ShortId", "Short ID", s => s.Length <= 16 && s.Length % 2 == 0 && s.All(Uri.IsHexDigit),
            s => Server.ShortId = s, Server.ShortId).Field;
        var spider = CreateTextBox("SpiderX", "SpiderX", _ => true, s => Server.SpiderX = s, Server.SpiderX).Field;
        var mldsa = CreateTextBox("Mldsa65Verify", "Mldsa65Verify", _ => true, s => Server.Mldsa65Verify = s, Server.Mldsa65Verify).Field;
        var ech = CreateTextBox("EchConfigList", "EchConfigList", _ => true, s => Server.EchConfigList = s, Server.EchConfigList).Field;
        var echQuery = CreateComboBox("EchForceQuery", "EchForceQuery", Constants.EchForceQuerys,
            s => Server.EchForceQuery = s, Server.EchForceQuery);
        var flow = _fields.Controls.Find("FlowComboBox", false).FirstOrDefault();
        var realityNotice = AddNotice("Choose a transport that supports REALITY or change the TLS security mode.");
        bool SupportsReality(string? network) => network is "tcp" or "raw" or "grpc" or "xhttp" ||
            (network is "http" or "h2" && TypeName is EConfigType.VLESS or EConfigType.VMess);
        bool IsNewUnsupportedReality() => security.Text == "reality" && !SupportsReality(_network.Text) &&
            (_network.Text != initialNetwork || selectedSecurity != "reality");
        CheckField(security, _ => !IsNewUnsupportedReality());

        void UpdateSecurity()
        {
            var secure = security.Text is "tls" or "reality";
            var reality = security.Text == "reality";
            var xhttp = _network?.Text == "xhttp";
            ShowField(sni, secure);
            ShowField(alpn, secure);
            ShowField(fingerprint, secure);
            ShowField(insecure, security.Text == "tls");
            ShowField(cert, security.Text == "tls");
            ShowField(publicKey, reality);
            ShowField(shortId, reality);
            ShowField(spider, reality && xhttp);
            ShowField(mldsa, reality && xhttp);
            ShowField(ech, security.Text == "tls" && xhttp);
            ShowField(echQuery, security.Text == "tls" && xhttp);
            ShowField(realityNotice, IsNewUnsupportedReality());
            if (flow != null) ShowField(flow, secure && _network.Text is "tcp" or "raw");
        }
        void UpdateTransportSecurity()
        {
            SetChoices(security, SupportsReality(_network.Text) ? ["none", "tls", "reality"] : ["none", "tls"], security.Text);
            var choices = TransportAlpn(_network?.Text);
            SetChoices(alpn, choices, choices.Contains(alpn.Text) ? alpn.Text : "");
            UpdateSecurity();
        }
        SetChoices(security, SupportsReality(initialNetwork) ? ["none", "tls", "reality"] : ["none", "tls"], selectedSecurity);
        SetChoices(alpn, TransportAlpn(initialNetwork), Server.Alpn);
        security.SelectedIndexChanged += (_, _) => UpdateFields(UpdateSecurity);
        _refreshTransportSecurity = UpdateTransportSecurity;
        _saves[security] = () =>
        {
            if (security.Text != selectedSecurity || _network.Text != initialNetwork)
            {
                if (_hidden.Contains(cert)) Server.Cert = "";
                if (_hidden.Contains(ech)) Server.EchConfigList = "";
                if (_hidden.Contains(echQuery)) Server.EchForceQuery = "";
                if (flow != null && _hidden.Contains(flow)) Server.ProtoExtra.Flow = "";
            }
            Server.StreamSecurity = security.Text;
        };
        UpdateSecurity();
    }

    private static IEnumerable<string> TransportAlpn(string? network) => network switch
    {
        "xhttp" or "quic" => Constants.Alpn,
        "grpc" or "h2" => ["", "h2"],
        "ws" or "httpupgrade" => ["", "http/1.1"],
        _ => ["", "h2", "http/1.1", "h2,http/1.1"]
    };

    private bool TrySave()
    {
        if (Server.IsInGroup()) return false;
        var valid = true;
        foreach (var (field, check) in _checks)
        {
            if (_hidden.Contains(field)) continue;
            var passed = check(field.Text);
            field.ForeColor = passed ? SystemColors.WindowText : Color.Firebrick;
            if (_labels[field] is { } label) label.ForeColor = field.ForeColor;
            valid &= passed;
        }
        if (!valid) return false;

        foreach (var (field, save) in _saves)
            if (!_hidden.Contains(field)) save();
        if (!Global.Settings.Server.Contains(Server)) Global.Settings.Server.Add(Server);
        return true;
    }

    private static bool IsJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException) { return false; }
    }

    private static bool IsPemChain(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        var certificates = CertPemManager.ParsePemChain(text);
        if (certificates.Count == 0) return false;
        try
        {
            foreach (var pem in certificates)
            {
                using var certificate = X509Certificate2.CreateFromPem(pem);
            }
            return true;
        }
        catch (CryptographicException) { return false; }
    }
}

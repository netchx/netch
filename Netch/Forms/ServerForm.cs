using Netch.Enums;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;
using System.ComponentModel;

namespace Netch.Forms;

[DesignerCategory(@"Code")]
[Fody.ConfigureAwait(true)]
public abstract class ServerForm : Form
{
    private const int ControlLineHeight = 28;
    private const int InputBoxWidth = 294;

    private readonly Dictionary<Control, Func<string, bool>> _checkActions = new();

    private readonly Dictionary<Control, Action<object>> _saveActions = new();

    private int _controlLines = 13;
    private Label AddressLabel;
    protected TextBox AddressTextBox;

    private readonly IContainer components = null;

    private GroupBox ConfigurationGroupBox;
    private Label PortLabel;
    private TextBox PortTextBox;
    private Label RemarkLabel;
    protected TextBox RemarkTextBox;

    private Label TLSSecureLable, SniLable, FingerprintLable, PublicKeyLable, ShortIdLable, SpiderXLable, Mldsa65VerifyLable, AlpnLable, allowInsecureLable, EchConfigListLable, EchForceQueryLable;
    private ComboBox TLSSecureComboBox, FingerprintComboBox, AlpnComboBox, allowInsecureComboBox, EchForceQueryComboBox;
    private TextBox SniTextBox, PublicKeyTextBox, ShortIdTextBox, SpiderXTextBox, Mldsa65VerifyTextBox, EchConfigListTextBox;
    protected ServerForm()
    {
        InitializeComponent();

        _checkActions.Add(RemarkTextBox, s => true);
        _saveActions.Add(RemarkTextBox, s => Server.Remarks = (string)s);

        _checkActions.Add(AddressTextBox, s => s != string.Empty);
        _saveActions.Add(AddressTextBox, s => Server.Address = (string)s);

        _checkActions.Add(PortTextBox, s => ushort.TryParse(s, out var port) && port != 0);
        _saveActions.Add(PortTextBox, s => Server.Port = ushort.Parse((string)s));

        _checkActions.Add(SniTextBox, s => true);
        _saveActions.Add(SniTextBox, s => Server.Sni = (string)s);

        _checkActions.Add(PublicKeyTextBox, s => true);
        _saveActions.Add(PublicKeyTextBox, s => Server.PublicKey = (string)s);

        _checkActions.Add(ShortIdTextBox, s => true);
        _saveActions.Add(ShortIdTextBox, s => Server.ShortId = (string)s);

        _checkActions.Add(SpiderXTextBox, s => true);
        _saveActions.Add(SpiderXTextBox, s => Server.SpiderX = (string)s);

        _checkActions.Add(Mldsa65VerifyTextBox, s => true);
        _saveActions.Add(Mldsa65VerifyTextBox, s => Server.Mldsa65Verify = (string)s);

        _checkActions.Add(EchConfigListTextBox, s => true);
        _saveActions.Add(EchConfigListTextBox, s => Server.EchConfigList = (string)s);

        _saveActions.Add(TLSSecureComboBox, s => Server.StreamSecurity = (string)s);
        _saveActions.Add(FingerprintComboBox, s => Server.Fingerprint = (string)s);
        _saveActions.Add(AlpnComboBox, s => Server.Alpn = (string)s);
        _saveActions.Add(allowInsecureComboBox, s => Server.AllowInsecure = (string)s switch
        {
            "" => null,
            "true" => true,
            "false" => false,
            _ => null
        });
        _saveActions.Add(EchForceQueryComboBox, s => Server.EchConfigList = (string)s);


    }

    protected abstract EConfigType TypeName { get; }

    protected Server Server { get; set; }

    public new void ShowDialog()
    {
        AfterFactor();
        base.ShowDialog();
    }

    public new void Show()
    {
        AfterFactor();
        base.Show();
    }

    private void AfterFactor()
    {
        Text = TypeName.ToString() ?? string.Empty;

        RemarkTextBox.Text = Server.Remarks;
        AddressTextBox.Text = Server.Address;
        PortTextBox.Text = Server.Port.ToString();
        TLSSecureComboBox.SelectedIndex = Constants.TLSSecure.IndexOf(Server.StreamSecurity);
        SniTextBox.Text = Server.Sni;
        FingerprintComboBox.SelectedIndex = Constants.Fingerprints.IndexOf(Server.Fingerprint);
        PublicKeyTextBox.Text = Server.PublicKey;
        ShortIdTextBox.Text = Server.ShortId;
        SpiderXTextBox.Text = Server.SpiderX;
        Mldsa65VerifyTextBox.Text = Server.Mldsa65Verify;
        AlpnComboBox.SelectedIndex = Constants.Alpn.IndexOf(Server.Alpn);
        allowInsecureComboBox.SelectedIndex = Server.AllowInsecure switch
        {
            null => 0,
            true => 1,
            false => 2
        };
        EchConfigListTextBox.Text = Server.EchConfigList;
        EchForceQueryComboBox.SelectedIndex = Constants.EchForceQuerys.IndexOf(Server.EchForceQuery);

        AddSaveButton();
        i18N.TranslateForm(this);

        ConfigurationGroupBox.Enabled = !Server.IsInGroup();

        ConfigurationGroupBox.ResumeLayout(false);
        ConfigurationGroupBox.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    protected (Label, TextBox) CreateTextBox(string name,
        string remark,
        Func<string, bool> check,
        Action<string> save,
        string value,
        int width = InputBoxWidth)
    {
        _controlLines++;

        var textBox = new TextBox
        {
            Location = new Point(120, ControlLineHeight * _controlLines),
            Name = $"{name}TextBox",
            Size = new Size(width, 23),
            TextAlign = HorizontalAlignment.Center,
            Text = value
        };

        _checkActions.Add(textBox, check);
        _saveActions.Add(textBox, o => save.Invoke((string)o));
        var label = new Label
        {
            AutoSize = true,
            Location = new Point(10, ControlLineHeight * _controlLines),
            Name = $"{name}Label",
            Size = new Size(56, 17),
            Text = remark
        };

        ConfigurationGroupBox.Controls.AddRange(new Control[]
        {
            label,
            textBox
        });
        return (label, textBox);
    }

    protected void CreateComboBox(string name, string remark, List<string> values, Action<string> save, string value, int width = InputBoxWidth)
    {
        _controlLines++;

        var comboBox = new ComboBox
        {
            Location = new Point(120, ControlLineHeight * _controlLines),
            Name = $"{name}ComboBox",
            Size = new Size(width, 23),
            DrawMode = DrawMode.OwnerDrawFixed,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true
        };

        comboBox.Items.AddRange(values.ToArray());
        comboBox.SelectedIndex = values.IndexOf(value);
        comboBox.DrawItem += Utils.Utils.DrawCenterComboBox;
        _saveActions.Add(comboBox, o => save.Invoke((string)o));
        ConfigurationGroupBox.Controls.AddRange(new Control[]
        {
            comboBox,
            new Label
            {
                AutoSize = true,
                Location = new Point(10, ControlLineHeight * _controlLines),
                Name = $"{name}Label",
                Size = new Size(56, 17),
                Text = remark
            }
        });
    }

    protected void CreateCheckBox(string name, string remark, Action<bool> save, bool value)
    {
        _controlLines++;

        var checkBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(120, ControlLineHeight * _controlLines),
            Name = $"{name}CheckBox",
            Checked = value,
            Text = remark
        };

        _saveActions.Add(checkBox, o => save.Invoke((bool)o));
        ConfigurationGroupBox.Controls.AddRange(new Control[]
        {
            checkBox
        });
    }

    private void AddSaveButton()
    {
        _controlLines++;
        var control = new Button
        {
            Location = new Point(340, _controlLines * ControlLineHeight + 10),
            Name = "ControlButton",
            Size = new Size(75, 23),
            Text = "Save",
            UseVisualStyleBackColor = true
        };

        control.Click += ControlButton_Click;
        ConfigurationGroupBox.Controls.Add(control);
    }

    private void ControlButton_Click(object sender, EventArgs e)
    {
        Utils.Utils.ComponentIterator(this, component => Utils.Utils.ChangeControlForeColor(component, Color.Black));

        var flag = true;
        foreach (var pair in _checkActions.Where(pair => !pair.Value.Invoke(pair.Key.Text)))
        {
            Utils.Utils.ChangeControlForeColor(pair.Key, Color.Red);
            flag = false;
        }

        if (!flag)
            return;

        foreach (var pair in _saveActions)
            switch (pair.Key)
            {
                case CheckBox c:
                    pair.Value.Invoke(c.Checked);
                    break;
                default:
                    pair.Value.Invoke(pair.Key.Text);
                    break;
            }

        if (Global.Settings.Server.IndexOf(Server) == -1)
            Global.Settings.Server.Add(Server);

        MessageBoxX.Show(i18N.Translate("Saved"));

        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        ConfigurationGroupBox = new GroupBox();
        AddressLabel = new Label();
        PortTextBox = new TextBox();
        AddressTextBox = new TextBox();
        RemarkTextBox = new TextBox();
        RemarkLabel = new Label();
        PortLabel = new Label();
        ConfigurationGroupBox.SuspendLayout();
        SuspendLayout();


        TLSSecureLable = new Label();
        SniLable = new Label();
        FingerprintLable = new Label();
        PublicKeyLable = new Label();
        ShortIdLable = new Label();
        SpiderXLable = new Label();
        Mldsa65VerifyLable = new Label();
        AlpnLable = new Label();
        allowInsecureLable = new Label();
        EchConfigListLable = new Label();
        EchForceQueryLable = new Label();

        TLSSecureComboBox = new ComboBox();
        FingerprintComboBox = new ComboBox();
        AlpnComboBox = new ComboBox();
        allowInsecureComboBox = new ComboBox();
        EchForceQueryComboBox = new ComboBox();

        SniTextBox = new TextBox();
        PublicKeyTextBox = new TextBox();
        ShortIdTextBox = new TextBox();
        SpiderXTextBox = new TextBox();
        Mldsa65VerifyTextBox = new TextBox();
        EchConfigListTextBox = new TextBox();

        // 
        // ConfigurationGroupBox
        // 
        ConfigurationGroupBox.AutoSize = true;
        ConfigurationGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ConfigurationGroupBox.Controls.Add(AddressLabel);
        ConfigurationGroupBox.Controls.Add(PortTextBox);
        ConfigurationGroupBox.Controls.Add(AddressTextBox);
        ConfigurationGroupBox.Controls.Add(RemarkTextBox);
        ConfigurationGroupBox.Controls.Add(RemarkLabel);
        ConfigurationGroupBox.Controls.Add(PortLabel);
        ConfigurationGroupBox.Controls.Add(TLSSecureLable);
        ConfigurationGroupBox.Controls.Add(SniLable);
        ConfigurationGroupBox.Controls.Add(FingerprintLable);
        ConfigurationGroupBox.Controls.Add(PublicKeyLable);
        ConfigurationGroupBox.Controls.Add(ShortIdLable);
        ConfigurationGroupBox.Controls.Add(SpiderXLable);
        ConfigurationGroupBox.Controls.Add(Mldsa65VerifyLable);
        ConfigurationGroupBox.Controls.Add(AlpnLable);
        ConfigurationGroupBox.Controls.Add(allowInsecureLable);
        ConfigurationGroupBox.Controls.Add(EchConfigListLable);
        ConfigurationGroupBox.Controls.Add(EchForceQueryLable);
        ConfigurationGroupBox.Controls.Add(TLSSecureComboBox);
        ConfigurationGroupBox.Controls.Add(FingerprintComboBox);
        ConfigurationGroupBox.Controls.Add(AlpnComboBox);
        ConfigurationGroupBox.Controls.Add(allowInsecureComboBox);
        ConfigurationGroupBox.Controls.Add(EchForceQueryComboBox);
        ConfigurationGroupBox.Controls.Add(SniTextBox);
        ConfigurationGroupBox.Controls.Add(PublicKeyTextBox);
        ConfigurationGroupBox.Controls.Add(ShortIdTextBox);
        ConfigurationGroupBox.Controls.Add(SpiderXTextBox);
        ConfigurationGroupBox.Controls.Add(Mldsa65VerifyTextBox);
        ConfigurationGroupBox.Controls.Add(EchConfigListTextBox);
        ConfigurationGroupBox.Dock = DockStyle.Fill;
        ConfigurationGroupBox.Location = new Point(5, 5);
        ConfigurationGroupBox.Name = "ConfigurationGroupBox";
        ConfigurationGroupBox.Size = new Size(434, 127);
        ConfigurationGroupBox.TabIndex = 0;
        ConfigurationGroupBox.TabStop = false;
        ConfigurationGroupBox.Text = "Configuration";
        // 
        // AddressLabel
        // 
        AddressLabel.AutoSize = true;
        AddressLabel.Location = new Point(10, ControlLineHeight * 2);
        AddressLabel.Name = "AddressLabel";
        AddressLabel.Size = new Size(56, 17);
        AddressLabel.TabIndex = 2;
        AddressLabel.Text = "Address";
        // 
        // PortTextBox
        // 
        PortTextBox.Location = new Point(358, ControlLineHeight * 2);
        PortTextBox.Name = "PortTextBox";
        PortTextBox.Size = new Size(56, 23);
        PortTextBox.TabIndex = 5;
        PortTextBox.TextAlign = HorizontalAlignment.Center;
        // 
        // AddressTextBox
        // 
        AddressTextBox.Location = new Point(120, ControlLineHeight * 2);
        AddressTextBox.Name = "AddressTextBox";
        AddressTextBox.Size = new Size(232, 23);
        AddressTextBox.TabIndex = 3;
        AddressTextBox.TextAlign = HorizontalAlignment.Center;
        // 
        // RemarkTextBox
        // 
        RemarkTextBox.Location = new Point(120, ControlLineHeight);
        RemarkTextBox.Name = "RemarkTextBox";
        RemarkTextBox.Size = new Size(294, 23);
        RemarkTextBox.TabIndex = 1;
        RemarkTextBox.TextAlign = HorizontalAlignment.Center;
        // 
        // RemarkLabel
        // 
        RemarkLabel.AutoSize = true;
        RemarkLabel.Location = new Point(10, ControlLineHeight);
        RemarkLabel.Name = "RemarkLabel";
        RemarkLabel.Size = new Size(53, 17);
        RemarkLabel.TabIndex = 0;
        RemarkLabel.Text = "Remark";
        // 
        // PortLabel
        // 
        PortLabel.AutoSize = true;
        PortLabel.Location = new Point(351, ControlLineHeight * 2);
        PortLabel.Name = "PortLabel";
        PortLabel.Size = new Size(11, 17);
        PortLabel.TabIndex = 4;
        PortLabel.Text = ":";
        // 
        // ServerForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ClientSize = new Size(444, 137);
        Controls.Add(ConfigurationGroupBox);
        Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = Icon.FromHandle(Resources.Netch.GetHicon());
        Margin = new Padding(3, 4, 3, 4);
        MaximizeBox = false;
        Name = "ServerForm";
        Padding = new Padding(11, 5, 11, 4);
        StartPosition = FormStartPosition.CenterScreen;
        //
        //TLSSecureLable
        //
        TLSSecureLable.AutoSize = true;
        TLSSecureLable.Location = new Point(10, ControlLineHeight * 3);
        TLSSecureLable.Name = "TLSSecureLable";
        TLSSecureLable.Size = new Size(56, 17);
        TLSSecureLable.Text = "TLSSecure";
        //
        //TLSSecureComboBox
        //
        TLSSecureComboBox.Location = new Point(120, ControlLineHeight * 3);
        TLSSecureComboBox.Name = "TLSSecureComboBox";
        TLSSecureComboBox.Size = new Size(InputBoxWidth, 23);
        TLSSecureComboBox.DrawMode = DrawMode.OwnerDrawFixed;
        TLSSecureComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        TLSSecureComboBox.FormattingEnabled = true;
        TLSSecureComboBox.Items.AddRange(Constants.TLSSecure.ToArray());
        TLSSecureComboBox.DrawItem += Utils.Utils.DrawCenterComboBox;
        //
        //SniLable
        //
        SniLable.AutoSize = true;
        SniLable.Location = new Point(10, ControlLineHeight * 4);
        SniLable.Name = "SniLable";
        SniLable.Size = new Size(56, 17);
        SniLable.Text = "Server Name(Sni)";
        //
        //SniTextBox
        //
        SniTextBox.Location = new Point(120, ControlLineHeight * 4);
        SniTextBox.Name = "SniTextBox";
        SniTextBox.Size = new Size(InputBoxWidth, 23);
        SniTextBox.TextAlign = HorizontalAlignment.Center;
        //
        //FingerprintLable
        //
        FingerprintLable.AutoSize = true;
        FingerprintLable.Location = new Point(10, ControlLineHeight * 5);
        FingerprintLable.Name = "FingerprintLable";
        FingerprintLable.Size = new Size(56, 17);
        FingerprintLable.Text = "Fingerprint";
        //
        //FingerprintComboBox
        //
        FingerprintComboBox.Location = new Point(120, ControlLineHeight * 5);
        FingerprintComboBox.Name = "FingerprintComboBox";
        FingerprintComboBox.Size = new Size(InputBoxWidth, 23);
        FingerprintComboBox.DrawMode = DrawMode.OwnerDrawFixed;
        FingerprintComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        FingerprintComboBox.FormattingEnabled = true;
        FingerprintComboBox.Items.AddRange(Constants.Fingerprints.ToArray());
        FingerprintComboBox.DrawItem += Utils.Utils.DrawCenterComboBox;
        //
        //PublicKeyLable
        //
        PublicKeyLable.AutoSize = true;
        PublicKeyLable.Location = new Point(10, ControlLineHeight * 6);
        PublicKeyLable.Name = "PublicKeyLable";
        PublicKeyLable.Size = new Size(56, 17);
        PublicKeyLable.Text = "PublicKey";
        //
        //PublicKeyTextBox
        //
        PublicKeyTextBox.Location = new Point(120, ControlLineHeight * 6);
        PublicKeyTextBox.Name = "PublicKeyTextBox";
        PublicKeyTextBox.Size = new Size(InputBoxWidth, 23);
        PublicKeyTextBox.TextAlign = HorizontalAlignment.Center;
        //
        //ShortIdLable
        //
        ShortIdLable.AutoSize = true;
        ShortIdLable.Location = new Point(10, ControlLineHeight * 7);
        ShortIdLable.Name = "ShortIdLable";
        ShortIdLable.Size = new Size(56, 17);
        ShortIdLable.Text = "ShortId";
        //
        //ShortIdTextBox
        //
        ShortIdTextBox.Location = new Point(120, ControlLineHeight * 7);
        ShortIdTextBox.Name = "ShortIdTextBox";
        ShortIdTextBox.Size = new Size(InputBoxWidth, 23);
        ShortIdTextBox.TextAlign = HorizontalAlignment.Center;
        //
        //SpiderXLable
        //
        SpiderXLable.AutoSize = true;
        SpiderXLable.Location = new Point(10, ControlLineHeight * 8);
        SpiderXLable.Name = "SpiderXLable";
        SpiderXLable.Size = new Size(56, 17);
        SpiderXLable.Text = "SpiderX";
        //
        //SpiderXTextBox
        //
        SpiderXTextBox.Location = new Point(120, ControlLineHeight * 8);
        SpiderXTextBox.Name = "SpiderXTextBox";
        SpiderXTextBox.Size = new Size(InputBoxWidth, 23);
        SpiderXTextBox.TextAlign = HorizontalAlignment.Center;
        //
        //Mldsa64VerifyLable
        //
        Mldsa65VerifyLable.AutoSize = true;
        Mldsa65VerifyLable.Location = new Point(10, ControlLineHeight * 9);
        Mldsa65VerifyLable.Name = "Mldsa64VerifyLable";
        Mldsa65VerifyLable.Size = new Size(56, 17);
        Mldsa65VerifyLable.Text = "Mldsa64Verify";
        //
        //Mldsa64VerifyTextBox
        //
        Mldsa65VerifyTextBox.Location = new Point(120, ControlLineHeight * 9);
        Mldsa65VerifyTextBox.Name = "Mldsa64VerifyTextBox";
        Mldsa65VerifyTextBox.Size = new Size(InputBoxWidth, 23);
        Mldsa65VerifyTextBox.TextAlign = HorizontalAlignment.Center;
        //
        //AlpnLable
        //
        AlpnLable.AutoSize = true;
        AlpnLable.Location = new Point(10, ControlLineHeight * 10);
        AlpnLable.Name = "AlpnLable";
        AlpnLable.Size = new Size(56, 17);
        AlpnLable.Text = "Alpn";
        //
        //AlpnComboBox
        //
        AlpnComboBox.Location = new Point(120, ControlLineHeight * 10);
        AlpnComboBox.Name = "AlpnComboBox";
        AlpnComboBox.Size = new Size(InputBoxWidth, 23);
        AlpnComboBox.DrawMode = DrawMode.OwnerDrawFixed;
        AlpnComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        AlpnComboBox.FormattingEnabled = true;
        AlpnComboBox.Items.AddRange(Constants.Alpn.ToArray());
        AlpnComboBox.DrawItem += Utils.Utils.DrawCenterComboBox;
        //
        //allowInsecureLable
        //
        allowInsecureLable.AutoSize = true;
        allowInsecureLable.Location = new Point(10, ControlLineHeight * 11);
        allowInsecureLable.Name = "allowInsecureLable";
        allowInsecureLable.Size = new Size(56, 17);
        allowInsecureLable.Text = "allowInsecure";
        //
        //allowInsecureComboBox
        //
        allowInsecureComboBox.Location = new Point(120, ControlLineHeight * 11);
        allowInsecureComboBox.Name = "allowInsecureComboBox";
        allowInsecureComboBox.Size = new Size(InputBoxWidth, 23);
        allowInsecureComboBox.DrawMode = DrawMode.OwnerDrawFixed;
        allowInsecureComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        allowInsecureComboBox.FormattingEnabled = true;
        allowInsecureComboBox.Items.AddRange(Constants.AllowInsecure.ToArray());
        allowInsecureComboBox.DrawItem += Utils.Utils.DrawCenterComboBox;
        //
        //EchConfigListLable
        //
        EchConfigListLable.AutoSize = true;
        EchConfigListLable.Location = new Point(10, ControlLineHeight * 12);
        EchConfigListLable.Name = "EchConfigListLable";
        EchConfigListLable.Size = new Size(56, 17);
        EchConfigListLable.Text = "EchConfigList";
        //
        //EchConfigListTextBox
        //
        EchConfigListTextBox.Location = new Point(120, ControlLineHeight * 12);
        EchConfigListTextBox.Name = "EchConfigListTextBox";
        EchConfigListTextBox.Size = new Size(InputBoxWidth, 23);
        EchConfigListTextBox.TextAlign = HorizontalAlignment.Center;
        //
        //EchForceQueryLable
        //
        EchForceQueryLable.AutoSize = true;
        EchForceQueryLable.Location = new Point(10, ControlLineHeight * 13);
        EchForceQueryLable.Name = "EchForceQueryLable";
        EchForceQueryLable.Size = new Size(56, 17);
        EchForceQueryLable.Text = "EchForceQuery";
        //
        //EchForceQueryComboBox
        //
        EchForceQueryComboBox.Location = new Point(120, ControlLineHeight * 13);
        EchForceQueryComboBox.Name = "EchForceQueryComboBox";
        EchForceQueryComboBox.Size = new Size(InputBoxWidth, 23);
        EchForceQueryComboBox.DrawMode = DrawMode.OwnerDrawFixed;
        EchForceQueryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        EchForceQueryComboBox.FormattingEnabled = true;
        EchForceQueryComboBox.Items.AddRange(Constants.EchForceQuerys.ToArray());
        EchForceQueryComboBox.DrawItem += Utils.Utils.DrawCenterComboBox;
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;
using Netch.Utils;

namespace Netch.Forms.Server
{
    public partial class VMess : Form
    {
        private static Models.Server server;

        public VMess(int index = -1)
        {
            InitializeComponent();

            server = index != -1
                ? Global.Settings.Server[index]
                : new Models.Server {EncryptMethod = Global.EncryptMethods.VMess[0]};
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                e.DrawBackground();

                if (e.Index >= 0)
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    };

                    var brush = new SolidBrush(cbx.ForeColor);

                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    {
                        brush = SystemBrushes.HighlightText as SolidBrush;
                    }

                    e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, brush, e.Bounds, sf);
                }
            }
        }

        private void VMess_Load(object sender, EventArgs e)
        {
            #region InitText

            ConfigurationGroupBox.Text = i18N.Translate(ConfigurationGroupBox.Text);
            RemarkLabel.Text = i18N.Translate(RemarkLabel.Text);
            AddressLabel.Text = i18N.Translate(AddressLabel.Text);
            UserIDLabel.Text = i18N.Translate(UserIDLabel.Text);
            AlterIDLabel.Text = i18N.Translate(AlterIDLabel.Text);
            EncryptMethodLabel.Text = i18N.Translate(EncryptMethodLabel.Text);
            TransferProtocolLabel.Text = i18N.Translate(TransferProtocolLabel.Text);
            FakeTypeLabel.Text = i18N.Translate(FakeTypeLabel.Text);
            HostLabel.Text = i18N.Translate(HostLabel.Text);
            PathLabel.Text = i18N.Translate(PathLabel.Text);
            QUICSecurityLabel.Text = i18N.Translate(QUICSecurityLabel.Text);
            QUICSecretLabel.Text = i18N.Translate(QUICSecretLabel.Text);
            TLSSecureCheckBox.Text = i18N.Translate(TLSSecureCheckBox.Text);
            UseMuxCheckBox.Text = i18N.Translate(UseMuxCheckBox.Text);
            ControlButton.Text = i18N.Translate(ControlButton.Text);

            EncryptMethodComboBox.Items.AddRange(Global.EncryptMethods.VMess.ToArray());
            TransferProtocolComboBox.Items.AddRange(Global.TransferProtocols.ToArray());
            FakeTypeComboBox.Items.AddRange(Global.FakeTypes.ToArray());
            QUICSecurityComboBox.Items.AddRange(Global.EncryptMethods.VMessQUIC.ToArray());

            #endregion

            RemarkTextBox.Text = server.Remark;
            AddressTextBox.Text = server.Hostname;
            PortTextBox.Text = server.Port.ToString();
            UserIDTextBox.Text = server.UserID;
            AlterIDTextBox.Text = server.AlterID.ToString();
            EncryptMethodComboBox.SelectedIndex = Global.EncryptMethods.VMess.IndexOf(server.EncryptMethod);
            TransferProtocolComboBox.SelectedIndex = Global.TransferProtocols.IndexOf(server.TransferProtocol);
            FakeTypeComboBox.SelectedIndex = Global.FakeTypes.IndexOf(server.FakeType);
            HostTextBox.Text = server.Host;
            PathTextBox.Text = server.Path;
            QUICSecurityComboBox.SelectedIndex = Global.EncryptMethods.VMessQUIC.IndexOf(server.QUICSecure);
            QUICSecretTextBox.Text = server.QUICSecret;
            TLSSecureCheckBox.Checked = server.TLSSecure;
            UseMuxCheckBox.Checked = server.UseMux;
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(PortTextBox.Text, out var port))
            {
                return;
            }

            if (AlterIDTextBox.Text == "")
            {
                MessageBoxX.Show(i18N.Translate("Please fill in alterID"));
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out var afterId))
            {
                return;
            }

            server.Remark = RemarkTextBox.Text;
            server.Type = "VMess";
            server.Hostname = AddressTextBox.Text;
            server.Port = port;
            server.UserID = UserIDTextBox.Text;
            server.AlterID = afterId;
            server.EncryptMethod = EncryptMethodComboBox.Text;
            server.TransferProtocol = TransferProtocolComboBox.Text;
            server.FakeType = FakeTypeComboBox.Text;
            server.Host = HostTextBox.Text;
            server.Path = PathTextBox.Text;
            server.QUICSecure = QUICSecurityComboBox.Text;
            server.QUICSecret = QUICSecretTextBox.Text;
            server.TLSSecure = TLSSecureCheckBox.Checked;
            server.UseMux = UseMuxCheckBox.Checked;
            server.Country = null;

            if (Global.Settings.Server.IndexOf(server) == -1)
            {
                Global.Settings.Server.Add(server);
            }

            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Global.MainForm.InitServer();
            Close();
        }
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Netch.Forms.Server
{
    public partial class VMess : Form
    {
        public int Index;

        public VMess(int index = -1)
        {
            InitializeComponent();

            Index = index;
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            var cbx = sender as ComboBox;
            if (cbx != null)
            {
                e.DrawBackground();

                if (e.Index >= 0)
                {
                    var sf = new StringFormat();
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Alignment = StringAlignment.Center;

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
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            RemarkLabel.Text = Utils.i18N.Translate("Remark");
            AddressLabel.Text = Utils.i18N.Translate("Address");
            UserIDLabel.Text = Utils.i18N.Translate("User ID");
            AlterIDLabel.Text = Utils.i18N.Translate("Alter ID");
            EncryptMethodLabel.Text = Utils.i18N.Translate("Encrypt Method");
            TransferProtocolLabel.Text = Utils.i18N.Translate("Transfer Protocol");
            FakeTypeLabel.Text = Utils.i18N.Translate("Fake Type");
            HostLabel.Text = Utils.i18N.Translate("Host");
            PathLabel.Text = Utils.i18N.Translate("Path");
            QUICSecurityLabel.Text = Utils.i18N.Translate("QUIC Security");
            QUICSecretLabel.Text = Utils.i18N.Translate("QUIC Secret");
            TLSSecureCheckBox.Text = Utils.i18N.Translate("TLS Secure");
            ControlButton.Text = Utils.i18N.Translate("Save");

            foreach (var encrypt in Global.EncryptMethods.VMess)
            {
                EncryptMethodComboBox.Items.Add(encrypt);
            }

            foreach (var protocol in Global.TransferProtocols)
            {
                TransferProtocolComboBox.Items.Add(protocol);
            }

            foreach (var fake in Global.FakeTypes)
            {
                FakeTypeComboBox.Items.Add(fake);
            }

            foreach (var security in Global.EncryptMethods.VMessQUIC)
            {
                QUICSecurityComboBox.Items.Add(security);
            }

            if (Index != -1)
            {
                RemarkTextBox.Text = Global.Server[Index].Remark;
                AddressTextBox.Text = Global.Server[Index].Address;
                PortTextBox.Text = Global.Server[Index].Port.ToString();
                UserIDTextBox.Text = Global.Server[Index].UserID;
                AlterIDTextBox.Text = Global.Server[Index].AlterID.ToString();
                EncryptMethodComboBox.SelectedIndex = Global.EncryptMethods.VMess.IndexOf(Global.Server[Index].EncryptMethod);
                TransferProtocolComboBox.SelectedIndex = Global.TransferProtocols.IndexOf(Global.Server[Index].TransferProtocol);
                FakeTypeComboBox.SelectedIndex = Global.FakeTypes.IndexOf(Global.Server[Index].FakeType);
                HostTextBox.Text = Global.Server[Index].Host;
                PathTextBox.Text = Global.Server[Index].Path;
                QUICSecurityComboBox.SelectedIndex = Global.EncryptMethods.VMessQUIC.IndexOf(Global.Server[Index].QUICSecurity);
                QUICSecretTextBox.Text = Global.Server[Index].QUICSecret;
                TLSSecureCheckBox.Checked = Global.Server[Index].TLSSecure;
            }
            else
            {
                EncryptMethodComboBox.SelectedIndex = 0;
                TransferProtocolComboBox.SelectedIndex = 0;
                FakeTypeComboBox.SelectedIndex = 0;
                QUICSecurityComboBox.SelectedIndex = 0;
            }
        }

        private void VMess_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (Index == -1)
            {
                Global.Server.Add(new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Type = "VMess",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    UserID = UserIDTextBox.Text,
                    AlterID = int.Parse(AlterIDTextBox.Text),
                    EncryptMethod = EncryptMethodComboBox.Text,
                    TransferProtocol = TransferProtocolComboBox.Text,
                    FakeType = FakeTypeComboBox.Text,
                    Host = HostTextBox.Text,
                    Path = PathTextBox.Text,
                    QUICSecurity = QUICSecurityComboBox.Text,
                    QUICSecret = QUICSecretTextBox.Text,
                    TLSSecure = TLSSecureCheckBox.Checked
                });
            }
            else
            {
                Global.Server[Index] = new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Type = "VMess",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    UserID = UserIDTextBox.Text,
                    AlterID = int.Parse(AlterIDTextBox.Text),
                    EncryptMethod = EncryptMethodComboBox.Text,
                    TransferProtocol = TransferProtocolComboBox.Text,
                    FakeType = FakeTypeComboBox.Text,
                    Host = HostTextBox.Text,
                    Path = PathTextBox.Text,
                    QUICSecurity = QUICSecurityComboBox.Text,
                    QUICSecret = QUICSecretTextBox.Text,
                    TLSSecure = TLSSecureCheckBox.Checked
                };
            }

            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Global.MainForm.InitServer();
            Close();
        }
    }
}

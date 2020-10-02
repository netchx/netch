using System;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers.VMess.Form
{
    public partial class VMessForm : System.Windows.Forms.Form
    {
        private static VMess _server;

        public VMessForm(Server server = default)
        {
            InitializeComponent();

            _server = (VMess) server ?? new VMess();
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Utils.Utils.DrawCenterComboBox(sender, e);
        }

        private void VMess_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);

            EncryptMethodComboBox.Items.AddRange(VMessGlobal.EncryptMethods.ToArray());
            TransferProtocolComboBox.Items.AddRange(VMessGlobal.TransferProtocols.ToArray());
            FakeTypeComboBox.Items.AddRange(VMessGlobal.FakeTypes.ToArray());
            QUICSecurityComboBox.Items.AddRange(VMessGlobal.QUIC.ToArray());


            RemarkTextBox.Text = _server.Remark;
            AddressTextBox.Text = _server.Hostname;
            PortTextBox.Text = _server.Port.ToString();
            UserIDTextBox.Text = _server.UserID;
            AlterIDTextBox.Text = _server.AlterID.ToString();
            EncryptMethodComboBox.SelectedIndex = VMessGlobal.EncryptMethods.IndexOf(_server.EncryptMethod);
            TransferProtocolComboBox.SelectedIndex = VMessGlobal.TransferProtocols.IndexOf(_server.TransferProtocol);
            FakeTypeComboBox.SelectedIndex = VMessGlobal.FakeTypes.IndexOf(_server.FakeType);
            HostTextBox.Text = _server.Host;
            PathTextBox.Text = _server.Path;
            QUICSecurityComboBox.SelectedIndex = VMessGlobal.QUIC.IndexOf(_server.QUICSecure);
            QUICSecretTextBox.Text = _server.QUICSecret;
            TLSSecureCheckBox.Checked = _server.TLSSecure;
            UseMuxCheckBox.Checked = _server.UseMux;
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!ushort.TryParse(PortTextBox.Text, out var port)) return;

            if (!int.TryParse(AlterIDTextBox.Text, out var alterId)) return;

            _server.Remark = RemarkTextBox.Text;
            _server.Type = "VMess";
            _server.Hostname = AddressTextBox.Text;
            _server.Port = port;
            _server.UserID = UserIDTextBox.Text;
            _server.AlterID = alterId;
            _server.EncryptMethod = EncryptMethodComboBox.Text;
            _server.TransferProtocol = TransferProtocolComboBox.Text;
            _server.FakeType = FakeTypeComboBox.Text;
            _server.Host = HostTextBox.Text;
            _server.Path = PathTextBox.Text;
            _server.QUICSecure = QUICSecurityComboBox.Text;
            _server.QUICSecret = QUICSecretTextBox.Text;
            _server.TLSSecure = TLSSecureCheckBox.Checked;
            _server.UseMux = UseMuxCheckBox.Checked;
            _server.Country = null;

            if (Global.Settings.Server.IndexOf(_server) == -1)
                Global.Settings.Server.Add(_server);

            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
using System;
using System.Windows.Forms;
using Netch.Utils;

namespace Netch.Servers.ShadowsocksR.Form
{
    public partial class ShadowsocksRForm : System.Windows.Forms.Form
    {
        private readonly ShadowsocksR _server;

        public ShadowsocksRForm(Models.Server server = default)
        {
            InitializeComponent();

            _server = (ShadowsocksR) (server ?? new ShadowsocksR());
        }

        private void ShadowsocksR_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);

            EncryptMethodComboBox.Items.AddRange(SSRGlobal.EncryptMethods.ToArray());
            ProtocolComboBox.Items.AddRange(SSRGlobal.Protocols.ToArray());
            OBFSComboBox.Items.AddRange(SSRGlobal.OBFSs.ToArray());

            RemarkTextBox.Text = _server.Remark;
            AddressTextBox.Text = _server.Hostname;
            PortTextBox.Text = _server.Port.ToString();
            PasswordTextBox.Text = _server.Password;
            EncryptMethodComboBox.SelectedIndex = SSRGlobal.EncryptMethods.IndexOf(_server.EncryptMethod);
            ProtocolComboBox.SelectedIndex = SSRGlobal.Protocols.IndexOf(_server.Protocol);
            ProtocolParamTextBox.Text = _server.ProtocolParam;
            OBFSComboBox.SelectedIndex = SSRGlobal.OBFSs.IndexOf(_server.OBFS);
            OBFSOptionParamTextBox.Text = _server.OBFSParam;
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Utils.Utils.DrawCenterComboBox(sender, e);
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!ushort.TryParse(PortTextBox.Text, out var port)) return;

            _server.Remark = RemarkTextBox.Text;
            _server.Type = "SSR";
            _server.Hostname = AddressTextBox.Text;
            _server.Port = port;
            _server.Password = PasswordTextBox.Text;
            _server.EncryptMethod = EncryptMethodComboBox.Text;
            _server.Protocol = ProtocolComboBox.Text;
            _server.ProtocolParam = ProtocolParamTextBox.Text;
            _server.OBFS = OBFSComboBox.Text;
            _server.OBFSParam = OBFSOptionParamTextBox.Text;
            _server.Country = null;

            if (Global.Settings.Server.IndexOf(_server) == -1)
                Global.Settings.Server.Add(_server);

            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
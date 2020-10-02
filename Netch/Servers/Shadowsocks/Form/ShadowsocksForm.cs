using System;
using System.Windows.Forms;
using Netch.Utils;

namespace Netch.Servers.Shadowsocks.Form
{
    public partial class ShadowsocksForm : System.Windows.Forms.Form
    {
        private readonly Shadowsocks _server;

        public ShadowsocksForm(Shadowsocks server = default)
        {
            InitializeComponent();

            _server = server ?? new Shadowsocks();
        }

        private void Shadowsocks_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);

            EncryptMethodComboBox.Items.AddRange(SSGlobal.EncryptMethods.ToArray());
            
            RemarkTextBox.Text = _server.Remark;
            AddressTextBox.Text = _server.Hostname;
            PortTextBox.Text = _server.Port.ToString();
            PasswordTextBox.Text = _server.Password;
            EncryptMethodComboBox.SelectedIndex = SSGlobal.EncryptMethods.IndexOf(_server.EncryptMethod);
            PluginTextBox.Text = _server.Plugin;
            PluginOptionsTextBox.Text = _server.PluginOption;
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Utils.Utils.DrawCenterComboBox(sender, e);
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!ushort.TryParse(PortTextBox.Text, out var port)) return;

            _server.Remark = RemarkTextBox.Text;
            _server.Type = "SS";
            _server.Hostname = AddressTextBox.Text;
            _server.Port = port;
            _server.Password = PasswordTextBox.Text;
            _server.EncryptMethod = EncryptMethodComboBox.Text;
            _server.Plugin = PluginTextBox.Text;
            _server.PluginOption = PluginOptionsTextBox.Text;
            _server.Country = null;

            if (Global.Settings.Server.IndexOf(_server) == -1)
                Global.Settings.Server.Add(_server);

            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
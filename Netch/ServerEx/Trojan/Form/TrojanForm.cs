using System;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.ServerEx.Trojan.Form
{
    public partial class TrojanForm : System.Windows.Forms.Form
    {
        private readonly Trojan _server;

        public TrojanForm(Server server = default)
        {
            InitializeComponent();

            _server = (Trojan) (server ?? new Trojan());
        }

        private void TrojanForm_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);

            RemarkTextBox.Text = _server.Remark;
            AddressTextBox.Text = _server.Hostname;
            PortTextBox.Text = _server.Port.ToString();
            PasswordTextBox.Text = _server.Password;
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Utils.Utils.DrawCenterComboBox(sender, e);
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!ushort.TryParse(PortTextBox.Text, out var port)) return;

            _server.Remark = RemarkTextBox.Text;
            _server.Type = "Trojan";
            _server.Hostname = AddressTextBox.Text;
            _server.Port = port;
            _server.Password = PasswordTextBox.Text;
            _server.Country = null;

            if (Global.Settings.Server.IndexOf(_server) == -1)
                Global.Settings.Server.Add(_server);

            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
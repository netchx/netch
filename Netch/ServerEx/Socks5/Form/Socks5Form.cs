using System;
using Netch.Utils;

namespace Netch.ServerEx.Socks5.Form
{
    public partial class Socks5Form : System.Windows.Forms.Form
    {
        private readonly Socks5 _server;

        public Socks5Form(Models.Server server = default)
        {
            InitializeComponent();

            _server = (Socks5) (server ?? new Socks5());
        }

        private void Shadowsocks_Load(object sender, EventArgs e)
        {
            #region InitText

            ConfigurationGroupBox.Text = i18N.Translate(ConfigurationGroupBox.Text);
            RemarkLabel.Text = i18N.Translate(RemarkLabel.Text);
            AddressLabel.Text = i18N.Translate(AddressLabel.Text);
            UsernameLabel.Text = i18N.Translate(UsernameLabel.Text);
            PasswordLabel.Text = i18N.Translate(PasswordLabel.Text);
            ControlButton.Text = i18N.Translate(ControlButton.Text);

            #endregion

            RemarkTextBox.Text = _server.Remark;
            AddressTextBox.Text = _server.Hostname;
            PortTextBox.Text = _server.Port.ToString();
            UsernameTextBox.Text = _server.Username;
            PasswordTextBox.Text = _server.Password;
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!ushort.TryParse(PortTextBox.Text, out var port)) return;

            _server.Remark = RemarkTextBox.Text;
            _server.Type = "Socks5";
            _server.Hostname = AddressTextBox.Text;
            _server.Port = port;
            _server.Username = UsernameTextBox.Text;
            _server.Password = PasswordTextBox.Text;
            _server.Country = null;

            if (Global.Settings.Server.IndexOf(_server) == -1)
                Global.Settings.Server.Add(_server);

            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
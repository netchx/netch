using System;
using System.Drawing;
using System.Windows.Forms;
using Netch.Utils;

namespace Netch.Forms.Server
{
    public partial class Socks5 : Form
    {
        private readonly Models.Server _server;

        public Socks5(Models.Server server = default)
        {
            InitializeComponent();

            _server = server ?? new Models.Server();
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

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(PortTextBox.Text, out var port))
            {
                return;
            }

            _server.Remark = RemarkTextBox.Text;
            _server.Type = "Socks5";
            _server.Hostname = AddressTextBox.Text;
            _server.Port = port;
            _server.Username = UsernameTextBox.Text;
            _server.Password = PasswordTextBox.Text;
            _server.Country = null;

            if (Global.Settings.Server.IndexOf(_server) == -1)
            {
                Global.Settings.Server.Add(_server);
            }

            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
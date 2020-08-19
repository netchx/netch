using System;
using System.Drawing;
using System.Windows.Forms;
using Netch.Utils;

namespace Netch.Forms.Server
{
    public partial class Trojan : Form
    {
        private readonly Models.Server _server;

        /// <summary>
        ///     初始化
        /// </summary>
        /// <param name="index">需要编辑的索引</param>
        public Trojan(int index = -1)
        {
            InitializeComponent();


            _server = index != -1
                ? Global.Settings.Server[index]
                : new Models.Server();
        }

        private void Trojan_Load(object sender, EventArgs e)
        {
            ConfigurationGroupBox.Text = i18N.Translate(ConfigurationGroupBox.Text);
            RemarkLabel.Text = i18N.Translate(RemarkLabel.Text);
            AddressLabel.Text = i18N.Translate(AddressLabel.Text);
            PasswordLabel.Text = i18N.Translate(PasswordLabel.Text);
            ControlButton.Text = i18N.Translate(ControlButton.Text);

            RemarkTextBox.Text = _server.Remark;
            AddressTextBox.Text = _server.Hostname;
            PortTextBox.Text = _server.Port.ToString();
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
            _server.Type = "Trojan";
            _server.Hostname = AddressTextBox.Text;
            _server.Port = port;
            _server.Password = PasswordTextBox.Text;
            _server.Country = null;

            if (Global.Settings.Server.IndexOf(_server) == -1)
            {
                Global.Settings.Server.Add(_server);
            }

            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Global.MainForm.InitServer();
            Close();
        }
    }
}
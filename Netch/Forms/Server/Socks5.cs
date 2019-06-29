using System;
using System.Drawing;
using System.Windows.Forms;

namespace Netch.Forms.Server
{
    public partial class Socks5 : Form
    {
        public int Index;

        /// <summary>
        ///     初始化
        /// </summary>
        /// <param name="index">需要编辑的索引</param>
        public Socks5(int index = -1)
        {
            InitializeComponent();

            Index = index;
        }

        private void Shadowsocks_Load(object sender, EventArgs e)
        {
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            RemarkLabel.Text = Utils.i18N.Translate("Remark");
            AddressLabel.Text = Utils.i18N.Translate("Address");
            UsernameLabel.Text = Utils.i18N.Translate("Username");
            PasswordLabel.Text = Utils.i18N.Translate("Password");
            ControlButton.Text = Utils.i18N.Translate("Save");

            if (Index != -1)
            {
                RemarkTextBox.Text = Global.Server[Index].Remark;
                AddressTextBox.Text = Global.Server[Index].Address;
                PortTextBox.Text = Global.Server[Index].Port.ToString();
                UsernameTextBox.Text = Global.Server[Index].Username;
                PasswordTextBox.Text = Global.Server[Index].Password;
            }
        }

        private void Shadowsocks_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
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

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (Index == -1)
            {
                Global.Server.Add(new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Type = "Socks5",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Username = UsernameTextBox.Text,
                    Password = PasswordTextBox.Text
                });
            }
            else
            {
                Global.Server[Index] = new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Group = Global.Server[Index].Group,
                    Type = "Socks5",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Username = UsernameTextBox.Text,
                    Password = PasswordTextBox.Text
                };
            }

            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Global.MainForm.InitServer();
            Close();
        }
    }
}

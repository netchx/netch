using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Netch.Forms.Server
{
    public partial class Trojan : Form
    {
        public int Index;

        /// <summary>
        ///     初始化
        /// </summary>
        /// <param name="index">需要编辑的索引</param>
        public Trojan(int index = -1)
        {
            InitializeComponent();

            Index = index;
        }

        private void Trojan_Load(object sender, EventArgs e)
        {
            ConfigurationGroupBox.Text = Utils.i18N.Translate(ConfigurationGroupBox.Text);
            RemarkLabel.Text = Utils.i18N.Translate(RemarkLabel.Text);
            AddressLabel.Text = Utils.i18N.Translate(AddressLabel.Text);
            PasswordLabel.Text = Utils.i18N.Translate(PasswordLabel.Text);
            ControlButton.Text = Utils.i18N.Translate(ControlButton.Text);

            if (Index != -1)
            {
                RemarkTextBox.Text = Global.Settings.Server[Index].Remark;
                AddressTextBox.Text = Global.Settings.Server[Index].Hostname;
                PortTextBox.Text = Global.Settings.Server[Index].Port.ToString();
                PasswordTextBox.Text = Global.Settings.Server[Index].Password;
            }
        }

        private void Trojan_FormClosing(object sender, FormClosingEventArgs e)
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
            if (!Regex.Match(PortTextBox.Text, "^[0-9]+$").Success)
            {
                return;
            }
            if (Index == -1)
            {
                Global.Settings.Server.Add(new Models.Server
                {
                    Remark = RemarkTextBox.Text,
                    Type = "Trojan",
                    Hostname = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text
                });
            }
            else
            {
                Global.Settings.Server[Index] = new Models.Server
                {
                    Remark = RemarkTextBox.Text,
                    Group = Global.Settings.Server[Index].Group,
                    Type = "Trojan",
                    Hostname = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text
                };
            }

            Utils.Configuration.Save();
            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Global.MainForm.InitServer();
            Close();
        }
    }
}

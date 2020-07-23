using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Netch.Utils;

namespace Netch.Forms.Server
{
    public partial class Shadowsocks : Form
    {
        public int Index;

        /// <summary>
        ///     初始化
        /// </summary>
        /// <param name="index">需要编辑的索引</param>
        public Shadowsocks(int index = -1)
        {
            InitializeComponent();

            Index = index;
        }

        private void Shadowsocks_Load(object sender, EventArgs e)
        {
            ConfigurationGroupBox.Text = i18N.Translate(ConfigurationGroupBox.Text);
            RemarkLabel.Text = i18N.Translate(RemarkLabel.Text);
            AddressLabel.Text = i18N.Translate(AddressLabel.Text);
            PasswordLabel.Text = i18N.Translate(PasswordLabel.Text);
            EncryptMethodLabel.Text = i18N.Translate(EncryptMethodLabel.Text);
            PluginLabel.Text = i18N.Translate(PluginLabel.Text);
            PluginOptionsLabel.Text = i18N.Translate(PluginOptionsLabel.Text);
            ControlButton.Text = i18N.Translate(ControlButton.Text);

            foreach (var encrypt in Global.EncryptMethods.SS)
            {
                EncryptMethodComboBox.Items.Add(encrypt);
            }

            if (Index != -1)
            {
                RemarkTextBox.Text = Global.Settings.Server[Index].Remark;
                AddressTextBox.Text = Global.Settings.Server[Index].Hostname;
                PortTextBox.Text = Global.Settings.Server[Index].Port.ToString();
                PasswordTextBox.Text = Global.Settings.Server[Index].Password;
                EncryptMethodComboBox.SelectedIndex = Global.EncryptMethods.SS.IndexOf(Global.Settings.Server[Index].EncryptMethod);
                PluginTextBox.Text = Global.Settings.Server[Index].Plugin;
                PluginOptionsTextBox.Text = Global.Settings.Server[Index].PluginOption;
            }
            else
            {
                EncryptMethodComboBox.SelectedIndex = 0;
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
            if (!Regex.Match(PortTextBox.Text, "^[0-9]+$").Success)
            {
                return;
            }
            if (Index == -1)
            {
                Global.Settings.Server.Add(new Models.Server
                {
                    Remark = RemarkTextBox.Text,
                    Type = "SS",
                    Hostname = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    Plugin = PluginTextBox.Text,
                    PluginOption = PluginOptionsTextBox.Text
                });
            }
            else
            {
                Global.Settings.Server[Index] = new Models.Server
                {
                    Remark = RemarkTextBox.Text,
                    Group = Global.Settings.Server[Index].Group,
                    Type = "SS",
                    Hostname = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    Plugin = PluginTextBox.Text,
                    PluginOption = PluginOptionsTextBox.Text,
                    Country = null
                };
            }

            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Global.MainForm.InitServer();
            Close();
        }
    }
}

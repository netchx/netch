using System;
using System.Drawing;
using System.Windows.Forms;

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
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            RemarkLabel.Text = Utils.i18N.Translate("Remark");
            AddressLabel.Text = Utils.i18N.Translate("Address");
            PasswordLabel.Text = Utils.i18N.Translate("Password");
            EncryptMethodLabel.Text = Utils.i18N.Translate("Encrypt Method");
            PluginLabel.Text = Utils.i18N.Translate("Plugin");
            PluginOptionsLabel.Text = Utils.i18N.Translate("Plugin Options");
            ControlButton.Text = Utils.i18N.Translate("Save");

            foreach (var encrypt in Global.EncryptMethods.SS)
            {
                EncryptMethodComboBox.Items.Add(encrypt);
            }

            if (Index != -1)
            {
                RemarkTextBox.Text = Global.Server[Index].Remark;
                AddressTextBox.Text = Global.Server[Index].Address;
                PortTextBox.Text = Global.Server[Index].Port.ToString();
                PasswordTextBox.Text = Global.Server[Index].Password;
                EncryptMethodComboBox.SelectedIndex = Global.EncryptMethods.SS.IndexOf(Global.Server[Index].EncryptMethod);
                PluginTextBox.Text = Global.Server[Index].OBFS;
                PluginOptionsTextBox.Text = Global.Server[Index].OBFSParam;
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
            if (Index == -1)
            {
                Global.Server.Add(new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Type = "Shadowsocks",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    OBFS = PluginTextBox.Text,
                    OBFSParam = PluginOptionsTextBox.Text                 
                });
            }
            else
            {
                Global.Server[Index] = new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Group = Global.Server[Index].Group,
                    Type = "Shadowsocks",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    OBFS = PluginTextBox.Text,
                    OBFSParam = PluginOptionsTextBox.Text
                };
            }

            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Global.MainForm.InitServer();
            Close();
        }
    }
}

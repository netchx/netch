using System;
using System.Drawing;
using System.Windows.Forms;

namespace Netch.Forms.Server
{
    public partial class ShadowsocksR : Form
    {
        public int Index;

        /// <summary>
        ///     初始化
        /// </summary>
        /// <param name="index">需要编辑的索引</param>
        public ShadowsocksR(int index = -1)
        {
            InitializeComponent();

            Index = index;
        }

        private void ShadowsocksR_Load(object sender, EventArgs e)
        {
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            RemarkLabel.Text = Utils.i18N.Translate("Remark");
            AddressLabel.Text = Utils.i18N.Translate("Address");
            PasswordLabel.Text = Utils.i18N.Translate("Password");
            EncryptMethodLabel.Text = Utils.i18N.Translate("Encrypt Method");
            ProtocolLabel.Text = Utils.i18N.Translate("Protocol");
            ProtocolParamLabel.Text = Utils.i18N.Translate("Protocol Param");
            OBFSLabel.Text = Utils.i18N.Translate("OBFS");
            OBFSParamLabel.Text = Utils.i18N.Translate("OBFS Param");
            ControlButton.Text = Utils.i18N.Translate("Save");

            foreach (var encrypt in Global.EncryptMethods.SSR)
            {
                EncryptMethodComboBox.Items.Add(encrypt);
            }

            foreach (var protocol in Global.Protocols)
            {
                ProtocolComboBox.Items.Add(protocol);
            }

            foreach (var obfs in Global.OBFSs)
            {
                PluginComboBox.Items.Add(obfs);
            }

            if (Index != -1)
            {
                RemarkTextBox.Text = Global.Settings.Server[Index].Remark;
                AddressTextBox.Text = Global.Settings.Server[Index].Hostname;
                PortTextBox.Text = Global.Settings.Server[Index].Port.ToString();
                PasswordTextBox.Text = Global.Settings.Server[Index].Password;
                EncryptMethodComboBox.SelectedIndex = Global.EncryptMethods.SSR.IndexOf(Global.Settings.Server[Index].EncryptMethod);
                ProtocolComboBox.SelectedIndex = Global.Protocols.IndexOf(Global.Settings.Server[Index].Protocol);
                ProtocolParamTextBox.Text = Global.Settings.Server[Index].ProtocolParam;
                PluginComboBox.SelectedIndex = Global.OBFSs.IndexOf(Global.Settings.Server[Index].Plugin);
                PluginOptionParamTextBox.Text = Global.Settings.Server[Index].PluginOption;
            }
            else
            {
                EncryptMethodComboBox.SelectedIndex = 0;
                ProtocolComboBox.SelectedIndex = 0;
                PluginComboBox.SelectedIndex = 0;
            }
        }

        private void ShadowsocksR_FormClosing(object sender, FormClosingEventArgs e)
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
                Global.Settings.Server.Add(new Models.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Type = "SSR",
                    Hostname = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    Protocol = ProtocolComboBox.Text,
                    ProtocolParam = ProtocolParamTextBox.Text,
                    Plugin = PluginComboBox.Text,
                    PluginOption = PluginOptionParamTextBox.Text
                });
            }
            else
            {
                Global.Settings.Server[Index] = new Models.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Group = Global.Settings.Server[Index].Group,
                    Type = "SSR",
                    Hostname = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    Protocol = ProtocolComboBox.Text,
                    ProtocolParam = ProtocolParamTextBox.Text,
                    Plugin = PluginComboBox.Text,
                    PluginOption = PluginOptionParamTextBox.Text
                };
            }

            Utils.Configuration.Save();
            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Global.MainForm.InitServer();
            Close();
        }
    }
}

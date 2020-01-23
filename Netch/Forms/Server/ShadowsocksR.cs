using System;
using System.Drawing;
using System.Text.RegularExpressions;
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
            ConfigurationGroupBox.Text = Utils.i18N.Translate(ConfigurationGroupBox.Text);
            RemarkLabel.Text = Utils.i18N.Translate(RemarkLabel.Text);
            AddressLabel.Text = Utils.i18N.Translate(AddressLabel.Text);
            PasswordLabel.Text = Utils.i18N.Translate(PasswordLabel.Text);
            EncryptMethodLabel.Text = Utils.i18N.Translate(EncryptMethodLabel.Text);
            ProtocolLabel.Text = Utils.i18N.Translate(ProtocolLabel.Text);
            ProtocolParamLabel.Text = Utils.i18N.Translate(ProtocolParamLabel.Text);
            OBFSLabel.Text = Utils.i18N.Translate(OBFSLabel.Text);
            OBFSParamLabel.Text = Utils.i18N.Translate(OBFSParamLabel.Text);
            ControlButton.Text = Utils.i18N.Translate(ControlButton.Text);

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
                OBFSComboBox.Items.Add(obfs);
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
                OBFSComboBox.SelectedIndex = Global.OBFSs.IndexOf(Global.Settings.Server[Index].OBFS);
                OBFSOptionParamTextBox.Text = Global.Settings.Server[Index].OBFSParam;
            }
            else
            {
                EncryptMethodComboBox.SelectedIndex = 0;
                ProtocolComboBox.SelectedIndex = 0;
                OBFSComboBox.SelectedIndex = 0;
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
            if (!Regex.Match(PortTextBox.Text, "^[0-9]+$").Success)
            {
                return;
            }
            if (Index == -1)
            {
                Global.Settings.Server.Add(new Models.Server
                {
                    Remark = RemarkTextBox.Text,
                    Type = "SSR",
                    Hostname = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    Protocol = ProtocolComboBox.Text,
                    ProtocolParam = ProtocolParamTextBox.Text,
                    OBFS = OBFSComboBox.Text,
                    OBFSParam = OBFSOptionParamTextBox.Text
                });
            }
            else
            {
                Global.Settings.Server[Index] = new Models.Server
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
                    OBFS = OBFSComboBox.Text,
                    OBFSParam = OBFSOptionParamTextBox.Text
                };
            }

            Utils.Configuration.Save();
            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Global.MainForm.InitServer();
            Close();
        }
    }
}

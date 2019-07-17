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
                OBFSComboBox.Items.Add(obfs);
            }

            if (Index != -1)
            {
                RemarkTextBox.Text = Global.Server[Index].Remark;
                AddressTextBox.Text = Global.Server[Index].Address;
                PortTextBox.Text = Global.Server[Index].Port.ToString();
                PasswordTextBox.Text = Global.Server[Index].Password;
                EncryptMethodComboBox.SelectedIndex = Global.EncryptMethods.SSR.IndexOf(Global.Server[Index].EncryptMethod);
                ProtocolComboBox.SelectedIndex = Global.Protocols.IndexOf(Global.Server[Index].Protocol);
                ProtocolParamTextBox.Text = Global.Server[Index].ProtocolParam;
                OBFSComboBox.SelectedIndex = Global.OBFSs.IndexOf(Global.Server[Index].OBFS);
                OBFSParamTextBox.Text = Global.Server[Index].OBFSParam;
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
            if (Index == -1)
            {
                Global.Server.Add(new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Type = "ShadowsocksR",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    Protocol = ProtocolComboBox.Text,
                    ProtocolParam = ProtocolParamTextBox.Text,
                    OBFS = OBFSComboBox.Text,
                    OBFSParam = OBFSParamTextBox.Text
                });
            }
            else
            {
                Global.Server[Index] = new Objects.Server()
                {
                    Remark = RemarkTextBox.Text,
                    Group = Global.Server[Index].Group,
                    Type = "ShadowsocksR",
                    Address = AddressTextBox.Text,
                    Port = int.Parse(PortTextBox.Text),
                    Password = PasswordTextBox.Text,
                    EncryptMethod = EncryptMethodComboBox.Text,
                    Protocol = ProtocolComboBox.Text,
                    ProtocolParam = ProtocolParamTextBox.Text,
                    OBFS = OBFSComboBox.Text,
                    OBFSParam = OBFSParamTextBox.Text
                };
            }

            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Global.MainForm.InitServer();
            Close();
        }
    }
}

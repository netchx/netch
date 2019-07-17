using System;
using System.Net;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class GlobalBypassIPForm : Form
    {
        public GlobalBypassIPForm()
        {
            InitializeComponent();
        }

        private void GlobalBypassIPForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate("Global Bypass IPs");
            AddButton.Text = Utils.i18N.Translate("Add");
            DeleteButton.Text = Utils.i18N.Translate("Delete");
            ControlButton.Text = Utils.i18N.Translate("Save");

            IPListBox.Items.AddRange(Global.BypassIPs.ToArray());

            for (var i = 32; i >= 1; i--)
            {
                PrefixComboBox.Items.Add(i);
            }
            PrefixComboBox.SelectedIndex = 0;
        }

        private void GlobalBypassIPForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.SettingForm.Show();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(IPTextBox.Text))
            {
                if (IPAddress.TryParse(IPTextBox.Text, out var address))
                {
                    IPListBox.Items.Add(String.Format("{0}/{1}", address, PrefixComboBox.SelectedItem));
                }
                else
                {
                    MessageBox.Show(Utils.i18N.Translate("Please enter a correct IP address"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please enter an IP"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (IPListBox.SelectedIndex != -1)
            {
                IPListBox.Items.RemoveAt(IPListBox.SelectedIndex);
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select an IP"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            Global.BypassIPs.Clear();
            foreach (var ip in IPListBox.Items)
            {
                Global.BypassIPs.Add(ip as String);
            }

            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

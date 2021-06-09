using System;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Netch.Models;
using Netch.Properties;
using Netch.Services;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class GlobalBypassIPForm : Form
    {
        private readonly Setting _setting;

        public GlobalBypassIPForm(Setting setting)
        {
            _setting = setting;

            InitializeComponent();
            Icon = Resources.icon;
        }

        private void GlobalBypassIPForm_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);

            IPListBox.Items.AddRange(_setting.TUNTAP.BypassIPs.Cast<object>().ToArray());

            for (var i = 32; i >= 1; i--)
                PrefixComboBox.Items.Add(i);

            PrefixComboBox.SelectedIndex = 0;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(IPTextBox.Text))
            {
                if (IPAddress.TryParse(IPTextBox.Text, out var address))
                    IPListBox.Items.Add($"{address}/{PrefixComboBox.SelectedItem}");
                else
                    MessageBoxX.Show(i18N.Translate("Please enter a correct IP address"));
            }
            else
            {
                MessageBoxX.Show(i18N.Translate("Please enter an IP"));
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (IPListBox.SelectedIndex != -1)
                IPListBox.Items.RemoveAt(IPListBox.SelectedIndex);
            else
                MessageBoxX.Show(i18N.Translate("Please select an IP"));
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            _setting.TUNTAP.BypassIPs.Clear();
            foreach (var ip in IPListBox.Items)
                _setting.TUNTAP.BypassIPs.Add((string)ip);

            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate("About");
            GroupLinkLabel.Text = Utils.i18N.Translate("Group");
            ChannelLabel.Text = Utils.i18N.Translate("Channel");
            SponsorGroupBox.Text = Utils.i18N.Translate("Sponsor");
        }

        private void AboutForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void NetchPictureBox_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/NetchX/Netch");
        }

        private void GroupLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://t.me/NetchX");
        }

        private void ChannelLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://t.me/NetchXChannel");
        }

        private void SponsorPictureBox_Click(object sender, EventArgs e)
        {
            Process.Start("https://n3ro.io/register?ref=530");
        }
    }
}

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
            Text = Utils.i18N.Translate(Text);
            ChannelLabel.Text = Utils.i18N.Translate(ChannelLabel.Text);
            SponsorGroupBox.Text = Utils.i18N.Translate(SponsorGroupBox.Text);
        }

        private void AboutForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void NetchPictureBox_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/NetchX/Netch");
        }

        private void ChannelLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://t.me/Netch");
        }

        private void SponsorPictureBox_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.mansora.co");
        }
    }
}

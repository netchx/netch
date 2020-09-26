using System;
using System.Diagnostics;
using System.Windows.Forms;
using Netch.Utils;

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
            i18N.TranslateForm(this);
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

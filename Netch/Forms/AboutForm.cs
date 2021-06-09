using System;
using System.Windows.Forms;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            Icon = Resources.icon;
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);
        }

        private void NetchPictureBox_Click(object sender, EventArgs e)
        {
            Misc.Open("https://github.com/NetchX/Netch");
        }

        private void ChannelLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Misc.Open("https://t.me/Netch");
        }

        private void SponsorPictureBox_Click(object sender, EventArgs e)
        {
            Misc.Open("https://www.mansora.co");
        }
    }
}
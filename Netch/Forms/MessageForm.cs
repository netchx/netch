using System;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class MessageForm : Form
    {
        public MessageForm()
        {
            InitializeComponent();
        }

        private void ButtonYes_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MessageForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate("MessageForm");
            MessageLabel.Text = Utils.i18N.Translate("Not available for current version. More details on ");
        }

        private void MessageForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void MessageLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(MessageLinkLabel.Text);
        }
    }
}

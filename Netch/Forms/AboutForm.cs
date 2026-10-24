using Netch.Properties;
using Netch.Utils;

using Netch.Controllers;

namespace Netch.Forms;

[Fody.ConfigureAwait(true)]
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
        Utils.Utils.Open(UpdateChecker.ProjectUrl);
    }

    private void ChannelLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Utils.Utils.Open(UpdateChecker.ProjectUrl);
    }

    private void SponsorPictureBox_Click(object sender, EventArgs e)
    {
        Utils.Utils.Open("https://www.mansora.co");
    }
}

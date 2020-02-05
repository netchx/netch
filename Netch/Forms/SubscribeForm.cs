using Netch.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class SubscribeForm : Form
    {
        public SubscribeForm()
        {
            InitializeComponent();
        }

        public void InitSubscribeLink()
        {
            SubscribeLinkListView.Items.Clear();

            foreach (var item in Global.Settings.SubscribeLink)
            {
                if (!string.IsNullOrEmpty(item.UserAgent))
                {
                    SubscribeLinkListView.Items.Add(new ListViewItem(new[] {
                    item.Remark,
                    item.Link,
                    item.UserAgent}));
                }
                else
                {
                    SubscribeLinkListView.Items.Add(new ListViewItem(new[] {
                    item.Remark,
                    item.Link,
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36"}));
                }
            }
        }

        private void SubscribeForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate(Text);
            RemarkColumnHeader.Text = Utils.i18N.Translate(RemarkColumnHeader.Text);
            LinkColumnHeader.Text = Utils.i18N.Translate(LinkColumnHeader.Text);
            UseSelectedServerCheckBox.Text = Utils.i18N.Translate(UseSelectedServerCheckBox.Text);
            DeleteToolStripMenuItem.Text = Utils.i18N.Translate(DeleteToolStripMenuItem.Text);
            CopyLinkToolStripMenuItem.Text = Utils.i18N.Translate(CopyLinkToolStripMenuItem.Text);
            RemarkLabel.Text = Utils.i18N.Translate(RemarkLabel.Text);
            LinkLabel.Text = Utils.i18N.Translate(LinkLabel.Text);
            AddButton.Text = Utils.i18N.Translate(AddButton.Text);
            ControlButton.Text = Utils.i18N.Translate(ControlButton.Text);

            UserAgentTextBox.Text = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";
            UseSelectedServerCheckBox.Checked = Global.Settings.UseProxyToUpdateSubscription;

            InitSubscribeLink();
        }

        private void SubscribeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }
        private void CopyLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SubscribeLinkListView.SelectedItems.Count > 0)
            {
                for (var i = SubscribeLinkListView.SelectedItems.Count - 1; i >= 0; i--)
                {
                    var item = SubscribeLinkListView.SelectedItems[i];
                    var link = Global.Settings.SubscribeLink[item.Index];
                    Clipboard.SetText(link.Link);
                }
            }
        }
        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Utils.i18N.Translate("Delete or not ? Will clean up the corresponding group of items in the server list"), Utils.i18N.Translate("Information"), MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                if (SubscribeLinkListView.SelectedItems.Count > 0)
                {
                    for (var i = SubscribeLinkListView.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        var item = SubscribeLinkListView.SelectedItems[i];
                        var link = Global.Settings.SubscribeLink[item.Index];

                        var list = new List<Models.Server>();
                        foreach (var server in Global.Settings.Server)
                        {
                            if (server.Group != link.Remark)
                            {
                                list.Add(server);
                            }
                        }

                        Global.Settings.Server = list;
                        Global.Settings.SubscribeLink.RemoveAt(item.Index);
                        SubscribeLinkListView.Items.Remove(item);

                        Global.MainForm.InitServer();
                    }
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                if (!string.IsNullOrWhiteSpace(LinkTextBox.Text))
                {
                    if (LinkTextBox.Text.StartsWith("HTTP://", StringComparison.OrdinalIgnoreCase) || LinkTextBox.Text.StartsWith("HTTPS://", StringComparison.OrdinalIgnoreCase))
                    {
                        Global.Settings.SubscribeLink.Add(new Models.SubscribeLink
                        {
                            Remark = RemarkTextBox.Text,
                            Link = LinkTextBox.Text,
                            UserAgent = UserAgentTextBox.Text
                        });

                        RemarkTextBox.Text = string.Empty;
                        LinkTextBox.Text = string.Empty;
                        UserAgentTextBox.Text = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";

                        InitSubscribeLink();
                    }
                    else
                    {
                        MessageBox.Show(Utils.i18N.Translate("Links must start with http:// or https://"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(Utils.i18N.Translate("Link can not be empty"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Remark can not be empty"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            Utils.Configuration.Save();
            Global.Settings.UseProxyToUpdateSubscription = UseSelectedServerCheckBox.Checked;
            MessageBox.Show(Utils.i18N.Translate("Successfully saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
}

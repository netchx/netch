using System;
using System.Linq;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class SubscribeForm : Form
    {
        private int _editingIndex = -1;

        public SubscribeForm()
        {
            InitializeComponent();
            i18N.TranslateForm(this);
            i18N.TranslateForm(pContextMenuStrip);

            UseSelectedServerCheckBox.Enabled = Global.Settings.Server.Any();
            UseSelectedServerCheckBox.Checked = Global.Settings.Server.Any() && Global.Settings.UseProxyToUpdateSubscription;

            InitSubscribeLink();
            ResetEditingGroup();
        }

        public void InitSubscribeLink()
        {
            SubscribeLinkListView.Items.Clear();

            foreach (var item in Global.Settings.SubscribeLink)
            {
                var viewItem = new ListViewItem(new[]
                {
                    "",
                    item.Remark,
                    item.Link,
                    !string.IsNullOrEmpty(item.UserAgent) ? item.UserAgent : WebUtil.DefaultUserAgent
                });
                viewItem.Checked = item.Enable;
                SubscribeLinkListView.Items.Add(viewItem);
            }
        }

        private void SubscribeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Configuration.Save();
            Global.Settings.UseProxyToUpdateSubscription = UseSelectedServerCheckBox.Checked;
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
            if (MessageBoxX.Show(i18N.Translate("Delete or not ? Will clean up the corresponding group of items in the server list"), confirm: true) == DialogResult.OK)
            {
                if (SubscribeLinkListView.SelectedItems.Count > 0)
                {
                    for (var i = SubscribeLinkListView.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        var item = SubscribeLinkListView.SelectedItems[i];

                        DeleteServersInGroup(item.SubItems[0].Text);
                        Global.Settings.SubscribeLink.RemoveAt(item.Index);
                        SubscribeLinkListView.Items.Remove(item);
                        ResetEditingGroup();
                    }
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                MessageBoxX.Show(i18N.Translate("Remark can not be empty"));
                return;
            }

            if (string.IsNullOrWhiteSpace(LinkTextBox.Text))
            {
                MessageBoxX.Show(i18N.Translate("Link can not be empty"));
                return;
            }

            if (!LinkTextBox.Text.StartsWith("HTTP://", StringComparison.OrdinalIgnoreCase) && !LinkTextBox.Text.StartsWith("HTTPS://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxX.Show(i18N.Translate("Link must start with http:// or https://"));
                return;
            }

            if (_editingIndex == -1)
            {
                if (Global.Settings.SubscribeLink.Any(link => link.Remark.Equals(RemarkTextBox.Text)))
                {
                    MessageBoxX.Show("Remark Name Duplicate!");
                    return;
                }

                Global.Settings.SubscribeLink.Add(new SubscribeLink
                {
                    Enable = true,
                    Remark = RemarkTextBox.Text,
                    Link = LinkTextBox.Text,
                    UserAgent = UserAgentTextBox.Text
                });
            }
            else
            {
                var target = Global.Settings.SubscribeLink[_editingIndex];
                RenameServersGroup(target.Remark, RemarkTextBox.Text);

                var listViewItem = SubscribeLinkListView.Items[_editingIndex];

                target.Enable = listViewItem.Checked;
                target.Link = LinkTextBox.Text;
                target.Remark = RemarkTextBox.Text;
                target.UserAgent = UserAgentTextBox.Text;
            }

            MessageBoxX.Show(i18N.Translate("Saved"));
            Global.Settings.UseProxyToUpdateSubscription = UseSelectedServerCheckBox.Checked;
            Configuration.Save();

            InitSubscribeLink();
            ResetEditingGroup();
        }

        private static void DeleteServersInGroup(string group)
        {
            Global.Settings.Server.RemoveAll(server => server.Group == group);
        }

        private static void RenameServersGroup(string oldGroup, string newGroup)
        {
            foreach (var server in Global.Settings.Server)
            {
                if (server.Group == oldGroup)
                {
                    server.Group = newGroup;
                }
            }
        }

        /// <summary>
        /// 订阅列表选中节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubscribeLinkListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var listView = (ListView) sender;
            if (listView.SelectedItems.Count == 0)
            {
                // 重置
                ResetEditingGroup();
                return;
            }
            _editingIndex = listView.SelectedItems[0].Index;

            var target = SubscribeLinkListView.Items[_editingIndex];

            AddSubscriptionBox.Text = target.SubItems[1].Text;
            RemarkTextBox.Text = target.SubItems[1].Text;
            LinkTextBox.Text = target.SubItems[2].Text;
            UserAgentTextBox.Text = target.SubItems[3].Text;
        }

        private void SubscribeLinkListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (SubscribeLinkListView.SelectedItems.Count > 0)
                {
                    pContextMenuStrip.Show(SubscribeLinkListView, e.Location);
                }
            }
        }

        private void ResetEditingGroup()
        {
            _editingIndex = -1;
            AddSubscriptionBox.Text = string.Empty;
            RemarkTextBox.Text = string.Empty;
            LinkTextBox.Text = string.Empty;
            UserAgentTextBox.Text = WebUtil.DefaultUserAgent;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            ResetEditingGroup();
        }

        private void deleteServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SubscribeLinkListView.SelectedItems.Count > 0)
            {
                var item = SubscribeLinkListView.SelectedItems[0];

                if (MessageBoxX.Show(i18N.Translate("Confirm deletion?"), confirm: true) != DialogResult.OK)
                    return;
                DeleteServersInGroup(item.SubItems[1].Text);
            }
        }
    }
}
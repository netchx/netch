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

        #region EventHandler

        /// <summary>
        /// 订阅列表选中节点
        /// </summary>
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

        private void SubscribeLinkListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            var viewItem = SubscribeLinkListView.Items[e.Item.Index];
            var subscribeLink = Global.Settings.SubscribeLink[e.Item.Index];
            subscribeLink.Enable = viewItem.Checked;
        }


        private void SubscribeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.Settings.UseProxyToUpdateSubscription = UseSelectedServerCheckBox.Checked;
            Configuration.Save();
        }

        #endregion

        #region EditBox

        private void ClearButton_Click(object sender, EventArgs e)
        {
            ResetEditingGroup();
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
                var subscribeLink = Global.Settings.SubscribeLink[_editingIndex];

                RenameServersGroup(subscribeLink.Remark, RemarkTextBox.Text);
                subscribeLink.Link = LinkTextBox.Text;
                subscribeLink.Remark = RemarkTextBox.Text;
                subscribeLink.UserAgent = UserAgentTextBox.Text;
            }

            MessageBoxX.Show(i18N.Translate("Saved"));

            InitSubscribeLink();
            ResetEditingGroup();
        }

        #endregion

        #region ContextMenu

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBoxX.Show(i18N.Translate("Delete or not ? Will clean up the corresponding group of items in the server list"), confirm: true) != DialogResult.OK)
                return;

            var viewItem = SubscribeLinkListView.SelectedItems[0];
            var subscribeLink = Global.Settings.SubscribeLink[viewItem.Index];

            DeleteServersInGroup(subscribeLink.Remark);
            Global.Settings.SubscribeLink.Remove(subscribeLink);
            SubscribeLinkListView.Items.Remove(viewItem);
            ResetEditingGroup();
        }

        private void deleteServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBoxX.Show(i18N.Translate("Confirm deletion?"), confirm: true) != DialogResult.OK)
                return;

            var viewItem = SubscribeLinkListView.SelectedItems[0];
            DeleteServersInGroup(viewItem.SubItems[1].Text);
        }

        private void CopyLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var viewItem = SubscribeLinkListView.SelectedItems[0];
            var subscribeLink = Global.Settings.SubscribeLink[viewItem.Index];
            Clipboard.SetText(subscribeLink.Link);
        }

        #endregion

        #region Helper

        private static void DeleteServersInGroup(string group)
        {
            Global.Settings.Server.RemoveAll(server => server.Group == group);
        }

        private static void RenameServersGroup(string oldGroup, string newGroup)
        {
            foreach (var server in Global.Settings.Server.Where(server => server.Group == oldGroup))
            {
                server.Group = newGroup;
            }
        }

        private void InitSubscribeLink()
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
                })
                {
                    Checked = item.Enable
                };
                SubscribeLinkListView.Items.Add(viewItem);
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

        #endregion
    }
}
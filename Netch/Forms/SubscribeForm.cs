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
        }

        public void InitSubscribeLink()
        {
            SubscribeLinkListView.Items.Clear();

            foreach (var item in Global.Settings.SubscribeLink)
            {
                SubscribeLinkListView.Items.Add(new ListViewItem(new[]
                {
                    item.Remark,
                    item.Link,
                    !string.IsNullOrEmpty(item.UserAgent) ? item.UserAgent : WebUtil.DefaultUserAgent
                }));
            }
        }

        private void SubscribeForm_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);
            i18N.TranslateForm(pContextMenuStrip);

            ResetEditingGroup();

            if (Global.Settings.Server.Count > 0)
            {
                UseSelectedServerCheckBox.Enabled = true;
                UseSelectedServerCheckBox.Checked = Global.Settings.UseProxyToUpdateSubscription;
            }
            else
            {
                UseSelectedServerCheckBox.Checked = false;
                UseSelectedServerCheckBox.Enabled = false;
            }

            InitSubscribeLink();
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
                    Remark = RemarkTextBox.Text,
                    Link = LinkTextBox.Text,
                    UserAgent = UserAgentTextBox.Text
                });
            }
            else
            {
                var target = Global.Settings.SubscribeLink[_editingIndex];
                if (MessageBox.Show(i18N.Translate("Delete the corresponding group of items in the server list?"), i18N.Translate("Confirm"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DeleteServersInGroup(target.Remark);
                }
                else
                {
                    RenameServersGroup(target.Remark, RemarkTextBox.Text);
                }

                target.Link = LinkTextBox.Text;
                target.Remark = RemarkTextBox.Text;
                target.UserAgent = UserAgentTextBox.Text;
            }

            Configuration.Save();
            Global.Settings.UseProxyToUpdateSubscription = UseSelectedServerCheckBox.Checked;
            // MessageBoxX.Show(i18N.Translate("Saved"));

            ResetEditingGroup();

            InitSubscribeLink();
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
            var editingCanOverwrite = true;
            if (_editingIndex != -1)
            {
                var targetItem = SubscribeLinkListView.Items[_editingIndex].SubItems;
                editingCanOverwrite = RemarkTextBox.Text == targetItem[0].Text &&
                                      LinkTextBox.Text == targetItem[1].Text &&
                                      UserAgentTextBox.Text == targetItem[2].Text;
            }

            if (SubscribeLinkListView.SelectedItems.Count == 1)
            {
                if (editingCanOverwrite)
                {
                    SelectEditing(SubscribeLinkListView.SelectedItems[0].Index);
                }
            }
            else if (SubscribeLinkListView.SelectedItems.Count > 1)
            {
            }
            else if (editingCanOverwrite)
            {
                // 不选
                // 重置
                ResetEditingGroup();
            }
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

        private void SelectEditing(int index)
        {
            _editingIndex = index;
            ListViewItem target;
            target = SubscribeLinkListView.Items[index];
            AddSubscriptionBox.Text = target.SubItems[0].Text;
            RemarkTextBox.Text = target.SubItems[0].Text;
            LinkTextBox.Text = target.SubItems[1].Text;
            UserAgentTextBox.Text = target.SubItems[2].Text;
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
    }
}
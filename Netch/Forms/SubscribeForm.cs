using System;
using System.Linq;
using System.Windows.Forms;
using Netch.Models;
using Netch.Properties;
using Netch.Services;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class SubscribeForm : Form
    {
        private readonly Setting _setting;
        private readonly Configuration _configuration;

        public SubscribeForm(Setting setting, Configuration configuration)
        {
            _setting = setting;
            _configuration = configuration;

            InitializeComponent();
            Icon = Resources.icon;

            i18N.TranslateForm(this);
            i18N.TranslateForm(pContextMenuStrip);

            InitSubscribeLink();
        }

        private int SelectedIndex
        {
            get
            {
                if (SubscribeLinkListView.MultiSelect)
                    throw new Exception();

                return SubscribeLinkListView.SelectedIndices.Count == 0 ? -1 : SubscribeLinkListView.SelectedIndices[0];
            }
        }

        #region EventHandler

        private void SubscribeLinkListView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                if (SelectedIndex != -1)
                    pContextMenuStrip.Show(SubscribeLinkListView, e.Location);
        }

        /// <summary>
        ///     选中/取消选中
        /// </summary>
        private void SubscribeLinkListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetEditingGroup(SelectedIndex);
        }

        /// <summary>
        ///     订阅启/禁用
        /// </summary>
        private void SubscribeLinkListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            var index = e.Item.Index;
            _setting.SubscribeLink[index].Enable = SubscribeLinkListView.Items[index].Checked;
        }

        private async void SubscribeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            await _configuration.SaveAsync();
        }

        #endregion

        #region EditBox

        private void UnselectButton_Click(object sender, EventArgs e)
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

            if (!LinkTextBox.Text.StartsWith("HTTP://", StringComparison.OrdinalIgnoreCase) &&
                !LinkTextBox.Text.StartsWith("HTTPS://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxX.Show(i18N.Translate("Link must start with http:// or https://"));
                return;
            }

            if (SelectedIndex == -1)
            {
                if (_setting.SubscribeLink.Any(link => link.Remark.Equals(RemarkTextBox.Text)))
                {
                    MessageBoxX.Show("Remark Name Duplicate!");
                    return;
                }

                _setting.SubscribeLink.Add(new SubscribeLink
                {
                    Enable = true,
                    Remark = RemarkTextBox.Text,
                    Link = LinkTextBox.Text,
                    UserAgent = UserAgentTextBox.Text
                });
            }
            else
            {
                var subscribeLink = _setting.SubscribeLink[SelectedIndex];

                RenameServers(subscribeLink.Remark, RemarkTextBox.Text);
                subscribeLink.Link = LinkTextBox.Text;
                subscribeLink.Remark = RemarkTextBox.Text;
                subscribeLink.UserAgent = UserAgentTextBox.Text;
            }

            InitSubscribeLink();
        }

        #endregion

        #region ContextMenu

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBoxX.Show(i18N.Translate("Delete or not ? Will clean up the corresponding group of items in the server list"),
                confirm: true) != DialogResult.OK)
                return;

            var subscribeLink = _setting.SubscribeLink[SelectedIndex];
            DeleteServers(subscribeLink.Remark);
            _setting.SubscribeLink.Remove(subscribeLink);

            InitSubscribeLink();
        }

        private void deleteServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBoxX.Show(i18N.Translate("Confirm deletion?"), confirm: true) != DialogResult.OK)
                return;

            DeleteServers(_setting.SubscribeLink[SelectedIndex].Remark);
        }

        private void CopyLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(_setting.SubscribeLink[SelectedIndex].Link);
        }

        #endregion

        #region Helper

        private void DeleteServers(string group)
        {
            _setting.Server.RemoveAll(server => server.Group == group);
        }

        private void RenameServers(string oldGroup, string newGroup)
        {
            foreach (var server in _setting.Server.Where(server => server.Group == oldGroup))
                server.Group = newGroup;
        }

        private void InitSubscribeLink()
        {
            SubscribeLinkListView.Items.Clear();

            foreach (var item in _setting.SubscribeLink)
                SubscribeLinkListView.Items.Add(new ListViewItem(new[]
                {
                    "",
                    item.Remark,
                    item.Link,
                    !string.IsNullOrEmpty(item.UserAgent) ? item.UserAgent : WebUtil.DefaultUserAgent
                })
                {
                    Checked = item.Enable
                });

            ResetEditingGroup();
        }

        private void ResetEditingGroup()
        {
            AddSubscriptionBox.Text = string.Empty;
            RemarkTextBox.Text = string.Empty;
            LinkTextBox.Text = string.Empty;
            UserAgentTextBox.Text = WebUtil.DefaultUserAgent;
        }

        private void SetEditingGroup(int index)
        {
            if (index == -1)
            {
                ResetEditingGroup();
                AddButton.Text = i18N.Translate("Add");
                return;
            }

            var item = _setting.SubscribeLink[index];
            AddSubscriptionBox.Text = item.Remark;
            RemarkTextBox.Text = item.Remark;
            LinkTextBox.Text = item.Link;
            UserAgentTextBox.Text = item.UserAgent;

            AddButton.Text = i18N.Translate("Modify");
        }

        #endregion
    }
}
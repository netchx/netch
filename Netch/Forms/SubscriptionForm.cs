using Netch.Models;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Forms;

[Fody.ConfigureAwait(true)]
public partial class SubscriptionForm : Form
{
    public SubscriptionForm()
    {
        InitializeComponent();
        Icon = Resources.icon;

        i18N.TranslateForm(this);
        i18N.TranslateForm(pContextMenuStrip);

        LoadSubscriptionLinks();
    }

    private int SelectedIndex
    {
        get
        {
            if (SubscriptionLinkListView.MultiSelect)
                throw new Exception();

            return SubscriptionLinkListView.SelectedIndices.Count == 0 ? -1 : SubscriptionLinkListView.SelectedIndices[0];
        }
    }

    #region EventHandler

    private void SubscriptionLinkListView_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
            if (SelectedIndex != -1)
                pContextMenuStrip.Show(SubscriptionLinkListView, e.Location);
    }

    /// <summary>
    ///     选中/取消选中
    /// </summary>
    private void SubscriptionLinkListView_SelectedIndexChanged(object sender, EventArgs e)
    {
        SetEditingGroup(SelectedIndex);
    }

    /// <summary>
    ///     订阅启/禁用
    /// </summary>
    private void SubscriptionLinkListView_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
        var index = e.Item.Index;
        Global.Settings.Subscription[index].Enable = SubscriptionLinkListView.Items[index].Checked;
    }

    private async void SubscriptionForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        await Configuration.SaveAsync();
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
            if (Global.Settings.Subscription.Any(link => link.Remark.Equals(RemarkTextBox.Text)))
            {
                MessageBoxX.Show(i18N.Translate("Subscription with the specified remark already exists"));
                return;
            }

            Global.Settings.Subscription.Add(new Subscription
            {
                Enable = true,
                Remark = RemarkTextBox.Text,
                Link = LinkTextBox.Text,
                UserAgent = UserAgentTextBox.Text
            });
        }
        else
        {
            var subscribeLink = Global.Settings.Subscription[SelectedIndex];

            RenameServers(subscribeLink.Remark, RemarkTextBox.Text);
            subscribeLink.Link = LinkTextBox.Text;
            subscribeLink.Remark = RemarkTextBox.Text;
            subscribeLink.UserAgent = UserAgentTextBox.Text;
        }

        LoadSubscriptionLinks();
    }

    #endregion

    #region ContextMenu

    private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (MessageBoxX.Show(i18N.Translate("Delete or not ? Will clean up the corresponding group of items in the server list"),
                confirm: true) != DialogResult.OK)
            return;

        var subscribeLink = Global.Settings.Subscription[SelectedIndex];
        DeleteServers(subscribeLink.Remark);
        Global.Settings.Subscription.Remove(subscribeLink);

        LoadSubscriptionLinks();
    }

    private void DeleteServersToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (MessageBoxX.Show(i18N.Translate("Confirm deletion?"), confirm: true) != DialogResult.OK)
            return;

        DeleteServers(Global.Settings.Subscription[SelectedIndex].Remark);
    }

    private void CopyLinkToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(Global.Settings.Subscription[SelectedIndex].Link);
    }

    #endregion

    #region Helper

    private static void DeleteServers(string group)
    {
        Global.Settings.Server.RemoveAll(server => server.Group == group);
    }

    private static void RenameServers(string oldGroup, string newGroup)
    {
        foreach (var server in Global.Settings.Server.Where(server => server.Group == oldGroup))
            server.Group = newGroup;
    }

    private void LoadSubscriptionLinks()
    {
        SubscriptionLinkListView.Items.Clear();

        foreach (var item in Global.Settings.Subscription)
            SubscriptionLinkListView.Items.Add(new ListViewItem(new[]
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

        var item = Global.Settings.Subscription[index];
        AddSubscriptionBox.Text = item.Remark;
        RemarkTextBox.Text = item.Remark;
        LinkTextBox.Text = item.Link;
        UserAgentTextBox.Text = item.UserAgent;

        AddButton.Text = i18N.Translate("Modify");
    }

    #endregion
}
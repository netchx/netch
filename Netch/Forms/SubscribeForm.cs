using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;

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
            Text = i18N.Translate(Text);
            RemarkColumnHeader.Text = i18N.Translate(RemarkColumnHeader.Text);
            LinkColumnHeader.Text = i18N.Translate(LinkColumnHeader.Text);
            UseSelectedServerCheckBox.Text = i18N.Translate(UseSelectedServerCheckBox.Text);
            DeleteToolStripMenuItem.Text = i18N.Translate(DeleteToolStripMenuItem.Text);
            CopyLinkToolStripMenuItem.Text = i18N.Translate(CopyLinkToolStripMenuItem.Text);
            RemarkLabel.Text = i18N.Translate(RemarkLabel.Text);
            LinkLabel.Text = i18N.Translate(LinkLabel.Text);
            AddButton.Text = i18N.Translate(AddButton.Text);
            ControlButton.Text = i18N.Translate(ControlButton.Text);

            UserAgentTextBox.Text = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";

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
            if (MessageBoxX.Show(i18N.Translate("Delete or not ? Will clean up the corresponding group of items in the server list"), confirm: true) == DialogResult.OK)
            {
                if (SubscribeLinkListView.SelectedItems.Count > 0)
                {
                    DeleteSubscribe();
                }
            }
        }
        public void DeleteSubscribe()
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

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                if (!string.IsNullOrWhiteSpace(LinkTextBox.Text))
                {
                    if (LinkTextBox.Text.StartsWith("HTTP://", StringComparison.OrdinalIgnoreCase) || LinkTextBox.Text.StartsWith("HTTPS://", StringComparison.OrdinalIgnoreCase))
                    {
                        //是否为新增订阅
                        var saveFlag = true;
                        Global.Settings.SubscribeLink.ForEach(subitem =>
                        {
                            if (subitem.Link.Equals(LinkTextBox.Text))
                            {
                                if (!subitem.Remark.Equals(RemarkTextBox.Text))
                                {
                                    //修改了订阅备注，修改旧订阅服务器
                                    Global.Settings.Server.ForEach(serverItem =>
                                    {
                                        try
                                        {
                                            //当前服务器组群组为订阅群组时批量修改备注
                                            if (serverItem.Group == subitem.Remark) {

                                                //serverItem.Group OldGroupRemark
                                                //RemarkTextBox.Text NewGroupRemark
                                                serverItem.Group = RemarkTextBox.Text;
                                            }
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    });

                                    subitem.Remark = RemarkTextBox.Text;
                                    Global.MainForm.InitServer();
                                }

                                subitem.UserAgent = UserAgentTextBox.Text;
                                saveFlag = false;

                                Configuration.Save();
                                Global.Settings.UseProxyToUpdateSubscription = UseSelectedServerCheckBox.Checked;
                                MessageBoxX.Show(i18N.Translate("Successfully saved"));
                            }
                        });
                        if (saveFlag)
                        {
                            Global.Settings.SubscribeLink.Add(new SubscribeLink
                            {
                                Remark = RemarkTextBox.Text,
                                Link = LinkTextBox.Text,
                                UserAgent = UserAgentTextBox.Text
                            });
                        }

                        RemarkTextBox.Text = string.Empty;
                        LinkTextBox.Text = string.Empty;
                        UserAgentTextBox.Text = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";

                        InitSubscribeLink();
                    }
                    else
                    {
                        MessageBoxX.Show(i18N.Translate("Links must start with http:// or https://"));
                    }
                }
                else
                {
                    MessageBoxX.Show(i18N.Translate("Link can not be empty"));
                }
            }
            else
            {
                MessageBoxX.Show(i18N.Translate("Remark can not be empty"));
            }
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            Configuration.Save();
            Global.Settings.UseProxyToUpdateSubscription = UseSelectedServerCheckBox.Checked;
            MessageBoxX.Show(i18N.Translate("Successfully saved"));
            Close();
        }
        /// <summary>
        /// 订阅列表选中节点
        /// TODO 选中节点编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubscribeLinkListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SubscribeLinkListView.SelectedItems.Count > 0)
            {
                RemarkTextBox.Text = SubscribeLinkListView.SelectedItems[0].SubItems[0].Text;
                LinkTextBox.Text = SubscribeLinkListView.SelectedItems[0].SubItems[1].Text;
                UserAgentTextBox.Text = SubscribeLinkListView.SelectedItems[0].SubItems[2].Text;
            }
        }
    }
}

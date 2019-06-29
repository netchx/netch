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

            foreach (var link in Global.SubscribeLink)
            {
                SubscribeLinkListView.Items.Add(new ListViewItem(new String[] { link.Remark, link.Link }));
            }
        }

        private void SubscribeForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate("Subscribe");
            RemarkColumnHeader.Text = Utils.i18N.Translate("Remark");
            LinkColumnHeader.Text = Utils.i18N.Translate("Link");
            DeleteToolStripMenuItem.Text = Utils.i18N.Translate("Delete");
            RemarkLabel.Text = Utils.i18N.Translate("Remark");
            LinkLabel.Text = Utils.i18N.Translate("Link");
            AddButton.Text = Utils.i18N.Translate("Add");
            ControlButton.Text = Utils.i18N.Translate("Save");

            InitSubscribeLink();
        }

        private void SubscribeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Utils.i18N.Translate("Delete or not ? Will clean up the corresponding group of items in the server list"), Utils.i18N.Translate("Information"), MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                if (SubscribeLinkListView.SelectedItems.Count > 0)
                {
                    for (int i = SubscribeLinkListView.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        var item = SubscribeLinkListView.SelectedItems[i];
                        var link = Global.SubscribeLink[item.Index];

                        var list = new List<Objects.Server>();
                        foreach (var server in Global.Server)
                        {
                            if (server.Group != link.Remark)
                            {
                                list.Add(server);
                            }
                        }

                        Global.Server = list;
                        Global.SubscribeLink.RemoveAt(item.Index);
                        SubscribeLinkListView.Items.Remove(item);

                        Global.MainForm.InitServer();
                    }
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                if (!String.IsNullOrWhiteSpace(LinkTextBox.Text))
                {
                    if (LinkTextBox.Text.StartsWith("HTTP://", StringComparison.OrdinalIgnoreCase) || LinkTextBox.Text.StartsWith("HTTPS://", StringComparison.OrdinalIgnoreCase))
                    {
                        Global.SubscribeLink.Add(new Objects.SubscribeLink()
                        {
                            Remark = RemarkTextBox.Text,
                            Link = LinkTextBox.Text
                        });

                        RemarkTextBox.Text = String.Empty;
                        LinkTextBox.Text = String.Empty;

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
            MessageBox.Show(Utils.i18N.Translate("Successfully saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
}

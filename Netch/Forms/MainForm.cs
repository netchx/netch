using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class MainForm : Form
    {
        /// <summary>
        ///     当前状态
        /// </summary>
        public Objects.State State = Objects.State.Waiting;

        /// <summary>
        ///     主控制器
        /// </summary>
        public Controllers.MainController MainController;

        public MainForm()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
        }

        public void InitServer()
        {
            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Server.ToArray());

            if (ServerComboBox.Items.Count > 0)
            {
                ServerComboBox.SelectedIndex = 0;
            }
        }

        public void InitMode()
        {
            ModeComboBox.Items.Clear();

            if (Directory.Exists("mode"))
            {
                var list = new List<Objects.Mode>();

                foreach (var name in Directory.GetFiles("mode", "*.txt"))
                {
                    var mode = new Objects.Mode();

                    using (var sr = new StringReader(File.ReadAllText(name)))
                    {
                        var i = 0;
                        string text;

                        while ((text = sr.ReadLine()) != null)
                        {
                            if (i == 0)
                            {
                                mode.Remark = text.Substring(1).Trim();
                            }
                            else
                            {
                                if (!text.StartsWith("#") && !String.IsNullOrWhiteSpace(text))
                                {
                                    mode.Rule.Add(text.Trim());
                                }
                            }

                            i++;
                        }
                    }

                    list.Add(mode);
                }

                var array = list.ToArray();
                Array.Sort(array, (a, b) => String.Compare(a.Remark, b.Remark, StringComparison.Ordinal));

                ModeComboBox.Items.AddRange(array);
            }

            if (ModeComboBox.Items.Count > 0)
            {
                ModeComboBox.SelectedIndex = 0;
            }
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            var cbx = sender as ComboBox;

            // 绘制背景颜色
            e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

            if (e.Index >= 0)
            {
                // 绘制 备注/名称 字符串
                e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, new SolidBrush(Color.Black), e.Bounds);

                if (cbx.Items[e.Index] is Objects.Server)
                {
                    var item = cbx.Items[e.Index] as Objects.Server;

                    // 计算延迟底色
                    SolidBrush brush;
                    if (item.Delay == 999)
                    {
                        // 灰色
                        brush = new SolidBrush(Color.Gray);
                    }
                    else if (item.Delay > 200)
                    {
                        // 红色
                        brush = new SolidBrush(Color.Red);
                    }
                    else if (item.Delay > 80)
                    {
                        // 黄色
                        brush = new SolidBrush(Color.Yellow);
                    }
                    else
                    {
                        // 绿色
                        brush = new SolidBrush(Color.FromArgb(50, 255, 56));
                    }

                    // 绘制延迟底色
                    e.Graphics.FillRectangle(brush, ServerComboBox.Size.Width - 60, e.Bounds.Y, 60, e.Bounds.Height);

                    // 绘制延迟字符串
                    e.Graphics.DrawString(item.Delay.ToString(), cbx.Font, new SolidBrush(Color.Black), ServerComboBox.Size.Width - 58, e.Bounds.Y);
                }
                else if (cbx.Items[e.Index] is Objects.Mode)
                {
                    var item = cbx.Items[e.Index] as Objects.Mode;

                    // 绘制延迟底色
                    e.Graphics.FillRectangle(new SolidBrush(Color.Gray), ServerComboBox.Size.Width - 60, e.Bounds.Y, 60, e.Bounds.Height);

                    // 绘制延迟字符串
                    e.Graphics.DrawString(item.Rule.Count.ToString(), cbx.Font, new SolidBrush(Color.Black), ServerComboBox.Size.Width - 58, e.Bounds.Y);
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 加载配置
            Utils.Configuration.Load();

            // 加载服务器
            InitServer();

            // 加载模式
            InitMode();

            // 加载翻译
            ServerToolStripDropDownButton.Text = Utils.i18N.Translate("Server");
            AddSocks5ServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [Socks5] Server");
            AddShadowsocksServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [Shadowsocks] Server");
            AddShadowsocksRServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [ShadowsocksR] Server");
            AddVMessServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [VMess] Server");
            ImportServersFromClipboardToolStripMenuItem.Text = Utils.i18N.Translate("Import Servers From Clipboard");
            SubscribeToolStripDropDownButton.Text = Utils.i18N.Translate("Subscribe");
            ManageSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate("Manage Subscribe Links");
            UpdateServersFromSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate("Update Servers From Subscribe Links");
            FastCreateModeToolStripButton.Text = Utils.i18N.Translate("Fast Create Mode");
            AboutToolStripDropDownButton.Text = Utils.i18N.Translate("About");
            TelegarmGroupToolStripMenuItem.Text = Utils.i18N.Translate("Telegram Group");
            TelegramChannelToolStripMenuItem.Text = Utils.i18N.Translate("Telegram Channel");
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            ServerLabel.Text = Utils.i18N.Translate("Server");
            ModeLabel.Text = Utils.i18N.Translate("Mode");
            ControlButton.Text = Utils.i18N.Translate("Start");
            StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Waiting for command")}";

            // 自动检测延迟
            Task.Run(() =>
            {
                while (true)
                {
                    foreach (var server in Global.Server)
                    {
                        Task.Run(server.Test);
                    }

                    Thread.Sleep(10000);
                }
            });
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (State != Objects.State.Waiting && State != Objects.State.Stopped)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please press Stop button first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    e.Cancel = true;
                }

                Utils.Configuration.Save();
            }
        }

        private void AddSocks5ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Server.Socks5()).Show();
            Hide();
        }

        private void AddShadowsocksServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Server.Shadowsocks()).Show();
            Hide();
        }

        private void AddShadowsocksRServerToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void AddVMessServerToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ImportServersFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var texts = Clipboard.GetText();
            if (!String.IsNullOrWhiteSpace(texts))
            {
                using (var sr = new StringReader(texts))
                {
                    string text;

                    while ((text = sr.ReadLine()) != null)
                    {
                        var result = Utils.ShareLink.Parse(text);

                        if (result != null)
                        {
                            Global.Server.Add(result);
                        }
                    }
                }

                InitServer();
            }
        }

        private void ManageSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new SubscribeForm()).Show();
            Hide();
        }

        private void UpdateServersFromSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Global.SubscribeLink.Count > 0)
            {
                DeletePictureBox.Enabled = false;
                Task.Run(() =>
                {
                    foreach (var item in Global.SubscribeLink)
                    {
                        using (var client = new Override.WebClient())
                        {
                            try
                            {
                                var response = client.DownloadString(item.Link);

                                try
                                {
                                    response = Utils.ShareLink.URLSafeBase64Decode(response);
                                }
                                catch (Exception)
                                {
                                    // 跳过
                                }

                                var list = new List<Objects.Server>();
                                foreach (var server in Global.Server)
                                {
                                    if (server.Group != item.Remark)
                                    {
                                        list.Add(server);
                                    }
                                }
                                Global.Server = list;

                                using (var sr = new StringReader(response))
                                {
                                    string text;

                                    while ((text = sr.ReadLine()) != null)
                                    {
                                        var result = Utils.ShareLink.Parse(text);

                                        if (result != null)
                                        {
                                            if (item.Link.Contains("n3ro"))
                                            {
                                                result.Remark = result.Remark.Split('#')[0].Trim();
                                            }

                                            result.Group = item.Remark;

                                            Global.Server.Add(result);
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }

                    InitServer();
                    DeletePictureBox.Enabled = true;
                    MessageBox.Show(Utils.i18N.Translate("Update completed"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                });

                MessageBox.Show(Utils.i18N.Translate("Updating in the background"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("No subscription link"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FastCreateModeToolStripButton_Click(object sender, EventArgs e)
        {
            (new ModeForm()).Show();
            Hide();
        }

        private void TelegarmGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://t.me/NetchX");
        }

        private void TelegramChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://t.me/NetchXChannel");
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            TelegarmGroupToolStripMenuItem_Click(TelegarmGroupToolStripMenuItem, e);
        }

        private void EditPictureBox_Click(object sender, EventArgs e)
        {
            if (ServerComboBox.SelectedIndex != -1)
            {
                switch (Global.Server[ServerComboBox.SelectedIndex].Type)
                {
                    case "Socks5":
                        (new Server.Socks5(ServerComboBox.SelectedIndex)).Show();
                        break;
                    case "Shadowsocks":
                        (new Server.Shadowsocks(ServerComboBox.SelectedIndex)).Show();
                        break;
                    case "ShadowsocksR":
                        return;
                    case "VMess":
                        return;
                    default:
                        return;
                }

                Hide();
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select an server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeletePictureBox_Click(object sender, EventArgs e)
        {
            if (ServerComboBox.SelectedIndex != -1)
            {
                var index = ServerComboBox.SelectedIndex;

                Global.Server.Remove(ServerComboBox.SelectedItem as Objects.Server);
                ServerComboBox.Items.RemoveAt(index);

                if (ServerComboBox.Items.Count > 0)
                {
                    ServerComboBox.SelectedIndex = (index != 0) ? index - 1 : index;
                }
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select an server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                foreach (var server in Global.Server)
                {
                    Task.Run(server.Test);
                }

                Thread.Sleep(460);
                Refresh();
            });
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (State == Objects.State.Waiting || State == Objects.State.Stopped)
            {
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select an server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select an mode first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = false;
                ControlButton.Text = "...";
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting")}";
                State = Objects.State.Starting;

                Task.Run(() =>
                {
                    MainController = new Controllers.MainController();
                    if (MainController.Start(ServerComboBox.SelectedItem as Objects.Server, ModeComboBox.SelectedItem as Objects.Mode))
                    {
                        ControlButton.Enabled = true;
                        ControlButton.Text = Utils.i18N.Translate("Stop");
                        StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")}";
                        State = Objects.State.Started;
                    }
                    else
                    {
                        ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = true;
                        ControlButton.Text = Utils.i18N.Translate("Start");
                        StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Start failed")}";
                        State = Objects.State.Stopped;
                    }
                });
            }
            else
            {
                ControlButton.Enabled = false;
                ControlButton.Text = "...";
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Stopping")}";
                State = Objects.State.Stopping;

                Task.Run(() =>
                {
                    MainController.Stop();

                    ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = true;
                    ControlButton.Text = Utils.i18N.Translate("Start");
                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Stopped")}";
                    State = Objects.State.Stopped;
                });
            }
        }
    }
}

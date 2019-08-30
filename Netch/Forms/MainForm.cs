using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
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

        /// <summary>
        ///     上一次上传的流量
        /// </summary>
        public long LastUploadBandwidth = 0;

        /// <summary>
        ///     上一次下载的流量
        /// </summary>
        public long LastDownlaodBandwidth = 0;

        public MainForm()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
            //ToolStrip.Renderer = new Override.ToolStripProfessionalRender();
        }

        public void TestServer()
        {
            var list = new Task[Global.Server.Count];
            var count = 0;

            foreach (var server in Global.Server)
            {
                list[count++] = Task.Run(() => server.Test());
            }

            Task.WaitAll(list);
        }

        public void InitServer()
        {
            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Server.ToArray());

            // 查询设置中是否正常加载了上次存储的服务器位置
            if (Global.Settings.TryGetValue("ServerComboBoxSelectedIndex", out int count))
            {
                // 如果值合法，选中该位置
                if (count > 0 && count < ServerComboBox.Items.Count)
                {
                    ServerComboBox.SelectedIndex = count;
                }
                else if (ServerComboBox.Items.Count > 0) // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
                {
                    ServerComboBox.SelectedIndex = 0;
                }

                // 如果当前 ServerComboBox 中没元素，不做处理
            }
            else // 如果设置中没有加载上次的位置，给设置添加元素
            {
                Global.Settings.Add("ServerComboBoxSelectedIndex", 0);

                // 如果当前 ServerComboBox 中有元素，选择第一个位置
                if (ServerComboBox.Items.Count > 0)
                {
                    ServerComboBox.SelectedIndex = 0;
                }

                // 如果当前 ServerComboBox 中没元素，不做处理
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
                    var ok = true;
                    var mode = new Objects.Mode();

                    using (var sr = new StringReader(File.ReadAllText(name)))
                    {
                        var i = 0;
                        string text;

                        while ((text = sr.ReadLine()) != null)
                        {
                            if (i == 0)
                            {
                                var splited = text.Trim().Substring(1).Split(',');

                                if (splited.Length == 0)
                                {
                                    ok = false;
                                    break;
                                }

                                if (splited.Length >= 1)
                                {
                                    mode.Remark = splited[0].Trim();
                                }

                                if (splited.Length >= 2)
                                {
                                    if (int.TryParse(splited[1], out int result))
                                    {
                                        mode.Type = result;
                                    }
                                    else
                                    {
                                        ok = false;
                                        break;
                                    }
                                }

                                if (splited.Length >= 3)
                                {
                                    if (int.TryParse(splited[2], out int result))
                                    {
                                        mode.BypassChina = (result == 1);
                                    }
                                    else
                                    {
                                        ok = false;
                                        break;
                                    }
                                }
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

                    if (ok)
                    {
                        list.Add(mode);
                    }
                }

                var array = list.ToArray();
                Array.Sort(array, (a, b) => String.Compare(a.Remark, b.Remark, StringComparison.Ordinal));

                ModeComboBox.Items.AddRange(array);
            }

            // 查询设置中是否正常加载了上次存储的服务器位置
            if (Global.Settings.TryGetValue("ModeComboBoxSelectedIndex", out int count))
            {
                // 如果值合法，选中该位置
                if (count > 0 && count < ModeComboBox.Items.Count)
                {
                    ModeComboBox.SelectedIndex = count;
                }
                // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
                else if (ModeComboBox.Items.Count > 0)
                {
                    ModeComboBox.SelectedIndex = 0;
                }

                // 如果当前 ModeComboBox 中没元素，不做处理
            }
            else // 如果设置中没有加载上次的位置，给Settings添加元素
            {
                Global.Settings.Add("ModeComboBoxSelectedIndex", 0);

                // 如果当前 ModeComboBox 中有元素，选择第一个位置
                if (ModeComboBox.Items.Count > 0)
                {
                    ModeComboBox.SelectedIndex = 0;
                }

                // 如果当前 ModeComboBox 中没元素，不做处理
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
                    if (item.Delay > 200)
                    {
                        // 红色
                        brush = new SolidBrush(Color.Red);
                    }
                    else if (item.Delay > 80)
                    {
                        // 黄色
                        brush = new SolidBrush(Color.Yellow);
                    }
                    else if (item.Delay >= 0)
                    {
                        // 绿色
                        brush = new SolidBrush(Color.FromArgb(50, 255, 56));
                    }
                    else
                    {
                        // 灰色
                        brush = new SolidBrush(Color.Gray);
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
            ServerToolStripMenuItem.Text = Utils.i18N.Translate("Server");
            ImportServersFromClipboardToolStripMenuItem.Text = Utils.i18N.Translate("Import Servers From Clipboard");
            AddSocks5ServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [Socks5] Server");
            AddShadowsocksServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [Shadowsocks] Server");
            AddShadowsocksRServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [ShadowsocksR] Server");
            AddVMessServerToolStripMenuItem.Text = Utils.i18N.Translate("Add [VMess] Server");
            ModeToolStripMenuItem.Text = Utils.i18N.Translate("Mode");
            CreateProcessModeToolStripMenuItem.Text = Utils.i18N.Translate("Create Process Mode");
            SubscribeToolStripMenuItem.Text = Utils.i18N.Translate("Subscribe");
            ManageSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate("Manage Subscribe Links");
            UpdateServersFromSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate("Update Servers From Subscribe Links");
            OptionsToolStripMenuItem.Text = Utils.i18N.Translate("Options");
            RestartServiceToolStripMenuItem.Text = Utils.i18N.Translate("Restart Service");
            UninstallServiceToolStripMenuItem.Text = Utils.i18N.Translate("Uninstall Service");
            ReloadModesToolStripMenuItem.Text = Utils.i18N.Translate("Reload Modes");
            AboutToolStripButton.Text = Utils.i18N.Translate("About");
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            ServerLabel.Text = Utils.i18N.Translate("Server");
            ModeLabel.Text = Utils.i18N.Translate("Mode");
            SettingsButton.Text = Utils.i18N.Translate("Settings");
            ControlButton.Text = Utils.i18N.Translate("Start");
            UsedBandwidthLabel.Text = $"{Utils.i18N.Translate("Used")}{Utils.i18N.Translate(": ")}0 KB";
            StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Waiting for command")}";
            NotifyIcon.BalloonTipText = Utils.i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore.");
            ShowMainFormToolStripButton.Text = Utils.i18N.Translate("Show");
            ExitToolStripButton.Text = Utils.i18N.Translate("Exit");

            // 自动检测延迟
            Task.Run(() =>
            {
                while (true)
                {
                    if (State == Objects.State.Waiting || State == Objects.State.Stopped)
                    {
                        TestServer();

                        Thread.Sleep(10000);
                    }
                    else
                    {
                        Thread.Sleep(200);
                    }
                }
            });
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && State != Objects.State.Terminating)
            {
                // 取消"关闭窗口"事件
                e.Cancel = true; // 取消关闭窗体 

                // 使关闭时窗口向右下角缩小的效果
                this.WindowState = FormWindowState.Minimized;
                this.NotifyIcon.Visible = true;

                // 显示提示语
                this.NotifyIcon.BalloonTipTitle = "Netch";
                this.NotifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                this.NotifyIcon.ShowBalloonTip(5);

                Hide();
            }
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
                            Global.Server.AddRange(result);
                        }
                    }
                }

                InitServer();
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
            (new Server.ShadowsocksR()).Show();
            Hide();
        }

        private void AddVMessServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Server.VMess()).Show();
            Hide();
        }

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            (new Mode.Process()).Show();
            Hide();
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
                                                foreach (var x in result)
                                                {
                                                    x.Remark = x.Remark.Split('#')[0].Trim();
                                                }
                                            }

                                            foreach (var x in result)
                                            {
                                                x.Group = item.Remark;
                                            }

                                            Global.Server.AddRange(result);
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

        private void RestartServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            Task.Run(() =>
            {
                try
                {
                    var service = new ServiceController("netfilter2");
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running);
                    }
                    else if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped);
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running);
                    }
                }
                catch (Exception)
                {
                    nfapinet.NFAPI.nf_registerDriver("netfilter2");
                }

                MessageBox.Show(Utils.i18N.Translate("Service has been restarted"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            Task.Run(() =>
            {
                var driver = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";
                if (File.Exists(driver))
                {
                    try
                    {
                        var service = new ServiceController("netfilter2");
                        if (service.Status == ServiceControllerStatus.Running)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                    }
                    catch (Exception)
                    {
                        // 跳过
                    }

                    try
                    {
                        nfapinet.NFAPI.nf_unRegisterDriver("netfilter2");

                        File.Delete(driver);

                        MessageBox.Show(Utils.i18N.Translate("Service has been uninstalled"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Utils.i18N.Translate("Error") + Utils.i18N.Translate(": ") + ex.ToString(), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(Utils.i18N.Translate("Service has been uninstalled"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                Enabled = true;
            });
        }

        private void ReloadModesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            Task.Run(() =>
            {
                InitMode();

                MessageBox.Show(Utils.i18N.Translate("Modes have been reload"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/NetchX/Netch/releases");
        }

        private void EditPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
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
                        (new Server.ShadowsocksR(ServerComboBox.SelectedIndex)).Show();
                        break;
                    case "VMess":
                        (new Server.VMess(ServerComboBox.SelectedIndex)).Show();
                        break;
                    default:
                        return;
                }

                Hide();
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeletePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
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
                MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Testing")}";

            Task.Run(() =>
            {
                TestServer();

                Enabled = true;
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Test done")}";
                Refresh();
            });
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (State == Objects.State.Waiting || State == Objects.State.Stopped)
            {
                // 当前ServerComboBox中至少有一项
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 当前ModeComboBox中至少有一项
                if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select an mode first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;
                ControlButton.Text = "...";
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting")}";
                State = Objects.State.Starting;

                Task.Run(() =>
                {
                    var server = ServerComboBox.SelectedItem as Objects.Server;
                    var mode = ModeComboBox.SelectedItem as Objects.Mode;

                    MainController = new Controllers.MainController();
                    if (MainController.Start(server, mode))
                    {
                        if (mode.Type == 0)
                        {
                            UsedBandwidthLabel.Visible = UploadSpeedLabel.Visible = DownloadSpeedLabel.Visible = true;
                            MainController.pNFController.OnBandwidthUpdated += OnBandwidthUpdated;
                        }

                        ControlButton.Enabled = true;
                        ControlButton.Text = Utils.i18N.Translate("Stop");
                        StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")}";
                        State = Objects.State.Started;
                    }
                    else
                    {
                        ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
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
                    var server = ServerComboBox.SelectedItem as Objects.Server;
                    var mode = ModeComboBox.SelectedItem as Objects.Mode;

                    MainController.Stop();

                    if (mode.Type == 0)
                    {
                        LastUploadBandwidth = 0;
                        LastDownlaodBandwidth = 0;
                        UploadSpeedLabel.Text = "↑: 0 KB/s";
                        DownloadSpeedLabel.Text = "↓: 0 KB/s";
                        UsedBandwidthLabel.Text = $"{Utils.i18N.Translate("Used")}{Utils.i18N.Translate(": ")}0 KB";
                        UsedBandwidthLabel.Visible = UploadSpeedLabel.Visible = DownloadSpeedLabel.Visible = false;
                    }

                    ToolStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
                    ControlButton.Text = Utils.i18N.Translate("Start");
                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Stopped")}";
                    State = Objects.State.Stopped;

                    TestServer();
                });
            }
        }

        private void ShowMainFormToolStripButton_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = true;
                this.ShowInTaskbar = true;  // 显示在系统任务栏 
                this.WindowState = FormWindowState.Normal;  // 还原窗体 
                this.NotifyIcon.Visible = true;  // 托盘图标隐藏 
            }

            this.Activate();
        }

        private void ExitToolStripButton_Click(object sender, EventArgs e)
        {
            Global.Settings["ServerComboBoxSelectedIndex"] = ServerComboBox.SelectedIndex;
            Global.Settings["ModeComboBoxSelectedIndex"] = ModeComboBox.SelectedIndex;
            Utils.Configuration.Save();

            if (State != Objects.State.Waiting && State != Objects.State.Stopped)
            {
                MessageBox.Show(Utils.i18N.Translate("Please press Stop button first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Visible = true;
                this.ShowInTaskbar = true;  // 显示在系统任务栏 
                this.WindowState = FormWindowState.Normal;  // 还原窗体 
                this.NotifyIcon.Visible = true;  // 托盘图标隐藏 

                return;
            }

            State = Objects.State.Terminating;
            this.NotifyIcon.Visible = false;
            this.Close();
            this.Dispose();
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = true;
                this.ShowInTaskbar = true;  //显示在系统任务栏 
                this.WindowState = FormWindowState.Normal;  //还原窗体 
                this.NotifyIcon.Visible = true;  //托盘图标隐藏 
            }
            this.Activate();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            (Global.SettingForm = new SettingForm()).Show();
            Hide();
        }

        private void AboutToolStripButton_Click(object sender, EventArgs e)
        {
            (new AboutForm()).Show();
            Hide();
        }

        public void OnBandwidthUpdated(long upload, long download)
        {
            UsedBandwidthLabel.Text = $"{Utils.i18N.Translate("Used")}{Utils.i18N.Translate(": ")}{Utils.Bandwidth.Compute(upload + download)}";
            UploadSpeedLabel.Text = $"↑: {Utils.Bandwidth.Compute(upload - LastUploadBandwidth)}/s";
            DownloadSpeedLabel.Text = $"↓: {Utils.Bandwidth.Compute(download - LastDownlaodBandwidth)}/s";

            LastUploadBandwidth = upload;
            LastDownlaodBandwidth = download;
            Refresh();
        }
    }
}

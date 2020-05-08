using Microsoft.Win32;
using Netch.Controllers;
using Netch.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
        public Models.State State = Models.State.Waiting;

        /// <summary>
        ///     主控制器
        /// </summary>
        public MainController MainController;

        /// <summary>
        ///     上一次上传的流量
        /// </summary>
        public long LastUploadBandwidth;

        /// <summary>
        ///     上一次下载的流量
        /// </summary>
        public long LastDownloadBandwidth;

        /// <summary>
        ///     是否第一次打开
        /// </summary>
        public bool IsFirstOpened = true;

        public List<Button> ProfileButtons = new List<Button>();

        /// <summary>
        /// 主窗体的静态实例
        /// </summary>
        public static MainForm Instance = null;

        public MainForm()
        {
            InitializeComponent();

            // 监听电源事件
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);

            VersionLabel.Text = UpdateChecker.Version;

            CheckForIllegalCrossThreadCalls = false;
            // MenuStrip.Renderer = new Override.ToolStripProfessionalRender();
            Instance = this;
        }
        /// <summary>
        /// 监听电源事件，自动重启Netch服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend://操作系统即将挂起
                    Logging.Info("操作系统即将挂起，自动停止===>" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    ControlFun();
                    break;
                case PowerModes.Resume://操作系统即将从挂起状态继续
                    Logging.Info("操作系统即将从挂起状态继续，自动重启===>" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    ControlFun();
                    break;
            }
        }
        private void CheckUpdate()
        {
            var updater = new UpdateChecker();
            updater.NewVersionFound += (o, args) =>
            {
                NotifyIcon.ShowBalloonTip(5,
                UpdateChecker.Name,
                $"{Utils.i18N.Translate(@"New version available")}{Utils.i18N.Translate(@": ")}{updater.LatestVersionNumber}",
                ToolTipIcon.Info);
            };
            updater.Check(false, false);
        }

        public void TestServer()
        {
            try
            {
                Parallel.ForEach(Global.Settings.Server, new ParallelOptions { MaxDegreeOfParallelism = 16 }, server =>
                {
                    server.Test();
                });
            }
            catch (Exception)
            {
            }
        }

        public void InitServer()
        {
            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Settings.Server.ToArray());

            // 如果值合法，选中该位置
            if (Global.Settings.ServerComboBoxSelectedIndex > 0 && Global.Settings.ServerComboBoxSelectedIndex < ServerComboBox.Items.Count)
            {
                ServerComboBox.SelectedIndex = Global.Settings.ServerComboBoxSelectedIndex;
            }
            // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
            else if (ServerComboBox.Items.Count > 0)
            {
                ServerComboBox.SelectedIndex = 0;
            }

            // 如果当前 ServerComboBox 中没元素，不做处理
        }

        public void SelectLastMode()
        {
            // 如果值合法，选中该位置
            if (Global.Settings.ModeComboBoxSelectedIndex > 0 && Global.Settings.ModeComboBoxSelectedIndex < ModeComboBox.Items.Count)
            {
                ModeComboBox.SelectedIndex = Global.Settings.ModeComboBoxSelectedIndex;
            }
            // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
            else if (ModeComboBox.Items.Count > 0)
            {
                ModeComboBox.SelectedIndex = 0;
            }

            // 如果当前 ModeComboBox 中没元素，不做处理
        }

        public void InitMode()
        {
            ModeComboBox.Items.Clear();
            Global.ModeFiles.Clear();

            if (Directory.Exists("mode"))
            {
                foreach (var name in Directory.GetFiles("mode", "*.txt"))
                {
                    var ok = true;
                    var mode = new Models.Mode();

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
                                    mode.Remark = Utils.i18N.Translate(splited[0].Trim());
                                }

                                if (splited.Length >= 2)
                                {
                                    if (int.TryParse(splited[1], out var result))
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
                                    if (int.TryParse(splited[2], out var result))
                                    {
                                        mode.BypassChina = result == 1;
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
                                if (!text.StartsWith("#") && !string.IsNullOrWhiteSpace(text))
                                {
                                    mode.Rule.Add(text.Trim());
                                }
                            }

                            i++;
                        }
                    }

                    if (ok)
                    {
                        mode.FileName = Path.GetFileNameWithoutExtension(name);
                        Global.ModeFiles.Add(mode);
                    }
                }

                var array = Global.ModeFiles.ToArray();
                Array.Sort(array, (a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));

                ModeComboBox.Items.AddRange(array);

                SelectLastMode();
            }
        }

        public void AddMode(Models.Mode mode)
        {
            ModeComboBox.Items.Clear();
            Global.ModeFiles.Add(mode);
            var array = Global.ModeFiles.ToArray();
            Array.Sort(array, (a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
            ModeComboBox.Items.AddRange(array);

            SelectLastMode();
        }
        public void UpdateMode(Models.Mode NewMode, Models.Mode OldMode)
        {
            ModeComboBox.Items.Clear();
            Global.ModeFiles.Remove(OldMode);
            Global.ModeFiles.Add(NewMode);
            var array = Global.ModeFiles.ToArray();
            Array.Sort(array, (a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
            ModeComboBox.Items.AddRange(array);

            SelectLastMode();
        }

        private void SaveConfigs()
        {
            Global.Settings.ServerComboBoxSelectedIndex = ServerComboBox.SelectedIndex;
            if (ModeComboBox.SelectedItem != null)
            {

                if (ModeComboBox.Tag is object[] list)
                {
                    Global.Settings.ModeComboBoxSelectedIndex = list.ToList().IndexOf(ModeComboBox.SelectedItem);
                }
                else
                {
                    Global.Settings.ModeComboBoxSelectedIndex = ModeComboBox.Items.IndexOf(ModeComboBox.SelectedItem);
                }
            }
            Utils.Configuration.Save();
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {

                var cbx = sender as ComboBox;

                // 绘制背景颜色
                e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                if (e.Index >= 0)
                {
                    // 绘制 备注/名称 字符串
                    e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, new SolidBrush(Color.Black), e.Bounds);

                    if (cbx.Items[e.Index] is Models.Server)
                    {
                        var item = cbx.Items[e.Index] as Models.Server;

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
                    else if (cbx.Items[e.Index] is Models.Mode)
                    {
                        var item = cbx.Items[e.Index] as Models.Mode;

                        // 绘制延迟底色
                        e.Graphics.FillRectangle(new SolidBrush(Color.Gray), ServerComboBox.Size.Width - 60, e.Bounds.Y, 60, e.Bounds.Height);

                        // 绘制延迟字符串
                        e.Graphics.DrawString(item.Rule.Count.ToString(), cbx.Font, new SolidBrush(Color.Black), ServerComboBox.Size.Width - 58, e.Bounds.Y);
                    }
                }
            }
            catch (Exception)
            { }
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
            ServerToolStripMenuItem.Text = Utils.i18N.Translate(ServerToolStripMenuItem.Text);
            ImportServersFromClipboardToolStripMenuItem.Text = Utils.i18N.Translate(ImportServersFromClipboardToolStripMenuItem.Text);
            AddSocks5ServerToolStripMenuItem.Text = Utils.i18N.Translate(AddSocks5ServerToolStripMenuItem.Text);
            AddShadowsocksServerToolStripMenuItem.Text = Utils.i18N.Translate(AddShadowsocksServerToolStripMenuItem.Text);
            AddShadowsocksRServerToolStripMenuItem.Text = Utils.i18N.Translate(AddShadowsocksRServerToolStripMenuItem.Text);
            AddVMessServerToolStripMenuItem.Text = Utils.i18N.Translate(AddVMessServerToolStripMenuItem.Text);
            AddTrojanServerToolStripMenuItem.Text = Utils.i18N.Translate(AddTrojanServerToolStripMenuItem.Text);
            ModeToolStripMenuItem.Text = Utils.i18N.Translate(ModeToolStripMenuItem.Text);
            CreateProcessModeToolStripMenuItem.Text = Utils.i18N.Translate(CreateProcessModeToolStripMenuItem.Text);
            ManageProcessModeToolStripMenuItem.Text = Utils.i18N.Translate(ManageProcessModeToolStripMenuItem.Text);
            SubscribeToolStripMenuItem.Text = Utils.i18N.Translate(SubscribeToolStripMenuItem.Text);
            ManageSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate(ManageSubscribeLinksToolStripMenuItem.Text);
            UpdateServersFromSubscribeLinksToolStripMenuItem.Text = Utils.i18N.Translate(UpdateServersFromSubscribeLinksToolStripMenuItem.Text);
            OptionsToolStripMenuItem.Text = Utils.i18N.Translate(OptionsToolStripMenuItem.Text);
            exitToolStripMenuItem.Text = Utils.i18N.Translate(exitToolStripMenuItem.Text);
            RestartServiceToolStripMenuItem.Text = Utils.i18N.Translate(RestartServiceToolStripMenuItem.Text);
            UninstallServiceToolStripMenuItem.Text = Utils.i18N.Translate(UninstallServiceToolStripMenuItem.Text);
            ReloadModesToolStripMenuItem.Text = Utils.i18N.Translate(ReloadModesToolStripMenuItem.Text);
            CleanDNSCacheToolStripMenuItem.Text = Utils.i18N.Translate(CleanDNSCacheToolStripMenuItem.Text);
            UpdateACLToolStripMenuItem.Text = Utils.i18N.Translate(UpdateACLToolStripMenuItem.Text);
            updateACLWithProxyToolStripMenuItem.Text = Utils.i18N.Translate(updateACLWithProxyToolStripMenuItem.Text);
            reinstallTapDriverToolStripMenuItem.Text = Utils.i18N.Translate(reinstallTapDriverToolStripMenuItem.Text);
            AboutToolStripButton.Text = Utils.i18N.Translate(AboutToolStripButton.Text);
            ConfigurationGroupBox.Text = Utils.i18N.Translate(ConfigurationGroupBox.Text);
            ServerLabel.Text = Utils.i18N.Translate(ServerLabel.Text);
            ModeLabel.Text = Utils.i18N.Translate(ModeLabel.Text);
            ProfileLabel.Text = Utils.i18N.Translate(ProfileLabel.Text);
            ProfileGroupBox.Text = Utils.i18N.Translate(ProfileGroupBox.Text);
            SettingsButton.Text = Utils.i18N.Translate(SettingsButton.Text);
            ControlButton.Text = Utils.i18N.Translate(ControlButton.Text);
            UsedBandwidthLabel.Text = $@"{Utils.i18N.Translate("Used")}{Utils.i18N.Translate(": ")}0 KB";
            StatusLabel.Text = $@"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Waiting for command")}";
            ShowMainFormToolStripButton.Text = Utils.i18N.Translate(ShowMainFormToolStripButton.Text);
            ExitToolStripButton.Text = Utils.i18N.Translate(ExitToolStripButton.Text);

            InitProfile();

            // 自动检测延迟
            Task.Run(() =>
            {
                while (true)
                {
                    if (State == Models.State.Waiting || State == Models.State.Stopped)
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

            // 打开软件时启动加速，产生开始按钮点击事件
            if (Global.Settings.StartWhenOpened)
            {
                ControlButton.PerformClick();
            }

            // 检查更新
            if (Global.Settings.CheckUpdateWhenOpened)
            {
                CheckUpdate();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && State != Models.State.Terminating)
            {
                // 取消"关闭窗口"事件
                e.Cancel = true; // 取消关闭窗体 

                // 如果未勾选关闭窗口时退出，隐藏至右下角托盘图标
                if (!Global.Settings.ExitWhenClosed)
                {
                    // 使关闭时窗口向右下角缩小的效果
                    WindowState = FormWindowState.Minimized;
                    NotifyIcon.Visible = true;

                    if (IsFirstOpened)
                    {
                        // 显示提示语
                        NotifyIcon.ShowBalloonTip(5,
                        UpdateChecker.Name,
                        Utils.i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."),
                        ToolTipIcon.Info);

                        IsFirstOpened = false;
                    }

                    Hide();
                }
                // 如果勾选了关闭时退出，自动点击退出按钮
                else
                {
                    ExitToolStripButton.PerformClick();
                }
            }
        }

        private void ImportServersFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var texts = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texts))
            {
                var result = Utils.ShareLink.Parse(texts);

                if (result != null)
                {
                    Global.Settings.Server.AddRange(result);
                }
                else
                {
                    MessageBox.Show(Utils.i18N.Translate("Import servers error!"), Utils.i18N.Translate("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                InitServer();
                Utils.Configuration.Save();
            }
        }

        private void AddSocks5ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Server.Socks5().Show();
            Hide();
        }

        private void AddShadowsocksServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Server.Shadowsocks().Show();
            Hide();
        }

        private void AddShadowsocksRServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Server.ShadowsocksR().Show();
            Hide();
        }

        private void AddVMessServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Server.VMess().Show();
            Hide();
        }

        private void AddTrojanServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Server.Trojan().Show();
            Hide();
        }

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            new Mode.Process().Show();
            Hide();
        }

        private void ManageProcessModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.OpenDir(@"mode");
        }

        private void ManageSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SubscribeForm().Show();
            Hide();
        }

        private void UpdateServersFromSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Global.Settings.UseProxyToUpdateSubscription && ServerComboBox.SelectedIndex == -1)
                Global.Settings.UseProxyToUpdateSubscription = false;

            if (Global.Settings.UseProxyToUpdateSubscription)
            {
                // 当前 ServerComboBox 中至少有一项
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;
                ControlButton.Text = "...";
            }

            if (Global.Settings.SubscribeLink.Count > 0)
            {
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting update subscription")}";
                DeletePictureBox.Enabled = false;

                UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = false;
                Task.Run(() =>
                {
                    if (Global.Settings.UseProxyToUpdateSubscription)
                    {
                        var mode = new Models.Mode
                        {
                            Remark = "ProxyUpdate",
                            Type = 5
                        };
                        MainController = new MainController();
                        MainController.Start(ServerComboBox.SelectedItem as Models.Server, mode);
                    }
                    foreach (var item in Global.Settings.SubscribeLink)
                    {
                        using var client = new Override.WebClient();
                        try
                        {
                            if (!string.IsNullOrEmpty(item.UserAgent))
                            {
                                client.Headers.Add("User-Agent", item.UserAgent);
                            }
                            else
                            {
                                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");
                            }

                            if (Global.Settings.UseProxyToUpdateSubscription)
                            {
                                client.Proxy = new System.Net.WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");
                            }

                            var response = client.DownloadString(item.Link);

                            try
                            {
                                response = Utils.ShareLink.URLSafeBase64Decode(response);
                            }
                            catch (Exception)
                            {
                                // 跳过
                            }

                            Global.Settings.Server = Global.Settings.Server.Where(server => server.Group != item.Remark).ToList();
                            var result = Utils.ShareLink.Parse(response);

                            if (result != null)
                            {
                                foreach (var x in result)
                                {
                                    x.Group = item.Remark;
                                    x.Remark = "[" + item.Remark + "] " + x.Remark;
                                }
                                Global.Settings.Server.AddRange(result);
                                NotifyIcon.ShowBalloonTip(5,
                                        UpdateChecker.Name,
                                        string.Format(Utils.i18N.Translate("Update {1} server(s) from {0}"), item.Remark, result.Count),
                                        ToolTipIcon.Info);
                            }
                            else
                            {
                                NotifyIcon.ShowBalloonTip(5,
                                        UpdateChecker.Name,
                                        string.Format(Utils.i18N.Translate("Update servers error from {0}"), item.Remark),
                                        ToolTipIcon.Error);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    InitServer();
                    DeletePictureBox.Enabled = true;
                    if (Global.Settings.UseProxyToUpdateSubscription)
                    {
                        MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
                        ControlButton.Text = Utils.i18N.Translate("Start");
                        MainController.Stop();
                        NatTypeStatusLabel.Text = "";
                    }
                    Utils.Configuration.Save();
                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Subscription updated")}";
                }).ContinueWith(task =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true;
                    }));
                });

                NotifyIcon.ShowBalloonTip(5,
                        UpdateChecker.Name,
                        Utils.i18N.Translate("Updating in the background"),
                        ToolTipIcon.Info);
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("No subscription link"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RestartServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Restarting service")}";

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

                MessageBox.Show(this, Utils.i18N.Translate("Service has been restarted"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Uninstalling Service")}";

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

                        MessageBox.Show(this, Utils.i18N.Translate("Service has been uninstalled"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, Utils.i18N.Translate("Error") + Utils.i18N.Translate(": ") + ex, Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(this, Utils.i18N.Translate("Service has been uninstalled"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                Enabled = true;
            });
        }

        private void ReloadModesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            SaveConfigs();
            Task.Run(() =>
            {
                InitMode();

                MessageBox.Show(this, Utils.i18N.Translate("Modes have been reload"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void CleanDNSCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            Task.Run(() =>
            {
                Utils.DNS.Cache.Clear();

                MessageBox.Show(this, Utils.i18N.Translate("DNS cache cleanup succeeded"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("DNS cache cleanup succeeded")}";
                Enabled = true;
            });
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/NetchX/Netch/releases");
        }

        private void EditPictureBox_Click(object sender, EventArgs e)
        {
            SaveConfigs();
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex != -1)
            {
                switch (Global.Settings.Server[ServerComboBox.SelectedIndex].Type)
                {
                    case "Socks5":
                        new Server.Socks5(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "SS":
                        new Server.Shadowsocks(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "SSR":
                        new Server.ShadowsocksR(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "VMess":
                        new Server.VMess(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "Trojan":
                        new Server.Trojan(ServerComboBox.SelectedIndex).Show();
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
            // 当前 ServerComboBox 中至少有一项
            if (ServerComboBox.SelectedIndex != -1)
            {
                var index = ServerComboBox.SelectedIndex;

                Global.Settings.Server.Remove(ServerComboBox.SelectedItem as Models.Server);
                ServerComboBox.Items.RemoveAt(index);

                if (ServerComboBox.Items.Count > 0)
                {
                    ServerComboBox.SelectedIndex = index != 0 ? index - 1 : index;
                }
                Utils.Configuration.Save();
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
                Utils.Configuration.Save();
            });
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            ControlFun();
        }
        public void ControlFun()
        {
            //聚焦到启动按钮，防止模式选择框变成蓝色:D
            ControlButton.Focus();
            SaveConfigs();
            if (State == Models.State.Waiting || State == Models.State.Stopped)
            {
                // 当前 ServerComboBox 中至少有一项
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 当前 ModeComboBox 中至少有一项
                if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select an mode first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                //MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;

                //关闭启动按钮
                ControlButton.Enabled = false;

                //关闭部分选项功能
                RestartServiceToolStripMenuItem.Enabled = false;
                UninstallServiceToolStripMenuItem.Enabled = false;
                updateACLWithProxyToolStripMenuItem.Enabled = false;
                UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = false;
                reinstallTapDriverToolStripMenuItem.Enabled = false;
                ServerComboBox.Enabled = false;
                ModeComboBox.Enabled = false;

                ControlButton.Text = "...";
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting")}";
                State = Models.State.Starting;

                Task.Run(() =>
                {
                    var server = ServerComboBox.SelectedItem as Models.Server;
                    var mode = ModeComboBox.SelectedItem as Models.Mode;

                    MainController = new MainController();

                    var startResult = MainController.Start(server, mode);

                    if (startResult)
                    {
                        // UsedBandwidthLabel.Visible = UploadSpeedLabel.Visible = DownloadSpeedLabel.Visible = true;
                        // MainController.pNFController.OnBandwidthUpdated += OnBandwidthUpdated;

                        // 如果勾选启动后最小化
                        if (Global.Settings.MinimizeWhenStarted)
                        {
                            WindowState = FormWindowState.Minimized;
                            NotifyIcon.Visible = true;

                            if (IsFirstOpened)
                            {
                                // 显示提示语
                                NotifyIcon.ShowBalloonTip(5,
                                UpdateChecker.Name,
                                Utils.i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."),
                                ToolTipIcon.Info);

                                IsFirstOpened = false;
                            }

                            Hide();
                        }

                        ControlButton.Enabled = true;
                        ControlButton.Text = Utils.i18N.Translate("Stop");

                        if (mode.Type != 3 && mode.Type != 5)
                        {
                            if (server.Type != "Socks5")
                            {
                                if (Global.Settings.LocalAddress == "0.0.0.0")
                                {

                                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")} ({Utils.i18N.Translate("Allow other Devices to connect")} Socks5 {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.Socks5LocalPort})";
                                }
                                else
                                {
                                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")} (Socks5 {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.Socks5LocalPort}{")"}";
                                }
                            }
                            else
                            {
                                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")}";
                            }
                        }
                        else
                        {
                            if (server.Type != "Socks5")
                            {
                                if (Global.Settings.LocalAddress == "0.0.0.0")
                                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")} ({Utils.i18N.Translate("Allow other Devices to connect")} Socks5 {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.Socks5LocalPort} | HTTP {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.HTTPLocalPort}{")"}";
                                else
                                {
                                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")} (Socks5 {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.Socks5LocalPort} | HTTP {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.HTTPLocalPort})";
                                }
                            }
                            else
                            {
                                if (Global.Settings.LocalAddress == "0.0.0.0")
                                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")} ({Utils.i18N.Translate("Allow other Devices to connect")} HTTP {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.HTTPLocalPort}{")"}";
                                else
                                {
                                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Started")} (HTTP {Utils.i18N.Translate("Local Port")}{Utils.i18N.Translate(": ")}{Global.Settings.HTTPLocalPort})";
                                }
                            }
                        }

                        State = Models.State.Started;
                        if (Global.Settings.StartedTcping)
                        {
                            // 自动检测延迟
                            Task.Run(() =>
                            {
                                while (true)
                                {
                                    if (State == Models.State.Started)
                                    {
                                        server.Test();
                                        // 重载服务器列表
                                        InitServer();

                                        Thread.Sleep(Global.Settings.StartedTcping_Interval * 1000);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            });
                        }
                    }
                    else
                    {
                        MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;

                        RestartServiceToolStripMenuItem.Enabled = true;
                        UninstallServiceToolStripMenuItem.Enabled = true;
                        updateACLWithProxyToolStripMenuItem.Enabled = true;
                        UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true;
                        reinstallTapDriverToolStripMenuItem.Enabled = true;
                        ServerComboBox.Enabled = true;
                        ModeComboBox.Enabled = true;
                        //隐藏NTT测试
                        NatTypeStatusLabel.Visible = false;

                        ControlButton.Text = Utils.i18N.Translate("Start");
                        StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Start failed")}";
                        State = Models.State.Stopped;
                    }
                });
            }
            else
            {

                ControlButton.Enabled = false;
                ControlButton.Text = "...";
                StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Stopping")}";
                State = Models.State.Stopping;

                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = SettingsButton.Enabled = true;

                ProfileGroupBox.Enabled = false;

                Task.Run(() =>
                {
                    var server = ServerComboBox.SelectedItem as Models.Server;
                    var mode = ModeComboBox.SelectedItem as Models.Mode;

                    MainController.Stop();
                    NatTypeStatusLabel.Text = "";

                    // LastUploadBandwidth = 0;
                    // LastDownloadBandwidth = 0;
                    // UploadSpeedLabel.Text = "↑: 0 KB/s";
                    // DownloadSpeedLabel.Text = "↓: 0 KB/s";
                    // UsedBandwidthLabel.Text = $"{Utils.i18N.Translate("Used")}{Utils.i18N.Translate(": ")}0 KB";
                    // UsedBandwidthLabel.Visible = UploadSpeedLabel.Visible = DownloadSpeedLabel.Visible = false;

                    ControlButton.Enabled = true;
                    ProfileGroupBox.Enabled = true;

                    RestartServiceToolStripMenuItem.Enabled = true;
                    UninstallServiceToolStripMenuItem.Enabled = true;
                    updateACLWithProxyToolStripMenuItem.Enabled = true;
                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true;
                    reinstallTapDriverToolStripMenuItem.Enabled = true;
                    ServerComboBox.Enabled = true;
                    ModeComboBox.Enabled = true;
                    //隐藏NTT测试
                    NatTypeStatusLabel.Visible = false;

                    ControlButton.Text = Utils.i18N.Translate("Start");
                    StatusLabel.Text = $"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Stopped")}";
                    State = Models.State.Stopped;

                    TestServer();
                });
            }

        }
        private void ShowMainFormToolStripButton_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                ShowInTaskbar = true;  // 显示在系统任务栏 
                WindowState = FormWindowState.Normal;  // 还原窗体 
                NotifyIcon.Visible = true;  // 托盘图标隐藏 
            }

            Activate();
        }

        private void ExitToolStripButton_Click(object sender, EventArgs e)
        {
            // 当前状态如果不是已停止状态
            if (State != Models.State.Waiting && State != Models.State.Stopped)
            {
                // 如果未勾选退出时停止，要求先点击停止按钮
                if (!Global.Settings.StopWhenExited)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please press Stop button first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Visible = true;
                    ShowInTaskbar = true;  // 显示在系统任务栏 
                    WindowState = FormWindowState.Normal;  // 还原窗体 
                    NotifyIcon.Visible = true;  // 托盘图标隐藏 

                    return;
                }
                // 否则直接调用停止按钮的方法

                ControlButton_Click(sender, e);
            }

            SaveConfigs();

            State = Models.State.Terminating;
            NotifyIcon.Visible = false;
            Close();
            Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                ShowInTaskbar = true;  //显示在系统任务栏 
                WindowState = FormWindowState.Normal;  //还原窗体 
                NotifyIcon.Visible = true;  //托盘图标隐藏 
            }
            Activate();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            (Global.SettingForm = new SettingForm()).Show();
            Hide();
        }

        private void AboutToolStripButton_Click(object sender, EventArgs e)
        {
            new AboutForm().Show();
            Hide();
        }

        public void OnBandwidthUpdated(long upload, long download)
        {
            UsedBandwidthLabel.Text = $"{Utils.i18N.Translate("Used")}{Utils.i18N.Translate(": ")}{Utils.Bandwidth.Compute(upload + download)}";
            UploadSpeedLabel.Text = $"↑: {Utils.Bandwidth.Compute(upload - LastUploadBandwidth)}/s";
            DownloadSpeedLabel.Text = $"↓: {Utils.Bandwidth.Compute(download - LastDownloadBandwidth)}/s";

            LastUploadBandwidth = upload;
            LastDownloadBandwidth = download;
            Refresh();
        }

        private void ProfileButton_Click(object sender, EventArgs e)
        {
            var index = ProfileButtons.IndexOf((Button)sender);

            //Utils.Logging.Info(String.Format("Button no.{0} clicked", index));

            if (ModifierKeys == Keys.Control)
            {
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select an mode first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (ProfileNameText.Text == "")
                {
                    MessageBox.Show(Utils.i18N.Translate("Please enter a profile name first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SaveProfile(index);
                    ProfileButtons[index].Text = ProfileNameText.Text;
                }
            }
            else
            {
                if (ProfileButtons[index].Text == Utils.i18N.Translate("Error") || ProfileButtons[index].Text == Utils.i18N.Translate("None"))
                {
                    MessageBox.Show(Utils.i18N.Translate("No saved profile here. Save a profile first by Ctrl+Click on the button"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                try
                {
                    ProfileNameText.Text = LoadProfile(index);

                    // start the profile
                    var need2ndStart = true;
                    if (State == Models.State.Waiting || State == Models.State.Stopped)
                    {
                        need2ndStart = false;
                    }

                    ControlButton.PerformClick();

                    if (need2ndStart)
                    {
                        Task.Run(() =>
                        {
                            while (State != Models.State.Stopped)
                            {
                                Thread.Sleep(200);
                            }

                            ControlButton.PerformClick();
                        });
                    }
                }
                catch (Exception ee)
                {
                    Task.Run(() =>
                    {
                        Utils.Logging.Info(ee.Message);
                        ProfileButtons[index].Text = Utils.i18N.Translate("Error");
                        Thread.Sleep(1200);
                        ProfileButtons[index].Text = Utils.i18N.Translate("None");
                    });
                }

            }


        }

        public void InitProfile()
        {
            var num_profile = Global.Settings.ProfileCount;
            if (num_profile == 0)
            {
                ProfileGroupBox.Size = new Size(0, 0);
                ConfigurationGroupBox.Size -= new Size(0, 25);
                this.Size -= new Size(0, 70 + 25);
                configLayoutPanel.RowStyles[2].Height = 0;
                return;
            }

            ProfileTable.ColumnCount = num_profile;

            while (Global.Settings.profiles.Count < num_profile)
            {
                Global.Settings.profiles.Add(new Models.Profile());
            }

            // buttons
            for (var i = 0; i < num_profile; ++i)
            {
                var b = new Button();
                ProfileTable.Controls.Add(b, i, 0);
                b.Location = new Point(i * 100, 0);
                b.Click += ProfileButton_Click;
                b.Dock = DockStyle.Fill;
                b.Text = "None";
                ProfileButtons.Add(b);

                if (!Global.Settings.profiles[i].IsDummy)
                {
                    b.Text = Global.Settings.profiles[i].ProfileName;
                }
                else
                {
                    b.Text = Utils.i18N.Translate(b.Text);
                }
            }

            // equal column
            ProfileTable.ColumnStyles.Clear();
            for (var i = 1; i <= ProfileTable.RowCount; i++)
            {
                ProfileTable.RowStyles.Add(new RowStyle(SizeType.Percent, 1));
            }
            for (var i = 1; i <= ProfileTable.ColumnCount; i++)
            {
                ProfileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            }
        }

        private string LoadProfile(int index)
        {
            var p = Global.Settings.profiles[index];

            if (p.IsDummy)
                throw new Exception("Profile not found.");

            var result = false;

            foreach (Models.Server server in ServerComboBox.Items)
            {
                if (server.Remark.Equals(p.ServerRemark))
                {
                    ServerComboBox.SelectedItem = server;
                    result = true;
                    break;
                }
            }

            if (!result)
                throw new Exception("Server not found.");

            result = false;
            foreach (Models.Mode mode in ModeComboBox.Items)
            {
                if (mode.Remark.Equals(p.ModeRemark))
                {
                    ModeComboBox.SelectedItem = mode;
                    result = true;
                    break;
                }
            }

            if (!result)
                throw new Exception("Mode not found.");

            return p.ProfileName;
        }

        private void SaveProfile(int index)
        {
            var selectedServer = (Models.Server)ServerComboBox.SelectedItem;
            var selectedMode = (Models.Mode)ModeComboBox.SelectedItem;
            var name = ProfileNameText.Text;

            Global.Settings.profiles[index] = new Models.Profile(selectedServer, selectedMode, name);

        }

        private void EditModePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ModeComboBox中至少有一项
            if (ModeComboBox.Items.Count > 0 && ModeComboBox.SelectedIndex != -1)
            {
                SaveConfigs();
                var selectedMode = (Models.Mode)ModeComboBox.SelectedItem;
                // 只允许修改进程加速的模式
                if (selectedMode.Type == 0)
                {
                    //Process.Start(Environment.CurrentDirectory + "\\mode\\" + selectedMode.FileName + ".txt");
                    Mode.Process process = new Mode.Process(selectedMode);
                    process.Text = "Edit Process Mode";
                    process.Show();
                    Hide();
                }
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select an mode first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteModePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ModeComboBox中至少有一项
            if (ModeComboBox.Items.Count > 0 && ModeComboBox.SelectedIndex != -1)
            {
                var selectedMode = (Models.Mode)ModeComboBox.SelectedItem;

                //删除模式文件
                selectedMode.DeleteFile("mode");

                ModeComboBox.Items.Clear();
                Global.ModeFiles.Remove(selectedMode);
                var array = Global.ModeFiles.ToArray();
                Array.Sort(array, (a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
                ModeComboBox.Items.AddRange(array);

                SelectLastMode();
                Utils.Configuration.Save();
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select an mode first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CopyLinkPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex != -1)
            {
                var selectedMode = (Models.Server)ServerComboBox.SelectedItem;
                try
                {
                    //听说巨硬BUG经常会炸，所以Catch一下 :D
                    Clipboard.SetText(Utils.ShareLink.GetShareLink(selectedMode));
                }
                catch (Exception)
                {

                }
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        public void StatusText(string text)
        {
            StatusLabel.Text = text;
        }
        public void NatTypeStatusText(string text)
        {
            NatTypeStatusLabel.Visible = true;
            if (!string.IsNullOrWhiteSpace(text))
            {
                NatTypeStatusLabel.Text = "NAT" + Utils.i18N.Translate(": ") + text;
            }
            else
            {
                NatTypeStatusLabel.Text = "NAT" + Utils.i18N.Translate(": ") + Utils.i18N.Translate("Test failed");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 当前状态如果不是已停止状态
            if (State != Models.State.Waiting && State != Models.State.Stopped)
            {
                // 如果未勾选退出时停止，要求先点击停止按钮
                if (!Global.Settings.StopWhenExited)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please press Stop button first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Visible = true;
                    ShowInTaskbar = true;  // 显示在系统任务栏 
                    WindowState = FormWindowState.Normal;  // 还原窗体 
                    NotifyIcon.Visible = true;  // 托盘图标隐藏 

                    return;
                }
                // 否则直接调用停止按钮的方法

                ControlButton_Click(sender, e);
            }

            SaveConfigs();

            State = Models.State.Terminating;
            NotifyIcon.Visible = false;
            Close();
            Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        private void updateACLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting update ACL")}");
            using var client = new Override.WebClient();

            client.DownloadFileTaskAsync(Global.Settings.ACL, "bin\\default.acl");
            client.DownloadFileCompleted += ((sender, args) =>
            {
                try
                {

                    if (args.Error == null)
                    {
                        NotifyIcon.ShowBalloonTip(5,
                                UpdateChecker.Name, Utils.i18N.Translate("ACL updated successfully"),
                                ToolTipIcon.Info);
                        //MessageBox.Show(Utils.i18N.Translate("ACL updated successfully"));
                    }
                    else
                    {
                        Utils.Logging.Info("ACL更新失败！" + args.Error);
                        /*NotifyIcon.ShowBalloonTip(5,
                                UpdateChecker.Name,
                                Utils.i18N.Translate("ACL update failed") + args.Error,
                                ToolTipIcon.Error);*/
                        MessageBox.Show(Utils.i18N.Translate("ACL update failed") + "\n" + args.Error);
                    }
                }
                finally
                {
                    StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Waiting for command")}");
                }
            });
        }

        private void updateACLWithProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updateACLWithProxyToolStripMenuItem.Enabled = false;
            if (Global.Settings.UseProxyToUpdateSubscription)
            {
                // 当前 ServerComboBox 中至少有一项
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(Utils.i18N.Translate("Please select a server first"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;
                ControlButton.Text = "...";
            }

            Task.Run(() =>
            {
                var mode = new Models.Mode
                {
                    Remark = "ProxyUpdate",
                    Type = 5
                };
                MainController = new MainController();
                MainController.Start(ServerComboBox.SelectedItem as Models.Server, mode);

                using var client = new Override.WebClient();

                client.Proxy = new System.Net.WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Updating in the background")}");
                try
                {
                    client.DownloadFile(Global.Settings.ACL, "bin\\default.acl");
                    NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name, Utils.i18N.Translate("ACL updated successfully"),
                            ToolTipIcon.Info);
                }
                catch (Exception e)
                {
                    Utils.Logging.Info("使用代理更新ACL失败！" + e.Message);
                    /*NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name,
                            Utils.i18N.Translate("ACL update failed") + args.Error,
                            ToolTipIcon.Error);*/
                    MessageBox.Show(Utils.i18N.Translate("ACL update failed") + "\n" + e.Message);
                }
                finally
                {
                    updateACLWithProxyToolStripMenuItem.Enabled = true;
                    MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;

                    ControlButton.Text = Utils.i18N.Translate("Start");
                    StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Waiting for command")}");
                    MainController.Stop();
                }
            });
        }

        private void reinstallTapDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Reinstalling Tap driver")}");
                Enabled = false;
                try
                {
                    Configuration.deltapall();
                    Configuration.addtap();
                    NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name, Utils.i18N.Translate("Reinstall Tap driver successfully"),
                            ToolTipIcon.Info);
                }
                catch
                {
                    NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name, Utils.i18N.Translate("Reinstall Tap driver failed"),
                            ToolTipIcon.Error);
                }
                finally
                {
                    ControlButton.Text = Utils.i18N.Translate("Start");
                    StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Waiting for command")}");
                    Enabled = true;
                }
            });
        }
    }
}

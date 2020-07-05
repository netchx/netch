using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Netch.Controllers;
using Netch.Forms.Mode;
using Netch.Forms.Server;
using Netch.Models;
using Netch.Utils;
using nfapinet;
using Trojan = Netch.Forms.Server.Trojan;
using VMess = Netch.Forms.Server.VMess;
using WebClient = Netch.Override.WebClient;

namespace Netch.Forms
{
    public partial class MainForm : Form
    {
        /// <summary>
        ///     当前状态
        /// </summary>
        public State State { get; private set; } = State.Waiting;

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
        public static MainForm Instance;


        public MainForm()
        {
            InitializeComponent();

            // 监听电源事件
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

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
            //不对Netch命令等待状态的电源事件做任何处理
            if (!State.Equals(State.Waiting))
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
        }
        private void CheckUpdate()
        {
            var updater = new UpdateChecker();
            updater.NewVersionFound += (o, args) =>
            {
                NotifyIcon.ShowBalloonTip(5,
                UpdateChecker.Name,
                $"{i18N.Translate(@"New version available")}{i18N.Translate(@": ")}{updater.LatestVersionNumber}",
                ToolTipIcon.Info);
            };
            updater.Check(false, false);
        }

        public void TestServer()
        {
            try
            {
                Parallel.ForEach(Global.Settings.Server, new ParallelOptions {MaxDegreeOfParallelism = 16},
                    server => { server.Test(); });
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

        public void InitText(bool isStarted)
        {
            ServerToolStripMenuItem.Text = i18N.Translate("Server");
            ImportServersFromClipboardToolStripMenuItem.Text = i18N.Translate("Import Servers From Clipboard");
            AddSocks5ServerToolStripMenuItem.Text = i18N.Translate("Add [Socks5] Server");
            AddShadowsocksServerToolStripMenuItem.Text = i18N.Translate("Add [Shadowsocks] Server");
            AddShadowsocksRServerToolStripMenuItem.Text = i18N.Translate("Add [ShadowsocksR] Server");
            AddVMessServerToolStripMenuItem.Text = i18N.Translate("Add [VMess] Server");
            AddTrojanServerToolStripMenuItem.Text = i18N.Translate("Add [Trojan] Server");
            ModeToolStripMenuItem.Text = i18N.Translate("Mode");
            CreateProcessModeToolStripMenuItem.Text = i18N.Translate("Create Process Mode");
            SubscribeToolStripMenuItem.Text = i18N.Translate("Subscribe");
            ManageSubscribeLinksToolStripMenuItem.Text = i18N.Translate("Manage Subscribe Links");
            UpdateServersFromSubscribeLinksToolStripMenuItem.Text = i18N.Translate("Update Servers From Subscribe Links");
            OptionsToolStripMenuItem.Text = i18N.Translate("Options");
            ReloadModesToolStripMenuItem.Text = i18N.Translate("Reload Modes");
            RestartServiceToolStripMenuItem.Text = i18N.Translate("Restart Service");
            UninstallServiceToolStripMenuItem.Text = i18N.Translate("Uninstall Service");
            CleanDNSCacheToolStripMenuItem.Text = i18N.Translate("Clean DNS Cache");
            UpdateACLToolStripMenuItem.Text = i18N.Translate("Update ACL");
            updateACLWithProxyToolStripMenuItem.Text = i18N.Translate("Update ACL with proxy");
            reinstallTapDriverToolStripMenuItem.Text = i18N.Translate("Reinstall TUN/TAP driver");
            OpenDirectoryToolStripMenuItem.Text = i18N.Translate("Open Directory");
            AboutToolStripButton.Text = i18N.Translate("About");
            VersionLabel.Text = i18N.Translate("xxx");
            exitToolStripMenuItem.Text = i18N.Translate("Exit");
            RelyToolStripMenuItem.Text = i18N.Translate("Unable to start? Click me to download");
            ConfigurationGroupBox.Text = i18N.Translate("Configuration");
            ProfileLabel.Text = i18N.Translate("Profile");
            ModeLabel.Text = i18N.Translate("Mode");
            ServerLabel.Text = i18N.Translate("Server");
            UsedBandwidthLabel.Text = i18N.Translate("Used: 0 KB");
            DownloadSpeedLabel.Text = i18N.Translate("↓: 0 KB/s");
            UploadSpeedLabel.Text = i18N.Translate("↑: 0 KB/s");
            NotifyIcon.Text = i18N.Translate("Netch");
            ShowMainFormToolStripButton.Text = i18N.Translate("Show");
            ExitToolStripButton.Text = i18N.Translate("Exit");
            SettingsButton.Text = i18N.Translate("Settings");
            ProfileGroupBox.Text = i18N.Translate("Profiles");
            // 加载翻译

            UsedBandwidthLabel.Text = $@"{i18N.Translate("Used")}{i18N.Translate(": ")}0 KB";
            UpdateStatus();

            VersionLabel.Text = UpdateChecker.Version;
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
                                    mode.Remark = i18N.Translate(splited[0].Trim());
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
            if (ModeComboBox.Items.Count != 0 && ModeComboBox.SelectedItem != null)
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
            Configuration.Save();
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                var cbx = sender as ComboBox;

                var eWidth = ServerComboBox.Width / 10;

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
                        e.Graphics.FillRectangle(brush, eWidth * 9, e.Bounds.Y, eWidth, e.Bounds.Height);

                        // 绘制延迟字符串
                        e.Graphics.DrawString(item.Delay.ToString(), cbx.Font, new SolidBrush(Color.Black), eWidth * 9 + eWidth / 30, e.Bounds.Y);
                    }
                    else if (cbx.Items[e.Index] is Models.Mode)
                    {
                        var item = cbx.Items[e.Index] as Models.Mode;


                        // 绘制延迟底色
                        e.Graphics.FillRectangle(new SolidBrush(Color.Gray), eWidth * 9, e.Bounds.Y, eWidth, e.Bounds.Height);


                        // 绘制延迟字符串
                        e.Graphics.DrawString(item.Rule.Count.ToString(), cbx.Font, new SolidBrush(Color.Black), eWidth * 9 + eWidth / 30, e.Bounds.Y);
                    }
                }
            }
            catch (Exception)
            { }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 加载服务器
            InitServer();

            // 加载模式
            InitMode();

            // 加载翻译
            InitText(false);

            // 加载快速配置
            SizeHeight = Size.Height;
            ControllHeight = ConfigurationGroupBox.Controls[0].Height / 3;
            ProfileBoxHeight = ProfileGroupBox.Height;
            CFGBoxHeight = ConfigurationGroupBox.Height;
            InitProfile();

            // 自动检测延迟
            Task.Run(() =>
            {
                while (true)
                {
                    if (State == State.Waiting || State == State.Stopped)
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
            if (e.CloseReason == CloseReason.UserClosing && State != State.Terminating)
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
                        i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."),
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
                var result = ShareLink.Parse(texts);

                if (result != null)
                {
                    Global.Settings.Server.AddRange(result);
                }
                else
                {
                    MessageBox.Show(i18N.Translate("Import servers error!"), i18N.Translate("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                InitServer();
                Configuration.Save();
            }
        }

        private void AddSocks5ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Socks5().Show();
            Hide();
        }

        private void AddShadowsocksServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Shadowsocks().Show();
            Hide();
        }

        private void AddShadowsocksRServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ShadowsocksR().Show();
            Hide();
        }

        private void AddVMessServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new VMess().Show();
            Hide();
        }

        private void AddTrojanServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Trojan().Show();
            Hide();
        }

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            new Process().Show();
            Hide();
        }

        private void OpenDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.OpenDir(@".\");
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
                    MessageBox.Show(i18N.Translate("Please select a server first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;
                ControlButton.Text = "...";
            }

            if (Global.Settings.SubscribeLink.Count > 0)
            {
                StatusText(i18N.Translate("Starting update subscription"));
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
                        using var client = new WebClient();
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
                                client.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");
                            }

                            var response = client.DownloadString(item.Link);

                            try
                            {
                                response = ShareLink.URLSafeBase64Decode(response);
                            }
                            catch (Exception)
                            {
                                // 跳过
                            }

                            Global.Settings.Server = Global.Settings.Server.Where(server => server.Group != item.Remark).ToList();
                            var result = ShareLink.Parse(response);

                            if (result != null)
                            {
                                foreach (var x in result)
                                {
                                    x.Group = item.Remark;
                                }

                                Global.Settings.Server.AddRange(result);
                                NotifyIcon.ShowBalloonTip(5,
                                        UpdateChecker.Name,
                                        string.Format(i18N.Translate("Update {1} server(s) from {0}"), item.Remark, result.Count),
                                        ToolTipIcon.Info);
                            }
                            else
                            {
                                NotifyIcon.ShowBalloonTip(5,
                                        UpdateChecker.Name,
                                        string.Format(i18N.Translate("Update servers error from {0}"), item.Remark),
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
                        ControlButton.Text = i18N.Translate("Start");
                        MainController.Stop();
                        NatTypeStatusLabel.Text = "";
                    }
                    Configuration.Save();
                    StatusText(i18N.Translate("Subscription updated"));
                }).ContinueWith(task =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true;
                    }));
                });

                NotifyIcon.ShowBalloonTip(5,
                        UpdateChecker.Name,
                        i18N.Translate("Updating in the background"),
                        ToolTipIcon.Info);
            }
            else
            {
                MessageBox.Show(i18N.Translate("No subscription link"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RestartServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Restarting service"));

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
                    NFAPI.nf_registerDriver("netfilter2");
                }

                MessageBox.Show(this, i18N.Translate("Service has been restarted"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Uninstalling Service"));

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
                        NFAPI.nf_unRegisterDriver("netfilter2");

                        File.Delete(driver);

                        MessageBox.Show(this, i18N.Translate("Service has been uninstalled"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, i18N.Translate("Error") + i18N.Translate(": ") + ex, i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(this, i18N.Translate("Service has been uninstalled"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                MessageBox.Show(this, i18N.Translate("Modes have been reload"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Enabled = true;
            });
        }

        private void CleanDNSCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            Task.Run(() =>
            {
                DNS.Cache.Clear();

                MessageBox.Show(this, i18N.Translate("DNS cache cleanup succeeded"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                StatusText(i18N.Translate("DNS cache cleanup succeeded"));
                Enabled = true;
            });
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start($"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases");
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
                        new Socks5(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "SS":
                        new Shadowsocks(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "SSR":
                        new ShadowsocksR(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "VMess":
                        new VMess(ServerComboBox.SelectedIndex).Show();
                        break;
                    case "Trojan":
                        new Trojan(ServerComboBox.SelectedIndex).Show();
                        break;
                    default:
                        return;
                }

                Hide();
            }
            else
            {
                MessageBox.Show(i18N.Translate("Please select a server first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Configuration.Save();
            }
            else
            {
                MessageBox.Show(i18N.Translate("Please select a server first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Testing"));

            Task.Run(() =>
            {
                TestServer();

                Enabled = true;
                StatusText(i18N.Translate("Test done"));
                Refresh();
                Configuration.Save();
            });
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            //防止模式选择框变成蓝色:D
            ModeComboBox.Select(0, 0);
            ControlFun();
        }

        public void ControlFun()
        {
            SaveConfigs();
            if (State == State.Waiting || State == State.Stopped)
            {
                // 服务器、模式 需选择
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(i18N.Translate("Please select a server first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(i18N.Translate("Please select an mode first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                //MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;

                UpdateStatus(State.Starting);

                Task.Run(() =>
                {
                    var server = ServerComboBox.SelectedItem as Models.Server;
                    var mode = ModeComboBox.SelectedItem as Models.Mode;

                    MainController = new MainController();

                    var startResult = MainController.Start(server, mode);

                    if (startResult)
                    {
                        Task.Run(() =>
                        {
                            LastUploadBandwidth = 0;
                            //LastDownloadBandwidth = 0;
                            //UploadSpeedLabel.Text = "↑: 0 KB/s";
                            DownloadSpeedLabel.Text = "↑↓: 0 KB/s";
                            UsedBandwidthLabel.Text = $"{i18N.Translate("Used")}{i18N.Translate(": ")}0 KB";
                            UsedBandwidthLabel.Visible = UploadSpeedLabel.Visible = DownloadSpeedLabel.Visible = true;


                            UploadSpeedLabel.Visible = false;
                            Bandwidth.NetTraffic(server, mode, MainController);
                        });
                        //MainController.pNFController.OnBandwidthUpdated += OnBandwidthUpdated;

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
                                i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."),
                                ToolTipIcon.Info);

                                IsFirstOpened = false;
                            }

                            Hide();
                        }

                        var text = new StringBuilder(" (");
                        text.Append(Global.Settings.LocalAddress == "0.0.0.0" ? i18N.Translate("Allow other Devices to connect") + " " : "");
                        if (server.Type == "Socks5")
                        {
                            // 不可控Socks5
                            if (mode.Type == 3 && mode.Type == 5)
                            {
                                // 可控HTTP
                                text.Append($"HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
                            }
                            else
                            {
                                // 不可控HTTP
                                text.Clear();
                            }
                        }
                        else
                        {
                            // 可控Socks5
                            text.Append($"Socks5 {i18N.Translate("Local Port", ": ")}{Global.Settings.Socks5LocalPort}");
                            if (mode.Type == 3 || mode.Type == 5)
                            {
                                //有HTTP
                                text.Append($" | HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
                            }
                        }

                        if (text.Length > 0)
                        {
                            text.Append(")");
                        }

                        UpdateStatus(State.Started);
                        StatusText(i18N.Translate(StateExtension.GetStatusString(State)) + text);

                        if (Global.Settings.StartedTcping)
                        {
                            // 自动检测延迟
                            Task.Run(() =>
                            {
                                while (true)
                                {
                                    if (State == State.Started)
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
                        UpdateStatus(State.Stopped);
                        StatusText(i18N.Translate("Start Failed"));
                    }
                });
            }
            else
            {
                // 停止
                UpdateStatus(State.Stopping);

                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = SettingsButton.Enabled = true;

                ProfileGroupBox.Enabled = false;

                Task.Run(() =>
                {
                    var server = ServerComboBox.SelectedItem as Models.Server;
                    var mode = ModeComboBox.SelectedItem as Models.Mode;

                    MainController.Stop();
                    NatTypeStatusLabel.Text = "";

                    UpdateStatus(State.Stopped);

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
            if (State != State.Waiting && State != State.Stopped)
            {
                // 如果未勾选退出时停止，要求先点击停止按钮
                if (!Global.Settings.StopWhenExited)
                {
                    MessageBox.Show(i18N.Translate("Please press Stop button first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

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

            State = State.Terminating;
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
        public void OnBandwidthUpdated(long download)
        {
            try
            {
                UsedBandwidthLabel.Text = $"{i18N.Translate("Used")}{i18N.Translate(": ")}{Bandwidth.Compute(download)}";
                //UploadSpeedLabel.Text = $"↑: {Utils.Bandwidth.Compute(upload - LastUploadBandwidth)}/s";
                DownloadSpeedLabel.Text = $"↑↓: {Bandwidth.Compute(download - LastDownloadBandwidth)}/s";

                //LastUploadBandwidth = upload;
                LastDownloadBandwidth = download;
                Refresh();
            }
            catch (Exception)
            {
            }
        }

        public void OnBandwidthUpdated(long upload, long download)
        {
            try
            {
                if (upload < 1 || download < 1)
                {
                    return;
                }
                UsedBandwidthLabel.Text = $"{i18N.Translate("Used")}{i18N.Translate(": ")}{Bandwidth.Compute(upload + download)}";
                UploadSpeedLabel.Text = $"↑: {Bandwidth.Compute(upload - LastUploadBandwidth)}/s";
                DownloadSpeedLabel.Text = $"↓: {Bandwidth.Compute(download - LastDownloadBandwidth)}/s";

                LastUploadBandwidth = upload;
                LastDownloadBandwidth = download;
                Refresh();
            }
            catch (Exception)
            {
            }
        }

        private void ProfileButton_Click(object sender, EventArgs e)
        {
            var index = ProfileButtons.IndexOf((Button)sender);

            //Utils.Logging.Info(String.Format("Button no.{0} clicked", index));

            if (ModifierKeys == Keys.Control)
            {
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(i18N.Translate("Please select a server first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBox.Show(i18N.Translate("Please select an mode first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (ProfileNameText.Text == "")
                {
                    MessageBox.Show(i18N.Translate("Please enter a profile name first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SaveProfile(index);
                    ProfileButtons[index].Text = ProfileNameText.Text;
                }
            }
            else
            {
                if (ProfileButtons[index].Text == i18N.Translate("Error") || ProfileButtons[index].Text == i18N.Translate("None"))
                {
                    MessageBox.Show(i18N.Translate("No saved profile here. Save a profile first by Ctrl+Click on the button"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                try
                {
                    ProfileNameText.Text = LoadProfile(index);

                    // start the profile
                    var need2ndStart = true;
                    if (State == State.Waiting || State == State.Stopped)
                    {
                        need2ndStart = false;
                    }

                    ControlButton.PerformClick();

                    if (need2ndStart)
                    {
                        Task.Run(() =>
                        {
                            while (State != State.Stopped)
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
                        Logging.Info(ee.Message);
                        ProfileButtons[index].Text = i18N.Translate("Error");
                        Thread.Sleep(1200);
                        ProfileButtons[index].Text = i18N.Translate("None");
                    });
                }
            }
        }

        // init at MainFrom_Load()
        private int SizeHeight;
        private int ControllHeight;
        private int ProfileBoxHeight;
        private int CFGBoxHeight;


        public void InitProfile()
        {

            foreach (var button in ProfileButtons)
            {
                button.Dispose();
            }
            ProfileButtons.Clear();
            ProfileTable.ColumnStyles.Clear();
            ProfileTable.RowStyles.Clear();

            var numProfile = Global.Settings.ProfileCount;
            if (numProfile == 0)
            {
                configLayoutPanel.RowStyles[2].SizeType = SizeType.Percent;
                configLayoutPanel.RowStyles[2].Height = 0;
                ProfileGroupBox.Visible = false;

                ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, CFGBoxHeight - ControllHeight);
                Size = new Size(Size.Width, SizeHeight - (ControllHeight + ProfileBoxHeight));

                return;
            }

            configLayoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
            ProfileGroupBox.Visible = true;
            ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, CFGBoxHeight);
            Size = new Size(Size.Width, SizeHeight);


            ProfileTable.ColumnCount = numProfile;

            while (Global.Settings.Profiles.Count < numProfile)
            {
                Global.Settings.Profiles.Add(new Profile());
            }

            // buttons
            for (var i = 0; i < numProfile; ++i)
            {
                var b = new Button();
                ProfileTable.Controls.Add(b, i, 0);
                b.Location = new Point(i * 100, 0);
                b.Click += ProfileButton_Click;
                b.Dock = DockStyle.Fill;
                b.Text = !Global.Settings.Profiles[i].IsDummy ? Global.Settings.Profiles[i].ProfileName : i18N.Translate("None");

                ProfileButtons.Add(b);
            }

            // equal column
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
            var p = Global.Settings.Profiles[index];

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

            Global.Settings.Profiles[index] = new Profile(selectedServer, selectedMode, name);

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
                    var process = new Process(selectedMode);
                    process.Text = "Edit Process Mode";
                    process.Show();
                    Hide();
                }
            }
            else
            {
                MessageBox.Show(i18N.Translate("Please select an mode first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Configuration.Save();
            }
            else
            {
                MessageBox.Show(i18N.Translate("Please select an mode first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    Clipboard.SetText(ShareLink.GetShareLink(selectedMode));
                }
                catch (Exception)
                {

                }
            }
            else
            {
                MessageBox.Show(i18N.Translate("Please select a server first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        public void NatTypeStatusText(string text)
        {
            NatTypeStatusLabel.Visible = true;
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();
                NatTypeStatusLabel.Text = "NAT" + i18N.Translate(": ") + text;
            }
            else
            {
                NatTypeStatusLabel.Text = "NAT" + i18N.Translate(": ") + i18N.Translate("Test failed");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 已启动
            if (State != State.Waiting && State != State.Stopped)
            {
                // 未设置自动停止
                if (!Global.Settings.StopWhenExited)
                {
                    MessageBox.Show(i18N.Translate("Please press Stop button first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Visible = true;
                    ShowInTaskbar = true;
                    WindowState = FormWindowState.Normal;
                    NotifyIcon.Visible = true;

                    return;
                }

                // 自动停止
                ControlButton_Click(sender, e);
            }

            SaveConfigs();

            State = State.Terminating;
            NotifyIcon.Visible = false;
            Close();
            Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        private void updateACLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StatusText(i18N.Translate("Starting update ACL"));
            using var client = new WebClient();

            client.DownloadFileTaskAsync(Global.Settings.ACL, "bin\\default.acl");
            client.DownloadFileCompleted += ((sender, args) =>
            {
                try
                {

                    if (args.Error == null)
                    {
                        NotifyIcon.ShowBalloonTip(5,
                                UpdateChecker.Name, i18N.Translate("ACL updated successfully"),
                                ToolTipIcon.Info);
                    }
                    else
                    {
                        Logging.Info("ACL 更新失败！" + args.Error);
                        MessageBox.Show(i18N.Translate("ACL update failed") + "\n" + args.Error);
                    }
                }
                finally
                {
                    UpdateStatus(State.Waiting);
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
                    MessageBox.Show(i18N.Translate("Please select a server first"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                using var client = new WebClient();

                client.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                StatusText(i18N.Translate("Updating in the background"));
                try
                {
                    client.DownloadFile(Global.Settings.ACL, "bin\\default.acl");
                    NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name, i18N.Translate("ACL updated successfully"),
                            ToolTipIcon.Info);
                }
                catch (Exception e)
                {
                    Logging.Info("使用代理更新 ACL 失败！" + e.Message);
                    MessageBox.Show(i18N.Translate("ACL update failed") + "\n" + e.Message);
                }
                finally
                {
                    updateACLWithProxyToolStripMenuItem.Enabled = true;
                    MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;

                    UpdateStatus(State.Waiting);
                    MainController.Stop();
                }
            });
        }

        private void reinstallTapDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                StatusText(i18N.Translate("Reinstalling TUN/TAP driver"));
                Enabled = false;
                try
                {
                    Configuration.deltapall();
                    Configuration.addtap();
                    NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name, i18N.Translate("Reinstall TUN/TAP driver successfully"),
                            ToolTipIcon.Info);
                }
                catch
                {
                    NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name, i18N.Translate("Reinstall TUN/TAP driver failed"),
                            ToolTipIcon.Error);
                }
                finally
                {
                    UpdateStatus(State.Waiting);
                    Enabled = true;
                }
            });
        }

        private void RelyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://mega.nz/file/9OQ1EazJ#0pjJ3xt57AVLr29vYEEv15GSACtXVQOGlEOPpi_2Ico");
        }

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible)
                return;

            if (i18N.LangCode != Global.Settings.Language)
            {
                i18N.Load(Global.Settings.Language);
                InitText(State == State.Started);
                InitProfile();
            }

            if (ProfileButtons.Count != Global.Settings.ProfileCount)
                InitProfile();
        }

        public void StatusText(string text)
        {
            StatusLabel.Text = i18N.Translate("Status", ": ") + text;
        }

        public void UpdateStatus(State state)
        {
            switch (state)
            {
                case State.Starting:
                    ControlButton.Text = "...";
                    ControlButton.Enabled = false;

                    ServerComboBox.Enabled = false;
                    ModeComboBox.Enabled = false;

                    UninstallServiceToolStripMenuItem.Enabled = false;
                    updateACLWithProxyToolStripMenuItem.Enabled = false;
                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = false;
                    reinstallTapDriverToolStripMenuItem.Enabled = false;
                    break;
                case State.Waiting:
                    ControlButton.Text = i18N.Translate("Start");
                    ControlButton.Enabled = true;
                    break;
                case State.Started:
                    ControlButton.Text = i18N.Translate("Stop");
                    ControlButton.Enabled = true;
                    break;
                case State.Stopping:
                    NatTypeStatusLabel.Visible = false;
                    UsedBandwidthLabel.Visible = UploadSpeedLabel.Visible = DownloadSpeedLabel.Visible = false;

                    ControlButton.Enabled = false;
                    ControlButton.Text = "...";
                    break;
                case State.Stopped:
                    LastUploadBandwidth = 0;
                    LastDownloadBandwidth = 0;

                    UploadSpeedLabel.Text = "↑: 0 KB/s";
                    DownloadSpeedLabel.Text = "↓: 0 KB/s";
                    UsedBandwidthLabel.Text = $"{i18N.Translate("Used")}{i18N.Translate(": ")}0 KB";

                    ServerComboBox.Enabled = true;
                    ModeComboBox.Enabled = true;
                    ControlButton.Text = i18N.Translate("Start");
                    ControlButton.Enabled = true;
                    ProfileGroupBox.Enabled = true;

                    UninstallServiceToolStripMenuItem.Enabled = true;
                    updateACLWithProxyToolStripMenuItem.Enabled = true;
                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true;
                    reinstallTapDriverToolStripMenuItem.Enabled = true;
                    break;
                case State.Terminating:

                    break;
            }

            State = state;
            StatusText(i18N.Translate(StateExtension.GetStatusString(state)));
        }

        public void UpdateStatus()
        {
            UpdateStatus(State);
        }
    }
}
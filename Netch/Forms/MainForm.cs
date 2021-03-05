using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Netch.Controllers;
using Netch.Forms.Mode;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class MainForm : Form
    {
        private void createRouteTableModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hide();
            new Route().ShowDialog();
            Show();
        }

        #region Start

        private readonly Dictionary<string, object> _mainFormText = new();

        private bool _comboBoxInitialized;
        private bool _textRecorded;

        public MainForm()
        {
            InitializeComponent();
            NotifyIcon.Icon = Icon = Resources.icon;

            AddAddServerToolStripMenuItems();

            #region i18N Translations

            _mainFormText.Add(UninstallServiceToolStripMenuItem.Name, new[] {"Uninstall {0}", "NF Service"});
            _mainFormText.Add(UninstallTapDriverToolStripMenuItem.Name, new[] {"Uninstall {0}", "TUN/TAP driver"});

            #endregion

            // 监听电源事件
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            ModeComboBox.KeyUp += (_, args) =>
            {
                switch (args.KeyData)
                {
                    case Keys.Escape:
                    {
                        SelectLastMode();
                        return;
                    }
                }
            };

            CheckForIllegalCrossThreadCalls = false;
        }

        private void AddAddServerToolStripMenuItems()
        {
            foreach (var serversUtil in ServerHelper.ServerUtils.Where(i => !string.IsNullOrEmpty(i.FullName)))
            {
                var fullName = serversUtil.FullName;
                var control = new ToolStripMenuItem
                {
                    Name = $"Add{fullName}ServerToolStripMenuItem",
                    Size = new Size(259, 22),
                    Text = i18N.TranslateFormat("Add [{0}] Server", fullName)
                };

                _mainFormText.Add(control.Name, new[] {"Add [{0}] Server", fullName});
                control.Click += AddServerToolStripMenuItem_Click;
                ServerToolStripMenuItem.DropDownItems.Add(control);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 计算 ComboBox绘制 目标宽度
            RecordSize();

            LoadServers();
            ServerHelper.DelayTestHelper.UpdateInterval();

            ModeHelper.Load();
            LoadModes();
            _comboBoxInitialized = true;

            // 加载翻译
            TranslateControls();

            // 隐藏 NatTypeStatusLabel
            NatTypeStatusText();

            // 加载快速配置
            LoadProfiles();

            Task.Run(() =>
            {
                // 检查更新
                if (Global.Settings.CheckUpdateWhenOpened)
                    CheckUpdate();
            });

            Task.Run(() =>
            {
                // 检查订阅更新
                if (Global.Settings.UpdateServersWhenOpened)
                    UpdateServersFromSubscribe(Global.Settings.UseProxyToUpdateSubscription).Wait();

                // 打开软件时启动加速，产生开始按钮点击事件
                if (Global.Settings.StartWhenOpened)
                    ControlButton_Click(null, null);
            });
        }

        private void RecordSize()
        {
            _numberBoxWidth = ServerComboBox.Width / 10;
            _numberBoxX = _numberBoxWidth * 9;
            _numberBoxWrap = _numberBoxWidth / 30;

            _configurationGroupBoxHeight = ConfigurationGroupBox.Height;
            _profileConfigurationHeight = ConfigurationGroupBox.Controls[0].Height / 3; // 因为 AutoSize, 所以得到的是Controls的总高度
            _profileGroupBoxPaddingHeight = ProfileGroupBox.Height - ProfileTable.Height;
            _profileTableHeight = ProfileTable.Height;
        }

        private void TranslateControls()
        {
            #region Record English

            if (!_textRecorded)
            {
                void RecordText(Component component)
                {
                    try
                    {
                        switch (component)
                        {
                            case TextBoxBase:
                            case ListControl:
                                break;
                            case Control c:
                                _mainFormText.Add(c.Name, c.Text);
                                break;
                            case ToolStripItem c:
                                _mainFormText.Add(c.Name, c.Text);
                                break;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // ignored
                    }
                }

                Utils.Utils.ComponentIterator(this, RecordText);
                Utils.Utils.ComponentIterator(NotifyMenu, RecordText);
                _textRecorded = true;
            }

            #endregion

            #region Translate

            void TranslateText(Component component)
            {
                switch (component)
                {
                    case TextBoxBase:
                    case ListControl:
                        break;
                    case Control c:
                        if (_mainFormText.ContainsKey(c.Name))
                            c.Text = ControlText(c.Name);

                        break;
                    case ToolStripItem c:
                        if (_mainFormText.ContainsKey(c.Name))
                            c.Text = ControlText(c.Name);

                        break;
                }

                string ControlText(string name)
                {
                    var value = _mainFormText[name];
                    if (value.Equals(string.Empty))
                        return string.Empty;

                    if (value is object[] values)
                        return i18N.TranslateFormat((string) values.First(), values.Skip(1).ToArray());

                    return i18N.Translate(value);
                }
            }

            Utils.Utils.ComponentIterator(this, TranslateText);
            Utils.Utils.ComponentIterator(NotifyMenu, TranslateText);

            #endregion

            UsedBandwidthLabel.Text = $@"{i18N.Translate("Used", ": ")}0 KB";
            State = State;
            VersionLabel.Text = UpdateChecker.Version;
        }

        #endregion

        #region Controls

        #region MenuStrip

        #region Server

        private void ImportServersFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var texts = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texts))
            {
                var servers = ShareLink.ParseText(texts);
                Global.Settings.Server.AddRange(servers);
                NotifyTip(i18N.TranslateFormat("Import {0} server(s) form Clipboard", servers.Count));

                LoadServers();
                Configuration.Save();
            }
        }

        private void AddServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var s = ((ToolStripMenuItem) sender).Text;

            var start = s.IndexOf("[", StringComparison.Ordinal) + 1;
            var end = s.IndexOf("]", start, StringComparison.Ordinal);
            var result = s.Substring(start, end - start);

            Hide();
            ServerHelper.GetUtilByFullName(result).Create();

            LoadServers();
            Configuration.Save();
            Show();
        }

        #endregion

        #region Mode

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            Hide();
            new Process().ShowDialog();
            Show();
        }

        private void ReloadModesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            try
            {
                ModeHelper.Load();
                LoadModes();
                NotifyTip(i18N.Translate("Modes have been reload"));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                Enabled = true;
            }
        }

        #endregion

        #region Subscription

        private void ManageSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hide();
            new SubscribeForm().ShowDialog();
            LoadServers();
            Show();
        }

        private async void UpdateServersFromSubscribeLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Settings.UseProxyToUpdateSubscription = false;
            await UpdateServersFromSubscribe();
        }

        private async void UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Settings.UseProxyToUpdateSubscription = true;
            await UpdateServersFromSubscribe(true);
        }

        private async Task UpdateServersFromSubscribe(bool useProxy = false)
        {
            void DisableItems(bool v)
            {
                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ProfileGroupBox.Enabled = ControlButton.Enabled = v;
            }

            var server = ServerComboBox.SelectedItem as Server;
            if (useProxy)
            {
                if (server == null)
                {
                    MessageBoxX.Show(i18N.Translate("Please select a server first"));
                    return;
                }
            }

            if (Global.Settings.SubscribeLink.Count <= 0)
            {
                MessageBoxX.Show(i18N.Translate("No subscription link"));
                return;
            }

            StatusText(i18N.Translate("Starting update subscription"));
            DisableItems(false);
            try
            {
                string? proxyServer = null;
                if (useProxy)
                {
                    var mode = new Models.Mode
                    {
                        Remark = "ProxyUpdate",
                        Type = 5
                    };

                    await MainController.StartAsync(server!, mode);
                    proxyServer = $"http://127.0.0.1:{Global.Settings.HTTPLocalPort}";
                }

                await Subscription.UpdateServersAsync(proxyServer);

                LoadServers();
                Configuration.Save();
                StatusText(i18N.Translate("Subscription updated"));
            }
            catch (Exception e)
            {
                NotifyTip(i18N.Translate("update servers failed") + "\n" + e.Message, info: false);
                Logging.Error("更新服务器 失败！" + e);
            }
            finally
            {
                if (useProxy)
                    await MainController.StopAsync();

                DisableItems(true);
            }
        }

        #endregion

        #region Options

        private void CheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                void OnNewVersionNotFound(object o, EventArgs args)
                {
                    NotifyTip(i18N.Translate("Already latest version"));
                }

                void OnNewVersionFoundFailed(object o, EventArgs args)
                {
                    NotifyTip(i18N.Translate("New version found failed"), info: false);
                }

                try
                {
                    UpdateChecker.NewVersionNotFound += OnNewVersionNotFound;
                    UpdateChecker.NewVersionFoundFailed += OnNewVersionFoundFailed;
                    CheckUpdate();
                }
                finally
                {
                    UpdateChecker.NewVersionNotFound -= OnNewVersionNotFound;
                    UpdateChecker.NewVersionFoundFailed -= OnNewVersionFoundFailed;
                }
            });
        }

        private void OpenDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open(".\\");
        }

        private async void CleanDNSCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    NativeMethods.FlushDNSResolverCache();
                    DnsUtils.ClearCache();
                });

                NotifyTip(i18N.Translate("DNS cache cleanup succeeded"));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                StatusText();
            }
        }

        private void updateACLWithProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateACL(true);
        }

        private void updateACLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateACL(false);
        }

        private async void UpdateACL(bool useProxy)
        {
            Enabled = false;
            StatusText(i18N.TranslateFormat("Updating {0}", "ACL"));
            try
            {
                if (useProxy)
                {
                    if (!(ServerComboBox.SelectedItem is Server server))
                    {
                        MessageBoxX.Show(i18N.Translate("Please select a server first"));
                        return;
                    }
                    else
                    {
                        var mode = new Models.Mode
                        {
                            Remark = "ProxyUpdate",
                            Type = 5
                        };

                        await MainController.StartAsync(server, mode);
                    }
                }

                var req = WebUtil.CreateRequest(Global.Settings.ACL);
                if (useProxy)
                    req.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                await WebUtil.DownloadFileAsync(req, Path.Combine(Global.NetchDir, Global.UserACL));
                NotifyTip(i18N.Translate("ACL updated successfully"));
            }
            catch (Exception e)
            {
                NotifyTip(i18N.Translate("ACL update failed") + "\n" + e.Message, info: false);
                Logging.Error("更新 ACL 失败！" + e);
            }
            finally
            {
                if (useProxy)
                    await MainController.StopAsync();

                StatusText();
                Enabled = true;
            }
        }

        private async void updatePACToolStripMenuItem_Click(object sender, EventArgs eventArgs)
        {
            Enabled = false;

            StatusText(i18N.TranslateFormat("Updating {0}", "PAC"));
            try
            {
                var req = WebUtil.CreateRequest(Global.Settings.PAC);

                var pac = Path.Combine(Global.NetchDir, "bin\\pac.txt");

                await WebUtil.DownloadFileAsync(req, pac);

                NotifyTip(i18N.Translate("PAC updated successfully"));
            }
            catch (Exception e)
            {
                NotifyTip(i18N.Translate("PAC update failed") + "\n" + e.Message, info: false);
                Logging.Error("更新 PAC 失败！" + e);
            }
            finally
            {
                StatusText();
                Enabled = true;
            }
        }

        private async void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.TranslateFormat("Uninstalling {0}", "NF Service"));
            try
            {
                await Task.Run(() =>
                {
                    if (NFController.UninstallDriver())
                        NotifyTip(i18N.TranslateFormat("{0} has been uninstalled", "NF Service"));
                });
            }
            finally
            {
                StatusText();
                Enabled = true;
            }
        }

        private async void UninstallTapDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.TranslateFormat("Uninstalling {0}", "TUN/TAP driver"));
            try
            {
                await Task.Run(TUNTAP.deltapall);
                NotifyTip(i18N.TranslateFormat("{0} has been uninstalled", "TUN/TAP driver"));
            }
            catch (Exception exception)
            {
                Logging.Error($"卸载 TUN/TAP 适配器失败: {exception}");
            }
            finally
            {
                StatusText();
                Enabled = true;
            }
        }

        private void RemoveNetchFirewallRulesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Firewall.RemoveNetchFwRules();
        }

        #endregion

        /// <summary>
        ///     菜单栏强制退出
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit(true);
        }

        private void VersionLabel_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open($"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases");
        }

        private async void NewVersionLabel_Click(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Control || !UpdateChecker.LatestRelease!.assets.Any())
            {
                Utils.Utils.Open(UpdateChecker.LatestVersionUrl!);
                return;
            }

            if (MessageBoxX.Show(i18N.Translate("Download and install now?"), confirm: true) != DialogResult.OK)
                return;

            NotifyTip(i18N.Translate("Start downloading new version"));
            NewVersionLabel.Enabled = false;
            NewVersionLabel.Text = "...";

            try
            {
                await Task.Run(() =>
                {
                    Updater.Updater.DownloadAndUpdate(Path.Combine(Global.NetchDir, "data"),
                        Global.NetchDir,
                        (_, args) => BeginInvoke(new Action(() => NewVersionLabel.Text = $"{args.ProgressPercentage}%")));
                });
            }
            catch (Exception exception)
            {
                if (exception is not MessageException)
                {
                    Logging.Error($"更新失败: {exception}");
                    Utils.Utils.Open(Logging.LogFile);
                }

                NotifyTip(exception.Message, info: false);
            }
            finally
            {
                NewVersionLabel.Visible = false;
                NewVersionLabel.Enabled = true;
            }
        }

        private void AboutToolStripButton_Click(object sender, EventArgs e)
        {
            Hide();
            new AboutForm().ShowDialog();
            Show();
        }

        private void fAQToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.Utils.Open("https://netch.org/#/docs/zh-CN/faq");
        }

        #endregion

        #region ControlButton

        private async void ControlButton_Click(object? sender, EventArgs? e)
        {
            if (!IsWaiting())
            {
                // 停止
                State = State.Stopping;
                await MainController.StopAsync();
                State = State.Stopped;
                return;
            }

            Configuration.Save();

            // 服务器、模式 需选择
            if (!(ServerComboBox.SelectedItem is Server server))
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            if (!(ModeComboBox.SelectedItem is Models.Mode mode))
            {
                MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                return;
            }

            // 清除模式搜索框文本选择
            ModeComboBox.Select(0, 0);

            State = State.Starting;

            try
            {
                await MainController.StartAsync(server, mode);
            }
            catch (Exception exception)
            {
                State = State.Stopped;
                StatusText(i18N.Translate("Start failed"));
                MessageBoxX.Show(exception.Message);
                return;
            }

            State = State.Started;

            _ = Task.Run(Bandwidth.NetTraffic);
            _ = Task.Run(NatTest);

            if (Global.Settings.MinimizeWhenStarted)
                Minimize();

            // 自动检测延迟
            _ = Task.Run(() =>
            {
                while (State == State.Started)
                {
                    bool StartedPingEnabled()
                    {
                        return Global.Settings.StartedPingInterval >= 0;
                    }

                    if (StartedPingEnabled())
                    {
                        server.Test();
                        ServerComboBox.Refresh();
                    }

                    if (StartedPingEnabled())
                        Thread.Sleep(Global.Settings.StartedPingInterval * 1000);
                    else
                        Thread.Sleep(5000);
                }
            });
        }

        #endregion

        #region SettingsButton

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            var oldSettings = Global.Settings.Clone();

            Hide();
            new SettingForm().ShowDialog();

            if (oldSettings.Language != Global.Settings.Language)
            {
                i18N.Load(Global.Settings.Language);
                TranslateControls();
                LoadModes();
                LoadProfiles();
            }

            if (oldSettings.DetectionTick != Global.Settings.DetectionTick)
                ServerHelper.DelayTestHelper.UpdateInterval();

            if (oldSettings.ProfileCount != Global.Settings.ProfileCount)
                LoadProfiles();

            Show();
        }

        #endregion

        #region Server

        private void LoadServers()
        {
            var comboBoxInitialized = _comboBoxInitialized;
            _comboBoxInitialized = false;

            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Settings.Server.ToArray());
            SelectLastServer();
            _comboBoxInitialized = comboBoxInitialized;
        }

        public void SelectLastServer()
        {
            // 如果值合法，选中该位置
            if (Global.Settings.ServerComboBoxSelectedIndex > 0 && Global.Settings.ServerComboBoxSelectedIndex < ServerComboBox.Items.Count)
                ServerComboBox.SelectedIndex = Global.Settings.ServerComboBoxSelectedIndex;
            // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
            else if (ServerComboBox.Items.Count > 0)
                ServerComboBox.SelectedIndex = 0;

            // 如果当前 ServerComboBox 中没元素，不做处理
        }

        private void ServerComboBox_SelectedIndexChanged(object sender, EventArgs o)
        {
            if (!_comboBoxInitialized)
                return;

            Global.Settings.ServerComboBoxSelectedIndex = ServerComboBox.SelectedIndex;
        }

        private void EditServerPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (!(ServerComboBox.SelectedItem is Server server))
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            if (!server.Valid())
                return;

            Hide();
            ServerHelper.GetUtilByTypeName(server.Type).Edit(server);
            LoadServers();
            Configuration.Save();
            Show();
        }

        private void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Testing"));

            if (!IsWaiting() || ModifierKeys == Keys.Control)
            {
                (ServerComboBox.SelectedItem as Server)?.Test();
                ServerComboBox.Refresh();
                Enabled = true;
                StatusText();
            }
            else
            {
                ServerHelper.DelayTestHelper.TestDelayFinished += OnTestDelayFinished;
                _ = Task.Run(ServerHelper.DelayTestHelper.TestAllDelay);

                void OnTestDelayFinished(object o1, EventArgs e1)
                {
                    Refresh();

                    ServerHelper.DelayTestHelper.TestDelayFinished -= OnTestDelayFinished;
                    Enabled = true;
                    StatusText();
                }
            }
        }

        private void CopyLinkPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (!(ServerComboBox.SelectedItem is Server server))
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            if (!server.Valid())
                return;

            try
            {
                //听说巨硬BUG经常会炸，所以Catch一下 :D
                string text;
                if (ModifierKeys == Keys.Control)
                    text = ShareLink.GetNetchLink(server);
                else
                    text = ShareLink.GetShareLink(server);

                Clipboard.SetText(text);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void DeleteServerPictureBox_Click(object sender, EventArgs e)
        {
            // 当前 ServerComboBox 中至少有一项
            if (!(ServerComboBox.SelectedItem is Server server))
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            Global.Settings.Server.Remove(server);
            LoadServers();
        }

        #endregion

        #region Mode

        public void LoadModes()
        {
            var comboBoxInitialized = _comboBoxInitialized;
            _comboBoxInitialized = false;

            ModeComboBox.Items.Clear();
            ModeComboBox.Items.AddRange(Global.Modes.ToArray());
            ModeComboBox.Tag = null;
            SelectLastMode();
            _comboBoxInitialized = comboBoxInitialized;
        }

        public void SelectLastMode()
        {
            // 如果值合法，选中该位置
            if (Global.Settings.ModeComboBoxSelectedIndex > 0 && Global.Settings.ModeComboBoxSelectedIndex < ModeComboBox.Items.Count)
                ModeComboBox.SelectedIndex = Global.Settings.ModeComboBoxSelectedIndex;
            // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
            else if (ModeComboBox.Items.Count > 0)
                ModeComboBox.SelectedIndex = 0;

            // 如果当前 ModeComboBox 中没元素，不做处理
        }

        private void ModeComboBox_SelectedIndexChanged(object sender, EventArgs o)
        {
            if (!_comboBoxInitialized)
                return;

            try
            {
                Global.Settings.ModeComboBoxSelectedIndex = Global.Modes.IndexOf((Models.Mode) ModeComboBox.SelectedItem);
            }
            catch
            {
                Global.Settings.ModeComboBoxSelectedIndex = 0;
            }
        }

        private void EditModePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ModeComboBox中至少有一项
            if (ModeComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                return;
            }

            var mode = (Models.Mode) ModeComboBox.SelectedItem;
            if (ModifierKeys == Keys.Control)
            {
                Utils.Utils.Open(ModeHelper.GetFullPath(mode.RelativePath!));
                return;
            }

            switch (mode.Type)
            {
                case 0:
                    Hide();
                    new Process(mode).ShowDialog();
                    Show();
                    break;
                case 1:
                case 2:
                    Hide();
                    new Route(mode).ShowDialog();
                    Show();
                    break;
                default:
                    Utils.Utils.Open(ModeHelper.GetFullPath(mode.RelativePath!));
                    break;
            }
        }

        private void DeleteModePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ModeComboBox中至少有一项
            if (ModeComboBox.Items.Count <= 0 || ModeComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                return;
            }

            ModeHelper.Delete((Models.Mode) ModeComboBox.SelectedItem);
            SelectLastMode();
        }

        #endregion

        #region Profile

        private int _configurationGroupBoxHeight;
        private int _profileConfigurationHeight;
        private int _profileGroupBoxPaddingHeight;
        private int _profileTableHeight;

        private void LoadProfiles()
        {
            // Clear
            foreach (var button in ProfileTable.Controls)
                ((Button) button).Dispose();

            ProfileTable.Controls.Clear();
            ProfileTable.ColumnStyles.Clear();
            ProfileTable.RowStyles.Clear();

            var profileCount = Global.Settings.ProfileCount;
            if (profileCount == 0)
            {
                // Hide Profile GroupBox, Change window size
                configLayoutPanel.RowStyles[2].SizeType = SizeType.Percent;
                configLayoutPanel.RowStyles[2].Height = 0;
                ProfileGroupBox.Visible = false;

                ConfigurationGroupBox.Height = _configurationGroupBoxHeight - _profileConfigurationHeight;
            }
            else
            {
                // Load Profiles

                if (Global.Settings.ProfileTableColumnCount == 0)
                    Global.Settings.ProfileTableColumnCount = 5;

                var columnCount = Global.Settings.ProfileTableColumnCount;

                ProfileTable.ColumnCount = profileCount >= columnCount ? columnCount : profileCount;
                ProfileTable.RowCount = (int) Math.Ceiling(profileCount / (float) columnCount);

                for (var i = 0; i < profileCount; ++i)
                {
                    var profile = Global.Settings.Profiles.SingleOrDefault(p => p.Index == i);
                    var b = new Button
                    {
                        Dock = DockStyle.Fill,
                        Text = profile?.ProfileName ?? i18N.Translate("None"),
                        Tag = profile
                    };

                    b.Click += ProfileButton_Click;
                    ProfileTable.Controls.Add(b, i % columnCount, i / columnCount);
                }

                // equal column
                for (var i = 1; i <= ProfileTable.RowCount; i++)
                    ProfileTable.RowStyles.Add(new RowStyle(SizeType.Percent, 1));

                for (var i = 1; i <= ProfileTable.ColumnCount; i++)
                    ProfileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));

                configLayoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
                ProfileGroupBox.Visible = true;
                ProfileGroupBox.Height = ProfileTable.RowCount * _profileTableHeight + _profileGroupBoxPaddingHeight;
                ConfigurationGroupBox.Height = _configurationGroupBoxHeight;
            }
        }

        private void ActiveProfile(Profile profile)
        {
            ProfileNameText.Text = profile.ProfileName;
            ModeComboBox.ResetCompletionList();

            var server = ServerComboBox.Items.Cast<Server>().FirstOrDefault(s => s.Remark.Equals(profile.ServerRemark));
            var mode = ModeComboBox.Items.Cast<Models.Mode>().FirstOrDefault(m => m.Remark.Equals(profile.ModeRemark));

            if (server == null)
                throw new Exception("Server not found.");

            if (mode == null)
                throw new Exception("Mode not found.");

            ServerComboBox.SelectedItem = server;
            ModeComboBox.SelectedItem = mode;
        }

        private Profile CreateProfileAtIndex(int index)
        {
            var server = (Server) ServerComboBox.SelectedItem;
            var mode = (Models.Mode) ModeComboBox.SelectedItem;
            var name = ProfileNameText.Text;

            Profile? profile;
            if ((profile = Global.Settings.Profiles.SingleOrDefault(p => p.Index == index)) != null)
                Global.Settings.Profiles.Remove(profile);

            profile = new Profile(server, mode, name, index);
            Global.Settings.Profiles.Add(profile);
            return profile;
        }

        private async void ProfileButton_Click(object sender, EventArgs e)
        {
            var profileButton = (Button) sender;
            var profile = (Profile?) profileButton.Tag;
            var index = ProfileTable.Controls.IndexOf(profileButton);

            switch (ModifierKeys)
            {
                case Keys.Control:
                    if (ServerComboBox.SelectedIndex == -1)
                    {
                        MessageBoxX.Show(i18N.Translate("Please select a server first"));
                        return;
                    }

                    if (ModeComboBox.SelectedIndex == -1)
                    {
                        MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                        return;
                    }

                    if (ProfileNameText.Text == "")
                    {
                        MessageBoxX.Show(i18N.Translate("Please enter a profile name first"));
                        ProfileNameText.Focus();
                        return;
                    }

                    profileButton.Tag = profile = CreateProfileAtIndex(index);
                    profileButton.Text = profile.ProfileName;
                    ProfileNameText.Clear();
                    return;
                case Keys.Shift:
                    if (profile == null)
                        return;

                    Global.Settings.Profiles.Remove(profile);
                    profileButton.Tag = null;
                    profileButton.Text = i18N.Translate("None");
                    return;
            }

            if (profile == null)
            {
                MessageBoxX.Show(i18N.Translate("No saved profile here. Save a profile first by Ctrl+Click on the button"));
                return;
            }

            try
            {
                ActiveProfile(profile);
            }
            catch (Exception exception)
            {
                MessageBoxX.Show(exception.Message, LogLevel.ERROR);
                return;
            }

            // start the profile
            ControlButton_Click(null, null);
            if (State == State.Stopping || State == State.Stopped)
            {
                while (State != State.Stopped)
                    await Task.Delay(250);

                ControlButton_Click(null, null);
            }
        }

        #endregion

        #region State

        private State _state = State.Waiting;

        /// <summary>
        ///     当前状态
        /// </summary>
        public State State
        {
            get => _state;
            private set
            {
                void StartDisableItems(bool enabled)
                {
                    ServerComboBox.Enabled = ModeComboBox.Enabled = EditModePictureBox.Enabled =
                        EditServerPictureBox.Enabled = DeleteModePictureBox.Enabled = DeleteServerPictureBox.Enabled = enabled;

                    // 启动需要禁用的控件
                    UninstallServiceToolStripMenuItem.Enabled = UpdateACLToolStripMenuItem.Enabled = updateACLWithProxyToolStripMenuItem.Enabled =
                        updatePACToolStripMenuItem.Enabled = UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled =
                            UpdateServersFromSubscribeLinksWithProxyToolStripMenuItem.Enabled = UninstallTapDriverToolStripMenuItem.Enabled =
                                ReloadModesToolStripMenuItem.Enabled = enabled;
                }

                _state = value;

                ServerHelper.DelayTestHelper.Enabled = IsWaiting(_state);

                StatusText();
                switch (value)
                {
                    case State.Waiting:
                        ControlButton.Enabled = true;
                        ControlButton.Text = i18N.Translate("Start");

                        break;
                    case State.Starting:
                        ControlButton.Enabled = false;
                        ControlButton.Text = "...";

                        ProfileGroupBox.Enabled = false;
                        StartDisableItems(false);
                        break;
                    case State.Started:
                        ControlButton.Enabled = true;
                        ControlButton.Text = i18N.Translate("Stop");

                        ProfileGroupBox.Enabled = true;

                        break;
                    case State.Stopping:
                        ControlButton.Enabled = false;
                        ControlButton.Text = "...";

                        ProfileGroupBox.Enabled = false;
                        BandwidthState(false);
                        NatTypeStatusText();
                        break;
                    case State.Stopped:
                        ControlButton.Enabled = true;
                        ControlButton.Text = i18N.Translate("Start");

                        LastUploadBandwidth = 0;
                        LastDownloadBandwidth = 0;
                        Bandwidth.Stop();

                        ProfileGroupBox.Enabled = true;
                        StartDisableItems(true);
                        break;
                }
            }
        }

        private bool IsWaiting()
        {
            return State == State.Waiting || State == State.Stopped;
        }

        private static bool IsWaiting(State state)
        {
            return state == State.Waiting || state == State.Stopped;
        }

        /// <summary>
        ///     更新状态栏文本
        /// </summary>
        /// <param name="text"></param>
        public void StatusText(string? text = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(StatusText), text);
                return;
            }

            text ??= i18N.Translate(StateExtension.GetStatusString(State));
            StatusLabel.Text = i18N.Translate("Status", ": ") + text;
            if (_state == State.Started)
                StatusLabel.Text += StatusPortInfoText.Value;
        }

        public void BandwidthState(bool state)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(BandwidthState), state);
                return;
            }

            if (IsWaiting())
                return;

            UsedBandwidthLabel.Visible /*= UploadSpeedLabel.Visible*/ = DownloadSpeedLabel.Visible = state;
        }

        public void NatTypeStatusText(string? text = null, string? country = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(NatTypeStatusText), text, country);
                return;
            }

            if (State != State.Started)
            {
                NatTypeStatusLabel.Text = "";
                NatTypeStatusLabel.Visible = NatTypeStatusLightLabel.Visible = false;
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                NatTypeStatusLabel.Text = $"NAT{i18N.Translate(": ")}{text} {(!country.IsNullOrEmpty() ? $"[{country}]" : "")}";

                UpdateNatTypeLight(int.TryParse(text, out var natType) ? natType : -1);
            }
            else
            {
                NatTypeStatusLabel.Text = $@"NAT{i18N.Translate(": ", "Test failed")}";
            }

            NatTypeStatusLabel.Visible = true;
        }

        /// <summary>
        ///     更新 NAT指示灯颜色
        /// </summary>
        /// <param name="natType"></param>
        private void UpdateNatTypeLight(int natType = -1)
        {
            if (natType > 0 && natType < 5)
            {
                NatTypeStatusLightLabel.Visible = Global.Flags.IsWindows10Upper;
                var c = natType switch
                        {
                            1 => Color.LimeGreen,
                            2 => Color.Yellow,
                            3 => Color.Red,
                            4 => Color.Black,
                            _ => throw new ArgumentOutOfRangeException(nameof(natType), natType, null)
                        };

                NatTypeStatusLightLabel.ForeColor = c;
            }
            else
            {
                NatTypeStatusLightLabel.Visible = false;
            }
        }

        private void NatTypeStatusLabel_Click(object sender, EventArgs e)
        {
            if (_state == State.Started && NttTested)
                NatTest();
        }

        private static bool NttTested;

        /// <summary>
        ///     测试 NAT
        /// </summary>
        public void NatTest()
        {
            if (!MainController.Mode!.TestNatRequired())
                return;

            NttTested = false;
            Task.Run(() =>
            {
                NatTypeStatusText(i18N.Translate("Starting NatTester"));

                var (result, localEnd, publicEnd) = MainController.NTTController.Start().Result;

                if (!string.IsNullOrEmpty(publicEnd))
                {
                    var country = Utils.Utils.GetCityCode(publicEnd!);
                    NatTypeStatusText(result, country);
                }
                else
                {
                    NatTypeStatusText(result ?? "Error");
                }

                NttTested = true;
            });
        }

        #endregion

        #endregion

        #region Misc

        #region PowerEvent

        private bool _resumeFlag;

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend: //操作系统即将挂起
                    if (!IsWaiting())
                    {
                        _resumeFlag = true;
                        Logging.Info("操作系统即将挂起，自动停止");
                        ControlButton_Click(null, null);
                    }

                    break;
                case PowerModes.Resume: //操作系统即将从挂起状态继续
                    if (_resumeFlag)
                    {
                        _resumeFlag = false;
                        Logging.Info("操作系统即将从挂起状态继续，自动重启");
                        ControlButton_Click(null, null);
                    }

                    break;
            }
        }

        #endregion

        private void Minimize()
        {
            // 使关闭时窗口向右下角缩小的效果
            WindowState = FormWindowState.Minimized;

            if (_isFirstCloseWindow)
            {
                // 显示提示语
                NotifyTip(i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."));
                _isFirstCloseWindow = false;
            }

            Hide();
        }

        public async void Exit(bool forceExit = false, bool saveConfiguration = true)
        {
            if (!IsWaiting() && !Global.Settings.StopWhenExited && !forceExit)
            {
                MessageBoxX.Show(i18N.Translate("Please press Stop button first"));

                NotifyIcon_MouseDoubleClick(null, null);
                return;
            }

            State = State.Terminating;
            NotifyIcon.Visible = false;
            Hide();

            if (saveConfiguration)
            {
                Configuration.Save();
            }

            foreach (var file in new[] {"data\\last.json", "data\\privoxy.conf"})
                if (File.Exists(file))
                    File.Delete(file);

            if (!IsWaiting())
                await MainController.StopAsync();

            Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        #region FormClosingButton

        private bool _isFirstCloseWindow = true;

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && State != State.Terminating)
            {
                // 取消"关闭窗口"事件
                e.Cancel = true; // 取消关闭窗体 

                // 如果未勾选关闭窗口时退出，隐藏至右下角托盘图标
                if (!Global.Settings.ExitWhenClosed)
                    Minimize();
                // 如果勾选了关闭时退出，自动点击退出按钮
                else
                    Exit();
            }
        }

        #endregion

        #region Updater

        private void CheckUpdate()
        {
            try
            {
                UpdateChecker.NewVersionFound += OnUpdateCheckerOnNewVersionFound;
                UpdateChecker.Check(Global.Settings.CheckBetaUpdate).Wait();
            }
            finally
            {
                UpdateChecker.NewVersionFound -= OnUpdateCheckerOnNewVersionFound;
            }

            void OnUpdateCheckerOnNewVersionFound(object o, EventArgs eventArgs)
            {
                NotifyTip($"{i18N.Translate(@"New version available", ": ")}{UpdateChecker.LatestVersionNumber}");
                NewVersionLabel.Text = i18N.Translate("New version available");
                NewVersionLabel.Enabled = true;
                NewVersionLabel.Visible = true;
            }
        }

        #endregion

        #region NetTraffic

        /// <summary>
        ///     上一次下载的流量
        /// </summary>
        public ulong LastDownloadBandwidth;

        /// <summary>
        ///     上一次上传的流量
        /// </summary>
        public ulong LastUploadBandwidth;

        public void OnBandwidthUpdated(ulong download)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<ulong>(OnBandwidthUpdated), download);
                return;
            }

            try
            {
                UsedBandwidthLabel.Text = $"{i18N.Translate("Used", ": ")}{Bandwidth.Compute(download)}";
                //UploadSpeedLabel.Text = $"↑: {Utils.Bandwidth.Compute(upload - LastUploadBandwidth)}/s";
                DownloadSpeedLabel.Text = $"↑↓: {Bandwidth.Compute(download - LastDownloadBandwidth)}/s";

                //LastUploadBandwidth = upload;
                LastDownloadBandwidth = download;
                Refresh();
            }
            catch
            {
                // ignored
            }
        }

        #endregion

        #region NotifyIcon

        private void ShowMainFormToolStripButton_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                ShowInTaskbar = true;                 // 显示在系统任务栏 
                WindowState = FormWindowState.Normal; // 还原窗体 
            }

            Activate();
        }

        /// <summary>
        ///     通知图标右键菜单退出
        /// </summary>
        private void ExitToolStripButton_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void NotifyIcon_MouseDoubleClick(object? sender, MouseEventArgs? e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                ShowInTaskbar = true;                 //显示在系统任务栏 
                WindowState = FormWindowState.Normal; //还原窗体
            }

            Activate();
        }

        public void NotifyTip(string text, int timeout = 0, bool info = true)
        {
            // 会阻塞线程 timeout 秒
            NotifyIcon.ShowBalloonTip(timeout, UpdateChecker.Name, text, info ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        #endregion

        #region ComboBox_DrawItem

        private readonly SolidBrush _greenBrush = new(Color.FromArgb(50, 255, 56));
        private int _numberBoxWidth;
        private int _numberBoxX;
        private int _numberBoxWrap;

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (!(sender is ComboBox cbx))
                return;

            // 绘制背景颜色
            e.Graphics.FillRectangle(Brushes.White, e.Bounds);

            if (e.Index < 0)
                return;

            // 绘制 备注/名称 字符串
            TextRenderer.DrawText(e.Graphics, cbx.Items[e.Index].ToString(), cbx.Font, e.Bounds, Color.Black, TextFormatFlags.Left);

            switch (cbx.Items[e.Index])
            {
                case Server item:
                {
                    // 计算延迟底色
                    var numBoxBackBrush = item.Delay switch {> 200 => Brushes.Red, > 80 => Brushes.Yellow, >= 0 => _greenBrush, _ => Brushes.Gray};

                    // 绘制延迟底色
                    e.Graphics.FillRectangle(numBoxBackBrush, _numberBoxX, e.Bounds.Y, _numberBoxWidth, e.Bounds.Height);

                    // 绘制延迟字符串
                    TextRenderer.DrawText(e.Graphics,
                        item.Delay.ToString(),
                        cbx.Font,
                        new Point(_numberBoxX + _numberBoxWrap, e.Bounds.Y),
                        Color.Black,
                        TextFormatFlags.Left);

                    break;
                }
                case Models.Mode item:
                {
                    // 绘制 模式Box 底色
                    e.Graphics.FillRectangle(Brushes.Gray, _numberBoxX, e.Bounds.Y, _numberBoxWidth, e.Bounds.Height);

                    // 绘制 模式行数 字符串
                    TextRenderer.DrawText(e.Graphics,
                        item.Rule.Count.ToString(),
                        cbx.Font,
                        new Point(_numberBoxX + _numberBoxWrap, e.Bounds.Y),
                        Color.Black,
                        TextFormatFlags.Left);

                    break;
                }
            }
        }

        #endregion

        #endregion
    }
}
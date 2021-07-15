using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Netch.Controllers;
using Netch.Enums;
using Netch.Forms.ModeForms;
using Netch.Interfaces;
using Netch.Models;
using Netch.Properties;
using Netch.Services;
using Netch.Utils;
using Serilog;

namespace Netch.Forms
{
    public partial class MainForm : Form
    {
        #region Start

        private readonly Dictionary<string, object> _mainFormText = new();

        private bool _textRecorded;

        public MainForm()
        {
            InitializeComponent();
            NotifyIcon.Icon = Icon = Resources.icon;

            AddAddServerToolStripMenuItems();

            #region i18N Translations

            _mainFormText.Add(UninstallServiceToolStripMenuItem.Name, new[] { "Uninstall {0}", "NF Service" });

            #endregion

            // 监听电源事件
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void AddAddServerToolStripMenuItems()
        {
            foreach (var serversUtil in ServerHelper.ServerUtilDictionary.Values.OrderBy(i => i.Priority)
                .Where(i => !string.IsNullOrEmpty(i.FullName)))
            {
                var fullName = serversUtil.FullName;
                var control = new ToolStripMenuItem
                {
                    Name = $"Add{fullName}ServerToolStripMenuItem",
                    Size = new Size(259, 22),
                    Text = i18N.TranslateFormat("Add [{0}] Server", fullName),
                    Tag = serversUtil
                };

                _mainFormText.Add(control.Name, new[] { "Add [{0}] Server", fullName });
                control.Click += AddServerToolStripMenuItem_Click;
                ServerToolStripMenuItem.DropDownItems.Add(control);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 计算 ComboBox绘制 目标宽度
            RecordSize();

            LoadServers();
            SelectLastServer();
            ServerHelper.DelayTestHelper.UpdateInterval();

            ModeHelper.InitWatcher();
            ModeHelper.Load();
            LoadModes();
            SelectLastMode();

            // 加载翻译
            TranslateControls();

            // 隐藏 NatTypeStatusLabel
            NatTypeStatusText();

            // 加载快速配置
            LoadProfiles();

            // 检查更新
            if (Global.Settings.CheckUpdateWhenOpened)
                CheckUpdateAsync().Forget();

            // 检查订阅更新
            if (Global.Settings.UpdateServersWhenOpened)
                UpdateServersFromSubscribeAsync().Forget();

            // 打开软件时启动加速，产生开始按钮点击事件
            if (Global.Settings.StartWhenOpened)
                ControlButton.PerformClick();

            Netch.SingleInstance.ListenForArgumentsFromSuccessiveInstances();
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
                        return i18N.TranslateFormat((string)values.First(), values.Skip(1).ToArray());

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

        private async void ImportServersFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var texts = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texts))
            {
                var servers = ShareLink.ParseText(texts);
                Global.Settings.Server.AddRange(servers);
                NotifyTip(i18N.TranslateFormat("Import {0} server(s) form Clipboard", servers.Count));

                LoadServers();
                await Configuration.SaveAsync();
            }
        }

        private async void AddServerToolStripMenuItem_Click([NotNull] object? sender, EventArgs? e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            var util = (IServerUtil)((ToolStripMenuItem)sender).Tag;

            Hide();
            util.Create();

            LoadServers();
            await Configuration.SaveAsync();
            Show();
        }

        #endregion

        #region Mode

        private void CreateProcessModeToolStripButton_Click(object sender, EventArgs e)
        {
            Hide();
            new ProcessForm().ShowDialog();
            Show();
        }

        private void createRouteTableModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hide();
            new RouteForm().ShowDialog();
            Show();
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
            await UpdateServersFromSubscribeAsync();
        }

        private async Task UpdateServersFromSubscribeAsync()
        {
            void DisableItems(bool v)
            {
                MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ProfileGroupBox.Enabled = ControlButton.Enabled = v;
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
                await Subscription.UpdateServersAsync();

                LoadServers();
                await Configuration.SaveAsync();
                StatusText(i18N.Translate("Subscription updated"));
            }
            catch (Exception e)
            {
                NotifyTip(i18N.Translate("update servers failed") + "\n" + e.Message, info: false);
                Log.Error("更新服务器 失败！" + e);
            }
            finally
            {
                DisableItems(true);
            }
        }

        #endregion

        #region Options

        private async void CheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            void OnNewVersionNotFound(object? o, EventArgs? args)
            {
                NotifyTip(i18N.Translate("Already latest version"));
            }

            void OnNewVersionFoundFailed(object? o, EventArgs? args)
            {
                NotifyTip(i18N.Translate("New version found failed"), info: false);
            }

            try
            {
                UpdateChecker.NewVersionNotFound += OnNewVersionNotFound;
                UpdateChecker.NewVersionFoundFailed += OnNewVersionFoundFailed;
                await CheckUpdateAsync();
            }
            finally
            {
                UpdateChecker.NewVersionNotFound -= OnNewVersionNotFound;
                UpdateChecker.NewVersionFoundFailed -= OnNewVersionFoundFailed;
            }
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
                    NativeMethods.RefreshDNSCache();
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

        private async void UninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.TranslateFormat("Uninstalling {0}", "NF Service"));
            try
            {
                var task = Task.Run(NFController.UninstallDriver);

                if (await task)
                    NotifyTip(i18N.TranslateFormat("{0} has been uninstalled", "NF Service"));
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

        private void ShowHideConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var windowStyles = (WINDOW_STYLE)PInvoke.GetWindowLong(new HWND(Netch.ConsoleHwnd), WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            var visible = windowStyles.HasFlag(WINDOW_STYLE.WS_VISIBLE);
            PInvoke.ShowWindow(Netch.ConsoleHwnd, visible ? SHOW_WINDOW_CMD.SW_HIDE : SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
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

            if (MessageBoxX.Show(i18N.Translate($"Download and install now?\n\n{UpdateChecker.GetLatestReleaseContent()}"), confirm: true) !=
                DialogResult.OK)
                return;

            NotifyTip(i18N.Translate("Start downloading new version"));
            NewVersionLabel.Enabled = false;
            NewVersionLabel.Text = "...";

            try
            {
                var progress = new Progress<int>();
                progress.ProgressChanged += (_, percentage) => { NewVersionLabel.Text = $"{percentage}%"; };

                string downloadDirectory = Path.Combine(Global.NetchDir, "data");

                var (updateFileName, sha256) = UpdateChecker.GetLatestUpdateFileNameAndHash();
                var updateFileUrl = UpdateChecker.LatestRelease.assets[0].browser_download_url!;

                var updateFileFullName = Path.Combine(downloadDirectory, updateFileName);
                var updater = new Updater(updateFileFullName, Global.NetchDir);

                var downloaded = false;
                if (File.Exists(updateFileFullName))
                    if (Utils.Utils.SHA256CheckSum(updateFileFullName) == sha256)
                        downloaded = true;
                    else
                        File.Delete(updateFileFullName);

                if (!downloaded)
                {
                    try
                    {
                        await WebUtil.DownloadFileAsync(updateFileUrl, updateFileFullName, progress);
                    }
                    catch (Exception e1)
                    {
                        Log.Warning(e1, "Download Update File Failed");
                        throw new MessageException($"Download Update File Failed: {e1.Message}");
                    }

                    if (Utils.Utils.SHA256CheckSum(updateFileFullName) != sha256)
                        throw new MessageException(i18N.Translate("The downloaded file has the wrong hash"));
                }

                ModeHelper.SuspendWatcher = true;
                await StopAsync();
                await Configuration.SaveAsync();

                // Update
                await Task.Run(updater.ApplyUpdate);

                // release mutex, exit
                Netch.SingleInstance.Dispose();
                Process.Start(Global.NetchExecutable);
                Environment.Exit(0);
            }
            catch (MessageException exception)
            {
                NotifyTip(exception.Message, info: false);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "更新未处理异常");
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
                await StopCoreAsync();
                return;
            }

            Configuration.SaveAsync().Forget();

            // 服务器、模式 需选择
            if (ServerComboBox.SelectedItem is not Server server)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            if (ModeComboBox.SelectedItem is not Mode mode)
            {
                MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                return;
            }

            State = State.Starting;

            try
            {
                await MainController.StartAsync(server, mode);
            }
            catch (Exception exception)
            {
                State = State.Stopped;
                StatusText(i18N.Translate("Start failed"));
                MessageBoxX.Show(exception.Message, LogLevel.ERROR);
                return;
            }

            State = State.Started;

            Task.Run(Bandwidth.NetTraffic).Forget();
            NatTestAsync().Forget();

            if (Global.Settings.MinimizeWhenStarted)
                Minimize();

            // 自动检测延迟
            async Task StartedPingAsync()
            {
                while (State == State.Started)
                {
                    if (Global.Settings.StartedPingInterval >= 0)
                    {
                        await server.PingAsync();
                        ServerComboBox.Refresh();

                        await Task.Delay(Global.Settings.StartedPingInterval * 1000);
                    }
                    else
                    {
                        await Task.Delay(5000);
                    }
                }
            }

            StartedPingAsync().Forget();
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
            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Settings.Server.Cast<object>().ToArray());
            SelectLastServer();
        }

        private void SelectLastServer()
        {
            // 如果值合法，选中该位置
            if (Global.Settings.ServerComboBoxSelectedIndex > 0 && Global.Settings.ServerComboBoxSelectedIndex < ServerComboBox.Items.Count)
                ServerComboBox.SelectedIndex = Global.Settings.ServerComboBoxSelectedIndex;
            // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
            else if (ServerComboBox.Items.Count > 0)
                ServerComboBox.SelectedIndex = 0;

            // 如果当前 ServerComboBox 中没元素，不做处理
        }

        private void ServerComboBox_SelectionChangeCommitted(object sender, EventArgs o)
        {
            Global.Settings.ServerComboBoxSelectedIndex = ServerComboBox.SelectedIndex;
        }

        private async void EditServerPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (!(ServerComboBox.SelectedItem is Server server))
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            Hide();
            ServerHelper.GetUtilByTypeName(server.Type).Edit(server);
            LoadServers();
            await Configuration.SaveAsync();
            Show();
        }

        private async void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            void Enable()
            {
                ServerComboBox.Refresh();
                Enabled = true;
                StatusText();
            }

            Enabled = false;
            StatusText(i18N.Translate("Testing"));

            if (!IsWaiting() || ModifierKeys == Keys.Control)
            {
                (ServerComboBox.SelectedItem as Server)?.PingAsync();
                Enable();
            }
            else
            {
                await ServerHelper.DelayTestHelper.TestAllDelayAsync();
                Enable();
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
            if (InvokeRequired)
            {
                Invoke(new Action(LoadModes));
                return;
            }

            ModeComboBox.Items.Clear();
            ModeComboBox.Items.AddRange(Global.Modes.Cast<object>().ToArray());
            ModeComboBox.Tag = null;
            SelectLastMode();
        }

        private void SelectLastMode()
        {
            // 如果值合法，选中该位置
            if (Global.Settings.ModeComboBoxSelectedIndex > 0 && Global.Settings.ModeComboBoxSelectedIndex < ModeComboBox.Items.Count)
                ModeComboBox.SelectedIndex = Global.Settings.ModeComboBoxSelectedIndex;
            // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
            else if (ModeComboBox.Items.Count > 0)
                ModeComboBox.SelectedIndex = 0;

            // 如果当前 ModeComboBox 中没元素，不做处理
        }

        private void ModeComboBox_SelectionChangeCommitted(object sender, EventArgs o)
        {
            try
            {
                Global.Settings.ModeComboBoxSelectedIndex = Global.Modes.IndexOf((Mode)ModeComboBox.SelectedItem);
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

            var mode = (Mode)ModeComboBox.SelectedItem;
            if (ModifierKeys == Keys.Control)
            {
                Utils.Utils.Open(ModeHelper.GetFullPath(mode.RelativePath!));
                return;
            }

            switch (mode.Type)
            {
                case ModeType.Process:
                    Hide();
                    new ProcessForm(mode).ShowDialog();
                    Show();
                    break;
                case ModeType.ProxyRuleIPs:
                case ModeType.BypassRuleIPs:
                    Hide();
                    new RouteForm(mode).ShowDialog();
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

            ModeHelper.Delete((Mode)ModeComboBox.SelectedItem);
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
                ((Button)button).Dispose();

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
                ProfileTable.RowCount = (int)Math.Ceiling(profileCount / (float)columnCount);

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

            var server = ServerComboBox.Items.Cast<Server>().FirstOrDefault(s => s.Remark.Equals(profile.ServerRemark));
            var mode = ModeComboBox.Items.Cast<Mode>().FirstOrDefault(m => m.Remark.Equals(profile.ModeRemark));

            if (server == null)
                throw new Exception("Server not found.");

            if (mode == null)
                throw new Exception("Mode not found.");

            ServerComboBox.SelectedItem = server;
            ModeComboBox.SelectedItem = mode;
        }

        private Profile CreateProfileAtIndex(int index)
        {
            var server = (Server)ServerComboBox.SelectedItem;
            var mode = (Mode)ModeComboBox.SelectedItem;
            var name = ProfileNameText.Text;

            Profile? profile;
            if ((profile = Global.Settings.Profiles.SingleOrDefault(p => p.Index == index)) != null)
                Global.Settings.Profiles.Remove(profile);

            profile = new Profile(server, mode, name, index);
            Global.Settings.Profiles.Add(profile);
            return profile;
        }

        private async void ProfileButton_Click([NotNull] object? sender, EventArgs? e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            var profileButton = (Button)sender;
            var profile = (Profile?)profileButton.Tag;
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
                    UninstallServiceToolStripMenuItem.Enabled = UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = enabled;
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

        public async Task StopAsync()
        {
            if (IsWaiting())
                return;

            await StopCoreAsync();
        }

        private async Task StopCoreAsync()
        {
            State = State.Stopping;
            await MainController.StopAsync();
            State = State.Stopped;
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

        private void NatTypeStatusText(string? text = null, string? country = null)
        {
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
                NatTypeStatusLightLabel.Visible = Flags.IsWindows10Upper;
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

        private async void NatTypeStatusLabel_Click(object sender, EventArgs e)
        {
            if (_state == State.Started && !Monitor.IsEntered(_natTestLock))
                await NatTestAsync();
        }

        private bool _natTestLock = true;

        /// <summary>
        ///     测试 NAT
        /// </summary>
        private async Task NatTestAsync()
        {
            if (!MainController.Mode!.TestNatRequired())
                return;

            if (!_natTestLock)
                return;

            _natTestLock = false;

            try
            {
                NatTypeStatusText(i18N.Translate("Testing NAT"));

                var (result, _, publicEnd) = await MainController.NTTController.StartAsync();

                if (!string.IsNullOrEmpty(publicEnd))
                {
                    var country = await Utils.Utils.GetCityCodeAsync(publicEnd!);
                    NatTypeStatusText(result, country);
                }
                else
                {
                    NatTypeStatusText(result ?? "Error");
                }
            }
            finally
            {
                _natTestLock = true;
            }
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
                        Log.Information("操作系统即将挂起，自动停止");
                        ControlButton_Click(null, null);
                    }

                    break;
                case PowerModes.Resume: //操作系统即将从挂起状态继续
                    if (_resumeFlag)
                    {
                        _resumeFlag = false;
                        Log.Information("操作系统即将从挂起状态继续，自动重启");
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
                await Configuration.SaveAsync();

            foreach (var file in new[] { Constants.TempConfig, Constants.TempRouteFile })
                if (File.Exists(file))
                    File.Delete(file);

            await StopAsync();

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

        private async Task CheckUpdateAsync()
        {
            try
            {
                UpdateChecker.NewVersionFound += OnUpdateCheckerOnNewVersionFound;
                await UpdateChecker.CheckAsync(Global.Settings.CheckBetaUpdate);
                if (Flags.AlwaysShowNewVersionFound)
                    OnUpdateCheckerOnNewVersionFound(null!, null!);
            }
            finally
            {
                UpdateChecker.NewVersionFound -= OnUpdateCheckerOnNewVersionFound;
            }

            void OnUpdateCheckerOnNewVersionFound(object? o, EventArgs? eventArgs)
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

        public void ShowMainFormToolStripButton_Click(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowMainFormToolStripButton_Click(sender, e)));
                return;
            }

            var forms = Application.OpenForms.Cast<Form>().ToList();
            var anyWindowOpened = forms.Any(f => f is not (MainForm or LogForm));

            forms.ForEach(f =>
            {
                if (anyWindowOpened && f is MainForm or LogForm)
                    return;

                f.Show();
                f.WindowState = FormWindowState.Normal;
                f.Activate();
            });
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
            ShowMainFormToolStripButton.PerformClick();
        }

        public void NotifyTip(string text, int timeout = 0, bool info = true)
        {
            // 会阻塞线程 timeout 秒(?)
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
            if (sender is not ComboBox cbx)
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
                    var numBoxBackBrush = item.Delay switch { > 200 => Brushes.Red, > 80 => Brushes.Yellow, >= 0 => _greenBrush, _ => Brushes.Gray };

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
                case Mode item:
                {
                    // 绘制 模式Box 底色
                    e.Graphics.FillRectangle(Brushes.Gray, _numberBoxX, e.Bounds.Y, _numberBoxWidth, e.Bounds.Height);

                    // 绘制 模式行数 字符串
                    TextRenderer.DrawText(e.Graphics,
                        item.Content.Count.ToString(),
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
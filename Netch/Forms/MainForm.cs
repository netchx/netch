using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynamicData.Binding;
using Microsoft.Win32;
using Netch.Controllers;
using Netch.Enums;
using Netch.Forms.Mode;
using Netch.Interfaces;
using Netch.Models;
using Netch.Properties;
using Netch.Services;
using Netch.Utils;
using Netch.ViewModels;
using ReactiveUI;
using Serilog;

namespace Netch.Forms
{
    public partial class MainForm : Form, IViewFor<MainWindowViewModel>
    {
        private readonly Setting _setting;
        private readonly IConfigService _configService;
        private readonly ModeService _modeService;

        #region Start

        private readonly Dictionary<string, object> _mainFormText = new();

        private bool _textRecorded;

        public MainForm(MainWindowViewModel viewModel, Setting setting, IConfigService configService, ModeService modeService)
        {
            InitializeComponent();
            NotifyIcon.Icon = Icon = Resources.icon;

            ViewModel = viewModel;

            var items = AddAddServerToolStripMenuItems().ToArray();

            foreach (var item in items)
            {
                item.Events().Click.Subscribe(_ => ViewModel.CreateServerCommand.Execute((IServerUtil) (item.Tag)));
            }

            _setting = setting;
            _configService = configService;
            _modeService = modeService;

            this.WhenActivated(d =>
            {
                this.Bind(ViewModel, vm => vm.ServerList, v => v.ServerComboBox.DataSource).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.ModeCache, v => v.ModeComboBox.DataSource).DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.ImportServerFromClipBoardCommand, v => v.ImportServersFromClipboardToolStripMenuItem)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.CleanDnsCommand, v => v.CleanDNSCacheToolStripMenuItem).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.UpdateServersFromSubscribeCommand, v => v.UpdateServersFromSubscribeLinksToolStripMenuItem)
                    .DisposeWith(d);


                this.BindCommand(ViewModel, vm => vm.CreateProcessModeCommand, v => v.CreateProcessModeToolStripMenuItem).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.CreateRouteTableModeCommand, v => v.CreateRouteTableRuleToolStripMenuItem).DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.ManageSubscribeLinksCommand, v => v.ManageSubscribeLinksToolStripMenuItem).DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.OpenDirectoryCommand, v => v.OpenDirectoryToolStripMenuItem).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.RemoveNetchFirewallRulesCommand, v => v.removeNetchFirewallRulesToolStripMenuItem)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.ShowHideConsoleCommand, v => v.ShowHideConsoleToolStripMenuItem).DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.OpenFaqPageCommand, v => v.fAQToolStripMenuItem).DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.OpenReleasesPageCommand, v => v.VersionLabel).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.State, v => v.State).DisposeWith(d);

                this.OneWayBind(ViewModel,
                        vm => vm.ServerList,
                        v => v.DeleteServerPictureBox.Enabled,
                        list => !list.Any())
                    .DisposeWith(d);

                var o = ServerComboBox.Events()
                    .SelectionChangeCommitted.Where(_ => ServerComboBox.SelectedIndex >= 0)
                    .Select(_ => (Server) ServerComboBox.SelectedItem);

                this.BindCommand(ViewModel, vm => vm.DeleteServerCommand, v => v.DeleteServerPictureBox, o).DisposeWith(d);
            });


            #region i18N Translations

            _mainFormText.Add(UninstallServiceToolStripMenuItem.Name, new[] {"Uninstall {0}", "NF Service"});

            #endregion

            // 监听电源事件
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private IEnumerable<ToolStripMenuItem> AddAddServerToolStripMenuItems()
        {
            foreach (var serversUtil in ServerService.ServerUtils.Where(i => !string.IsNullOrEmpty(i.FullName)))
            {
                var fullName = serversUtil.FullName;
                var control = new ToolStripMenuItem
                {
                    Name = $"Add{fullName}ServerToolStripMenuItem",
                    Size = new Size(259, 22),
                    Text = i18N.TranslateFormat("Add [{0}] Server", fullName),
                    Tag = serversUtil
                };

                _mainFormText.Add(control.Name, new[] {"Add [{0}] Server", fullName});
                ServerToolStripMenuItem.DropDownItems.Add(control);
                yield return control;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 计算 ComboBox绘制 目标宽度
            RecordSize();

            SelectLastServer();
            ServerService.DelayTestHelper.UpdateInterval();
            SelectLastMode();

            // 加载翻译
            TranslateControls();

            // 隐藏 NatTypeStatusLabel
            NatTypeStatusText();

            // 加载快速配置
            LoadProfiles();

            BeginInvoke(new Action(async () =>
            {
                // 检查更新
                if (_setting.CheckUpdateWhenOpened)
                    await CheckUpdate();
            }));

            BeginInvoke(new Action(async () =>
            {
                // 检查订阅更新
                /* TODO
                if (_setting.UpdateServersWhenOpened)
                    await UpdateServersFromSubscribe();
                    */

                // 打开软件时启动加速，产生开始按钮点击事件
                if (_setting.StartWhenOpened)
                    ControlButton_Click(null, null);
            }));

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

                Misc.ComponentIterator(this, RecordText);
                Misc.ComponentIterator(NotifyMenu, RecordText);
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

            Misc.ComponentIterator(this, TranslateText);
            Misc.ComponentIterator(NotifyMenu, TranslateText);

            #endregion

            UsedBandwidthLabel.Text = $@"{i18N.Translate("Used", ": ")}0 KB";
            State = State;
            VersionLabel.Text = UpdateChecker.Version;
        }

        #endregion

        #region Controls

        #region MenuStrip

        #region Subscription

        public void ServerControls(bool v)
        {
            MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ProfileGroupBox.Enabled = ControlButton.Enabled = v;
        }

        public void ModeControls(bool v)
        {
            MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ProfileGroupBox.Enabled = ControlButton.Enabled = v;
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
                await CheckUpdate();
            }
            finally
            {
                UpdateChecker.NewVersionNotFound -= OnNewVersionNotFound;
                UpdateChecker.NewVersionFoundFailed -= OnNewVersionFoundFailed;
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

        #endregion

        /// <summary>
        ///     菜单栏强制退出
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit(true);
        }

        private async void NewVersionLabel_Click(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Control || !UpdateChecker.LatestRelease!.assets.Any())
            {
                Misc.Open(UpdateChecker.LatestVersionUrl!);
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
                await Updater.DownloadAndUpdate(Path.Combine(Global.NetchDir, "data"),
                    Global.NetchDir,
                    (_, args) => BeginInvoke(new Action(() => NewVersionLabel.Text = $"{args.ProgressPercentage}%")));
            }
            catch (Exception exception)
            {
                if (exception is not MessageException)
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

        #endregion

        #region ControlButton

        private async void ControlButton_Click(object? sender, EventArgs? e)
        {
            if (!IsWaiting())
            {
                await StopCore();
                return;
            }

            await _configService.SaveAsync();

            // 服务器、模式 需选择
            if (ServerComboBox.SelectedItem is not Server server)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            if (ModeComboBox.SelectedItem is not Models.Mode mode)
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

            _ = Task.Run(Bandwidth.NetTraffic);
            _ = Task.Run(NatTest);

            if (_setting.MinimizeWhenStarted)
                Minimize();

            // 自动检测延迟
            _ = Task.Run(() =>
            {
                while (State == State.Started)
                    if (_setting.StartedPingInterval >= 0)
                    {
                        server.Test();
                        ServerComboBox.Refresh();

                        Thread.Sleep(_setting.StartedPingInterval * 1000);
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
            });
        }

        #endregion

        #region SettingsButton

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            var oldSettings = _setting.Clone();

            Hide();
            var settingForm = DI.GetRequiredService<SettingForm>();

            settingForm.ShowDialog();

            if (oldSettings.Language != _setting.Language)
            {
                i18N.Load(_setting.Language);
                TranslateControls();
                SelectLastMode();
                LoadProfiles();
            }

            if (oldSettings.DetectionTick != _setting.DetectionTick)
                ServerService.DelayTestHelper.UpdateInterval();

            if (oldSettings.ProfileCount != _setting.ProfileCount)
                LoadProfiles();

            Show();
        }

        #endregion

        #region Server

        public void SelectLastServer()
        {
            // 如果值合法，选中该位置
            if (_setting.ServerComboBoxSelectedIndex > 0 && _setting.ServerComboBoxSelectedIndex < ServerComboBox.Items.Count)
                ServerComboBox.SelectedIndex = _setting.ServerComboBoxSelectedIndex;
            // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
            else if (ServerComboBox.Items.Count > 0)
                ServerComboBox.SelectedIndex = 0;

            // 如果当前 ServerComboBox 中没元素，不做处理
        }

        private void ServerComboBox_SelectionChangeCommitted(object sender, EventArgs o)
        {
            _setting.ServerComboBoxSelectedIndex = ServerComboBox.SelectedIndex;
        }

        private async void EditServerPictureBox_Click(object sender, EventArgs e)
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
            ServerService.GetUtilByTypeName(server.Type).Edit(server);
            SelectLastServer();
            await _configService.SaveAsync();
            Show();
        }

        private void SpeedPictureBox_Click(object sender, EventArgs e)
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
                (ServerComboBox.SelectedItem as Server)?.Test();
                Enable();
            }
            else
            {
                ServerService.DelayTestHelper.TestDelayFinished += OnTestDelayFinished;
                _ = Task.Run(ServerService.DelayTestHelper.TestAllDelay);

                void OnTestDelayFinished(object? o1, EventArgs? e1)
                {
                    ServerService.DelayTestHelper.TestDelayFinished -= OnTestDelayFinished;
                    Enable();
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
            /*
             
                        // 当前 ServerComboBox 中至少有一项
                        if (!(ServerComboBox.SelectedItem is Server server))
                        {
                            MessageBoxX.Show(i18N.Translate("Please select a server first"));
                            return;
                        }
            
                        SelectLastServer();*/
        }

        #endregion

        #region Mode

        private void SelectLastMode()
        {
            // 如果值合法，选中该位置
            if (_setting.ModeComboBoxSelectedIndex > 0 && _setting.ModeComboBoxSelectedIndex < ModeComboBox.Items.Count)
                ModeComboBox.SelectedIndex = _setting.ModeComboBoxSelectedIndex;
            // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
            else if (ModeComboBox.Items.Count > 0)
                ModeComboBox.SelectedIndex = 0;

            // 如果当前 ModeComboBox 中没元素，不做处理
        }

        private void ModeComboBox_SelectionChangeCommitted(object sender, EventArgs o)
        {
            try
            {
                _setting.ModeComboBoxSelectedIndex = ModeComboBox.Items.IndexOf((Models.Mode) ModeComboBox.SelectedItem);
            }
            catch
            {
                _setting.ModeComboBoxSelectedIndex = 0;
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
                Misc.Open(ModeService.GetFullPath(mode.RelativePath!));
                return;
            }

            switch (mode.Type)
            {
                case ModeType.Process:
                    Hide();
                    new Process(mode).ShowDialog();
                    Show();
                    break;
                case ModeType.ProxyRuleIPs:
                case ModeType.BypassRuleIPs:
                    Hide();
                    new Route(mode).ShowDialog();
                    Show();
                    break;
                default:
                    Misc.Open(ModeService.GetFullPath(mode.RelativePath!));
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

            _modeService.DeleteMode((Models.Mode) ModeComboBox.SelectedItem);
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

            var profileCount = _setting.ProfileCount;
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

                if (_setting.ProfileTableColumnCount == 0)
                    _setting.ProfileTableColumnCount = 5;

                var columnCount = _setting.ProfileTableColumnCount;

                ProfileTable.ColumnCount = profileCount >= columnCount ? columnCount : profileCount;
                ProfileTable.RowCount = (int) Math.Ceiling(profileCount / (float) columnCount);

                for (var i = 0; i < profileCount; ++i)
                {
                    var profile = _setting.Profiles.SingleOrDefault(p => p.Index == i);
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
            if ((profile = _setting.Profiles.SingleOrDefault(p => p.Index == index)) != null)
                _setting.Profiles.Remove(profile);

            profile = new Profile(server, mode, name, index);
            _setting.Profiles.Add(profile);
            return profile;
        }

        private async void ProfileButton_Click([NotNull] object? sender, EventArgs? e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

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

                    _setting.Profiles.Remove(profile);
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

                ServerService.DelayTestHelper.Enabled = IsWaiting(_state);

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

        public async Task Stop()
        {
            if (IsWaiting())
                return;

            await StopCore();
        }

        private async Task StopCore()
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
                await NatTest();
        }

        private bool _natTestLock = true;

        /// <summary>
        ///     测试 NAT
        /// </summary>
        private async Task NatTest()
        {
            if (!MainController.Mode!.TestNatRequired())
                return;

            if (!_natTestLock)
                return;

            _natTestLock = false;

            try
            {
                NatTypeStatusText(i18N.Translate("Testing NAT"));

                // Monitor.TryEnter() Monitor.Exit() (a.k.a. lock) not work with async/await
                var (result, _, publicEnd) = await MainController.NTTController.Start();

                if (!string.IsNullOrEmpty(publicEnd))
                {
                    var country = Misc.GetCityCode(publicEnd!);
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
            if (!IsWaiting() && !_setting.StopWhenExited && !forceExit)
            {
                MessageBoxX.Show(i18N.Translate("Please press Stop button first"));

                NotifyIcon_MouseDoubleClick(null, null);
                return;
            }

            State = State.Terminating;
            NotifyIcon.Visible = false;
            Hide();

            if (saveConfiguration)
                await _configService.SaveAsync();

            foreach (var file in new[] {Constants.TempConfig, Constants.TempRouteFile})
                if (File.Exists(file))
                    File.Delete(file);

            await Stop();

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
                if (!_setting.ExitWhenClosed)
                    Minimize();
                // 如果勾选了关闭时退出，自动点击退出按钮
                else
                    Exit();
            }
        }

        #endregion

        #region Updater

        private async Task CheckUpdate()
        {
            try
            {
                UpdateChecker.NewVersionFound += OnUpdateCheckerOnNewVersionFound;
                await UpdateChecker.Check(_setting.CheckBetaUpdate);
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

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainWindowViewModel?) value;
        }

        public MainWindowViewModel? ViewModel { get; set; }
    }
}
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
using Netch.Utils;

namespace Netch.Forms
{
    public partial class MainForm : Form
    {
        #region Start

        private readonly Dictionary<string, object> _mainFormText = new();

        private bool _comboBoxInitialized;
        private bool _textRecorded;
        public MainForm()
        {
            InitializeComponent();

            AddAddServerToolStripMenuItems();

            #region i18N Translations

            _mainFormText.Add(UninstallServiceToolStripMenuItem.Name, new[] {"Uninstall {0}", "NF Service"});
            _mainFormText.Add(UninstallTapDriverToolStripMenuItem.Name, new[] {"Uninstall {0}", "TUN/TAP driver"});

            #endregion

            // 监听电源事件
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            ModeComboBox.KeyUp += (sender, args) =>
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            OnlyInstance.Called += OnCalled;

            // 计算 ComboBox绘制 目标宽度
            _comboBoxNumberBoxWidth = ServerComboBox.Width / 10;

            InitServer();
            ServerHelper.DelayTestHelper.UpdateInterval();

            ModeHelper.Load();
            InitMode();
            _comboBoxInitialized = true;

            // 加载翻译
            InitText();

            // 隐藏 NatTypeStatusLabel
            NatTypeStatusText();

            _configurationGroupBoxHeight = ConfigurationGroupBox.Height;
            _profileConfigurationHeight = ConfigurationGroupBox.Controls[0].Height / 3; // 因为 AutoSize, 所以得到的是Controls的总高度
            // 加载快速配置
            InitProfile();

            // 打开软件时启动加速，产生开始按钮点击事件
            if (Global.Settings.StartWhenOpened)
                ControlButton_Click(null, null);

            Task.Run(() =>
            {
                // 检查更新
                if (Global.Settings.CheckUpdateWhenOpened)
                    CheckUpdate();
            });

            Task.Run(async () =>
            {
                // 检查订阅更新
                if (Global.Settings.UpdateServersWhenOpened)
                    await UpdateServersFromSubscribe(Global.Settings.UseProxyToUpdateSubscription);
            });
        }
        /// <summary>
        ///     Init at <see cref="MainForm_Load" />
        /// </summary>
        private int _comboBoxNumberBoxWidth;

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
        private void InitText()
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
                            case TextBoxBase _:
                            case ListControl _:
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
                    case TextBoxBase _:
                    case ListControl _:
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
                    if (value.Equals(string.Empty)) return string.Empty;

                    if (value is object[] values)
                        return i18N.TranslateFormat(values.First() as string, values.Skip(1).ToArray());
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

                InitServer();
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

            InitServer();
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
                InitMode();
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
            InitServer();
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

            if (useProxy && ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
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
                string proxyServer = null;
                if (useProxy)
                {
                    var mode = new Models.Mode
                    {
                        Remark = "ProxyUpdate",
                        Type = 5
                    };
                    await MainController.Start(ServerComboBox.SelectedItem as Server, mode);
                    proxyServer = $"http://127.0.0.1:{Global.Settings.HTTPLocalPort}";
                }

                await Subscription.UpdateServersAsync(proxyServer);

                InitServer();
                Configuration.Save();
                StatusText(i18N.Translate("Subscription updated"));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                if (useProxy)
                    try
                    {
                        await MainController.Stop();
                    }
                    catch
                    {
                        // ignored
                    }

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
                    UpdateChecker.NewVersionNotFound -= OnNewVersionNotFound;
                    NotifyTip(i18N.Translate("Already latest version"));
                }

                void OnNewVersionFoundFailed(object o, EventArgs args)
                {
                    UpdateChecker.NewVersionFoundFailed -= OnNewVersionFoundFailed;
                    NotifyTip(i18N.Translate("New version found failed"), info: false);
                }

                UpdateChecker.NewVersionNotFound += OnNewVersionNotFound;
                UpdateChecker.NewVersionFoundFailed += OnNewVersionFoundFailed;
                CheckUpdate();
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
                    DNS.Cache.Clear();
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
            if (useProxy && ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            Enabled = false;
            StatusText(i18N.TranslateFormat("Updating {0}", "ACL"));
            try
            {
                if (useProxy)
                {
                    var mode = new Models.Mode
                    {
                        Remark = "ProxyUpdate",
                        Type = 5
                    };
                    State = State.Starting;
                    await MainController.Start(ServerComboBox.SelectedItem as Server, mode);
                }

                var req = WebUtil.CreateRequest(Global.Settings.ACL);
                if (useProxy)
                    req.Proxy = new WebProxy($"http://127.0.0.1:{Global.Settings.HTTPLocalPort}");

                await WebUtil.DownloadFileAsync(req, Path.Combine(Global.NetchDir, "bin\\default.acl"));
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
                {
                    await MainController.Stop();
                    State = State.Stopped;
                }

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

        private async void ControlButton_Click(object sender, EventArgs e)
        {
            if (!IsWaiting())
            {
                // 停止
                State = State.Stopping;
                await MainController.Stop();
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

            if (!await MainController.Start(server, mode))
            {
                State = State.Stopped;
                StatusText(i18N.Translate("Start failed"));
                return;
            }

            State = State.Started;

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
            Hide();
            new SettingForm().ShowDialog();

            if (i18N.LangCode != Global.Settings.Language)
            {
                i18N.Load(Global.Settings.Language);
                InitText();
                InitMode();
                InitProfile();
            }

            if (ServerHelper.DelayTestHelper.Interval != Global.Settings.DetectionTick)
                ServerHelper.DelayTestHelper.UpdateInterval();

            if (ProfileButtons.Count != Global.Settings.ProfileCount)
                InitProfile();

            Show();
        }

        #endregion

        #region Server

        private void InitServer()
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
            if (Global.Settings.ServerComboBoxSelectedIndex > 0 &&
                Global.Settings.ServerComboBoxSelectedIndex < ServerComboBox.Items.Count)
                ServerComboBox.SelectedIndex = Global.Settings.ServerComboBoxSelectedIndex;
            // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
            else if (ServerComboBox.Items.Count > 0)
                ServerComboBox.SelectedIndex = 0;

            // 如果当前 ServerComboBox 中没元素，不做处理
        }

        private void ServerComboBox_SelectedIndexChanged(object sender, EventArgs o)
        {
            if (!_comboBoxInitialized) return;
            Global.Settings.ServerComboBoxSelectedIndex = ServerComboBox.SelectedIndex;
        }

        private void EditServerPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            Hide();
            var server = Global.Settings.Server[ServerComboBox.SelectedIndex];
            ServerHelper.GetUtilByTypeName(server.Type).Edit(server);
            InitServer();
            Configuration.Save();
            Show();
        }

        private void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Testing"));

            if (IsWaiting())
            {
                ServerHelper.DelayTestHelper.TestDelayFinished += OnTestDelayFinished;
                _ = Task.Run(ServerHelper.DelayTestHelper.TestAllDelay);

                void OnTestDelayFinished(object o1, EventArgs e1)
                {
                    Refresh();
                    NotifyTip(i18N.Translate("Test done"));

                    ServerHelper.DelayTestHelper.TestDelayFinished -= OnTestDelayFinished;
                    Enabled = true;
                    StatusText();
                }
            }
            else
            {
                (ServerComboBox.SelectedItem as Server)?.Test();
                ServerComboBox.Refresh();
                Enabled = true;
                StatusText();
            }
        }

        private void CopyLinkPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            try
            {
                //听说巨硬BUG经常会炸，所以Catch一下 :D
                var server = (Server) ServerComboBox.SelectedItem;
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
            if (ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            Global.Settings.Server.Remove(ServerComboBox.SelectedItem as Server);
            InitServer();
        }

        #endregion

        #region Mode

        public void InitMode()
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
            if (Global.Settings.ModeComboBoxSelectedIndex > 0 &&
                Global.Settings.ModeComboBoxSelectedIndex < ModeComboBox.Items.Count)
                ModeComboBox.SelectedIndex = Global.Settings.ModeComboBoxSelectedIndex;
            // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
            else if (ModeComboBox.Items.Count > 0)
                ModeComboBox.SelectedIndex = 0;

            // 如果当前 ModeComboBox 中没元素，不做处理
        }

        private void ModeComboBox_SelectedIndexChanged(object sender, EventArgs o)
        {
            if (!_comboBoxInitialized) return;
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
                Utils.Utils.Open(ModeHelper.GetFullPath(mode.RelativePath));
                return;
            }

            switch (mode.Type)
            {
                case 0:
                    Hide();
                    new Process(mode).ShowDialog();
                    Show();
                    break;
                default:
                    Utils.Utils.Open(ModeHelper.GetFullPath(mode.RelativePath));
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

        private void InitProfile()
        {
            // Clear
            foreach (var button in ProfileButtons)
                button.Dispose();

            ProfileButtons.Clear();
            ProfileTable.ColumnStyles.Clear();
            ProfileTable.RowStyles.Clear();

            var numProfile = Global.Settings.ProfileCount;
            if (numProfile == 0)
            {
                // Hide Profile GroupBox, Change window size
                configLayoutPanel.RowStyles[2].SizeType = SizeType.Percent;
                configLayoutPanel.RowStyles[2].Height = 0;
                ProfileGroupBox.Visible = false;

                ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, _configurationGroupBoxHeight - _profileConfigurationHeight);
            }
            else
            {
                // Load Profiles
                ProfileTable.ColumnCount = numProfile;

                while (Global.Settings.Profiles.Count < numProfile)
                    Global.Settings.Profiles.Add(new Profile());

                for (var i = 0; i < numProfile; ++i)
                {
                    var b = new Button();
                    b.Click += ProfileButton_Click;
                    b.Dock = DockStyle.Fill;
                    b.Text = !Global.Settings.Profiles[i].IsDummy ? Global.Settings.Profiles[i].ProfileName : i18N.Translate("None");

                    ProfileTable.Controls.Add(b, i, 0);
                    ProfileButtons.Add(b);
                }

                // equal column
                for (var i = 1; i <= ProfileTable.RowCount; i++)
                    ProfileTable.RowStyles.Add(new RowStyle(SizeType.Percent, 1));

                for (var i = 1; i <= ProfileTable.ColumnCount; i++)
                    ProfileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));

                configLayoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
                ProfileGroupBox.Visible = true;
                ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, _configurationGroupBoxHeight);
            }
        }

        private void LoadProfile(int index)
        {
            var p = Global.Settings.Profiles[index];
            ProfileNameText.Text = p.ProfileName;
            ModeComboBox.ResetCompletionList();

            if (p.IsDummy)
                throw new Exception("Profile not found.");

            var server = ServerComboBox.Items.Cast<Server>().FirstOrDefault(s => s.Remark.Equals(p.ServerRemark));
            var mode = ModeComboBox.Items.Cast<Models.Mode>().FirstOrDefault(m => m.Remark.Equals(p.ModeRemark));

            if (server == null)
                throw new Exception("Server not found.");

            if (mode == null)
                throw new Exception("Mode not found.");

            ServerComboBox.SelectedItem = server;
            ModeComboBox.SelectedItem = mode;
        }

        private void SaveProfile(int index)
        {
            var selectedServer = (Server) ServerComboBox.SelectedItem;
            var selectedMode = (Models.Mode) ModeComboBox.SelectedItem;
            var name = ProfileNameText.Text;

            Global.Settings.Profiles[index] = new Profile(selectedServer, selectedMode, name);
        }

        private void RemoveProfile(int index)
        {
            Global.Settings.Profiles[index] = new Profile();
        }

        private readonly List<Button> ProfileButtons = new();

        private async void ProfileButton_Click(object sender, EventArgs e)
        {
            var index = ProfileButtons.IndexOf((Button) sender);

            if (ModifierKeys == Keys.Control)
            {
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBoxX.Show(i18N.Translate("Please select a server first"));
                }
                else if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                }
                else if (ProfileNameText.Text == "")
                {
                    MessageBoxX.Show(i18N.Translate("Please enter a profile name first"));
                }
                else
                {
                    SaveProfile(index);
                    ProfileButtons[index].Text = ProfileNameText.Text;
                }

                return;
            }

            if (Global.Settings.Profiles[index].IsDummy)
            {
                MessageBoxX.Show(
                    i18N.Translate("No saved profile here. Save a profile first by Ctrl+Click on the button"));
                return;
            }

            if (ModifierKeys == Keys.Shift)
            {
                if (MessageBoxX.Show(i18N.Translate("Remove this Profile?"), confirm: true) != DialogResult.OK) return;
                RemoveProfile(index);
                ProfileButtons[index].Text = i18N.Translate("None");
                return;
            }

            try
            {
                LoadProfile(index);
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
                    ServerComboBox.Enabled =
                        ModeComboBox.Enabled =
                            EditModePictureBox.Enabled =
                                EditServerPictureBox.Enabled =
                                    DeleteModePictureBox.Enabled =
                                        DeleteServerPictureBox.Enabled = enabled;

                    // 启动需要禁用的控件
                    UninstallServiceToolStripMenuItem.Enabled =
                        UpdateACLToolStripMenuItem.Enabled =
                            updateACLWithProxyToolStripMenuItem.Enabled =
                                updatePACToolStripMenuItem.Enabled =
                                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled =
                                        UninstallTapDriverToolStripMenuItem.Enabled =
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

                        StatusTextAppend(StatusPortInfoText.Value);

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

        public static class StatusPortInfoText
        {
            private static ushort? _socks5Port;
            private static ushort? _httpPort;
            private static bool _shareLan;

            public static ushort HttpPort
            {
                set => _httpPort = value;
            }

            public static ushort Socks5Port
            {
                set => _socks5Port = value;
            }

            public static string Value
            {
                get
                {
                    var strings = new List<string>();

                    if (_socks5Port != null)
                        strings.Add($"Socks5 {i18N.Translate("Local Port", ": ")}{_socks5Port}");

                    if (_httpPort != null)
                        strings.Add($"HTTP {i18N.Translate("Local Port", ": ")}{_httpPort}");

                    if (!strings.Any())
                        return string.Empty;

                    return $" ({(_shareLan ? i18N.Translate("Allow other Devices to connect") + " " : "")}{string.Join(" | ", strings)})";
                }
            }

            public static void UpdateShareLan()
            {
                _shareLan = Global.Settings.LocalAddress != "127.0.0.1";
            }

            public static void Reset()
            {
                _httpPort = _socks5Port = null;
            }
        }

        /// <summary>
        ///     更新状态栏文本
        /// </summary>
        /// <param name="text"></param>
        public void StatusText(string text = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(StatusText), text);
                return;
            }

            text ??= i18N.Translate(StateExtension.GetStatusString(State));
            StatusLabel.Text = i18N.Translate("Status", ": ") + text;
        }

        public void StatusTextAppend(string text)
        {
            StatusLabel.Text += text;
        }
        public void BandwidthState(bool state)
        {
            if (InvokeRequired)
                BeginInvoke(new Action<bool>(BandwidthState), state);
            UsedBandwidthLabel.Visible /*= UploadSpeedLabel.Visible*/ = DownloadSpeedLabel.Visible = state;
        }

        public void NatTypeStatusText(string text = "", string country = "")
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
                NatTypeStatusLabel.Text = $"NAT{i18N.Translate(": ")}{text} {(country != string.Empty ? $"[{country}]" : "")}";

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
                Color c;
                switch (natType)
                {
                    case 1:
                        c = Color.LimeGreen;
                        break;
                    case 2:
                        c = Color.Yellow;
                        break;
                    case 3:
                        c = Color.Red;
                        break;
                    case 4:
                        c = Color.Black;
                        break;
                    default:
                        c = Color.Black;
                        break;
                }

                NatTypeStatusLightLabel.ForeColor = c;
            }
            else
            {
                NatTypeStatusLightLabel.Visible = false;
            }
        }

        private void NatTypeStatusLabel_Click(object sender, EventArgs e)
        {
            if (_state == State.Started && MainController.NttTested)
                MainController.NatTest();
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

        private async void Exit(bool forceExit = false)
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
            Configuration.Save();

            foreach (var file in new[] {"data\\last.json", "data\\privoxy.conf"})
                if (File.Exists(file))
                    File.Delete(file);

            if (!IsWaiting())
                await MainController.Stop();

            Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        private void OnCalled(object sender, OnlyInstance.Commands e)
        {
            switch (e)
            {
                case OnlyInstance.Commands.Show:
                    NotifyIcon_MouseDoubleClick(null, null);
                    break;
                case OnlyInstance.Commands.Exit:
                    Exit(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
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
            UpdateChecker.NewVersionFound += (_, _) =>
            {
                NotifyTip($"{i18N.Translate(@"New version available", ": ")}{UpdateChecker.LatestVersionNumber}");
                NewVersionLabel.Visible = true;
            };
            UpdateChecker.Check(Global.Settings.CheckBetaUpdate);
        }

        private async void NewVersionLabel_Click(object sender, EventArgs e)
        {
            if (!UpdateChecker.LatestRelease.assets.Any())
            {
                Utils.Utils.Open(UpdateChecker.LatestVersionUrl);
                return;
            }

            if (MessageBoxX.Show(i18N.Translate("Download and install now?"), confirm: true) != DialogResult.OK)
                return;
            NotifyTip(i18N.Translate("Start downloading new version"));

            NewVersionLabel.Enabled = false;
            NewVersionLabel.Text = "...";
            try
            {
                void OnDownloadProgressChanged(object o1, DownloadProgressChangedEventArgs args)
                {
                    BeginInvoke(new Action(() => { NewVersionLabel.Text = $"{args.ProgressPercentage}%"; }));
                }

                await UpdateChecker.UpdateNetch(OnDownloadProgressChanged);
            }
            catch (Exception exception)
            {
                Logging.Error(exception.Message);
                NotifyTip(exception.Message);
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

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
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
            NotifyIcon.ShowBalloonTip(timeout,
                UpdateChecker.Name,
                text,
                info ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        #endregion

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (!(sender is ComboBox cbx))
                    return;

                // 绘制背景颜色
                e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                if (e.Index < 0) return;

                // 绘制 备注/名称 字符串
                e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, new SolidBrush(Color.Black), e.Bounds);

                switch (cbx.Items[e.Index])
                {
                    case Server item:
                    {
                        // 计算延迟底色
                        SolidBrush brush;
                        if (item.Delay > 200)
                            brush = new SolidBrush(Color.Red);
                        else if (item.Delay > 80)
                            brush = new SolidBrush(Color.Yellow);
                        else if (item.Delay >= 0)
                            brush = new SolidBrush(Color.FromArgb(50, 255, 56));
                        else
                            brush = new SolidBrush(Color.Gray);

                        // 绘制延迟底色
                        e.Graphics.FillRectangle(brush, _comboBoxNumberBoxWidth * 9, e.Bounds.Y, _comboBoxNumberBoxWidth, e.Bounds.Height);

                        // 绘制延迟字符串
                        e.Graphics.DrawString(item.Delay.ToString(), cbx.Font, new SolidBrush(Color.Black),
                            _comboBoxNumberBoxWidth * 9 + _comboBoxNumberBoxWidth / 30, e.Bounds.Y);
                        break;
                    }
                    case Models.Mode item:
                    {
                        // 绘制 模式Box 底色
                        e.Graphics.FillRectangle(new SolidBrush(Color.Gray), _comboBoxNumberBoxWidth * 9, e.Bounds.Y, _comboBoxNumberBoxWidth,
                            e.Bounds.Height);

                        // 绘制 模式行数 字符串
                        e.Graphics.DrawString(item.Rule.Count.ToString(), cbx.Font, new SolidBrush(Color.Black),
                            _comboBoxNumberBoxWidth * 9 + _comboBoxNumberBoxWidth / 30, e.Bounds.Y);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion
    }
}
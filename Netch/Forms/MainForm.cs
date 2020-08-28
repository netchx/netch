using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Netch.Controllers;
using Netch.Forms.Mode;
using Netch.Forms.Server;
using Netch.Models;
using Netch.Utils;
using Trojan = Netch.Forms.Server.Trojan;
using VMess = Netch.Forms.Server.VMess;

namespace Netch.Forms
{
    public partial class MainForm : Form
    {
        /// <summary>
        ///     主控制器
        /// </summary>
        private MainController _mainController = new MainController();

        public MainForm()
        {
            InitializeComponent();

            // 监听电源事件
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            CheckForIllegalCrossThreadCalls = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            OnlyInstance.Called += OnCalled;
            // 计算 ComboBox绘制 目标宽度
            _eWidth = ServerComboBox.Width / 10;

            Modes.Load();
            InitMode();
            InitServer();
            _comboBoxInitialized = true;

            // 加载翻译
            InitText();

            // 隐藏 NatTypeStatusLabel
            NatTypeStatusText();

            _sizeHeight = Size.Height;
            _configurationGroupBoxHeight = ConfigurationGroupBox.Height;
            _profileConfigurationHeight = ConfigurationGroupBox.Controls[0].Height / 3; // 因为 AutoSize, 所以得到的是Controls的总高度
            _profileGroupboxHeight = ProfileGroupBox.Height;
            // 加载快速配置
            InitProfile();

            // 打开软件时启动加速，产生开始按钮点击事件
            if (Global.Settings.StartWhenOpened)
            {
                ControlButton.PerformClick();
            }

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

            Task.Run(() =>
            {
                // 检查更新
                if (Global.Settings.CheckUpdateWhenOpened)
                {
                    CheckUpdate();
                }
            });
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

                    if (_isFirstCloseWindow)
                    {
                        // 显示提示语
                        NotifyTip(i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."));
                        _isFirstCloseWindow = false;
                    }

                    Hide();
                }
                // 如果勾选了关闭时退出，自动点击退出按钮
                else
                {
                    Exit();
                }
            }
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            ControlFun();
        }


        private void SettingsButton_Click(object sender, EventArgs e)
        {
            Hide();
            new SettingForm().ShowDialog();

            if (i18N.LangCode != Global.Settings.Language)
            {
                i18N.Load(Global.Settings.Language);
                InitText();
                InitProfile();
            }

            if (ProfileButtons.Count != Global.Settings.ProfileCount)
                InitProfile();

            Show();
        }

        private void InitText()
        {
            ServerToolStripMenuItem.Text = i18N.Translate("Server");
            ImportServersFromClipboardToolStripMenuItem.Text = i18N.Translate("Import Servers From Clipboard");
            AddSocks5ServerToolStripMenuItem.Text = i18N.Translate("Add [Socks5] Server");
            AddShadowsocksServerToolStripMenuItem.Text = i18N.Translate("Add [Shadowsocks] Server");
            AddShadowsocksRServerToolStripMenuItem.Text = i18N.Translate("Add [ShadowsocksR] Server");
            AddVMessServerToolStripMenuItem.Text = i18N.Translate("Add [VMess] Server");
            AddTrojanServerToolStripMenuItem.Text = i18N.Translate("Add [Trojan] Server");
            ModeToolStripMenuItem.Text = i18N.Translate("Mode");
            HelpToolStripMenuItem.Text = i18N.Translate("Help");
            CreateProcessModeToolStripMenuItem.Text = i18N.Translate("Create Process Mode");
            SubscribeToolStripMenuItem.Text = i18N.Translate("Subscribe");
            ManageSubscribeLinksToolStripMenuItem.Text = i18N.Translate("Manage Subscribe Links");
            UpdateServersFromSubscribeLinksToolStripMenuItem.Text = i18N.Translate("Update Servers From Subscribe Links");
            OptionsToolStripMenuItem.Text = i18N.Translate("Options");
            ReloadModesToolStripMenuItem.Text = i18N.Translate("Reload Modes");
            UninstallServiceToolStripMenuItem.Text = i18N.Translate("Uninstall NF Service");
            CleanDNSCacheToolStripMenuItem.Text = i18N.Translate("Clean DNS Cache");
            UpdateACLToolStripMenuItem.Text = i18N.Translate("Update ACL");
            updateACLWithProxyToolStripMenuItem.Text = i18N.Translate("Update ACL with proxy");
            reinstallTapDriverToolStripMenuItem.Text = i18N.Translate("Reinstall TUN/TAP driver");
            CheckForUpdatesToolStripMenuItem.Text = i18N.Translate("Check for updates");
            OpenDirectoryToolStripMenuItem.Text = i18N.Translate("Open Directory");
            AboutToolStripButton.Text = i18N.Translate("About");
            NewVersionLabel.Text = i18N.Translate("New version available");
            // VersionLabel.Text = i18N.Translate("xxx");
            exitToolStripMenuItem.Text = i18N.Translate("Exit");
            ConfigurationGroupBox.Text = i18N.Translate("Configuration");
            ProfileLabel.Text = i18N.Translate("Profile");
            ModeLabel.Text = i18N.Translate("Mode");
            ServerLabel.Text = i18N.Translate("Server");
            // UsedBandwidthLabel.Text = i18N.Translate("Used: 0 KB");
            // DownloadSpeedLabel.Text = i18N.Translate("↓: 0 KB/s");
            // UploadSpeedLabel.Text = i18N.Translate("↑: 0 KB/s");
            NotifyIcon.Text = i18N.Translate("Netch");
            ShowMainFormToolStripButton.Text = i18N.Translate("Show");
            ExitToolStripButton.Text = i18N.Translate("Exit");
            SettingsButton.Text = i18N.Translate("Settings");
            ProfileGroupBox.Text = i18N.Translate("Profiles");
            // 加载翻译

            UsedBandwidthLabel.Text = $@"{i18N.Translate("Used", ": ")}0 KB";
            State = State;

            VersionLabel.Text = UpdateChecker.Version;
        }

        private void Exit(bool forceExit = false)
        {
            if (State != State.Waiting && State != State.Stopped && !Global.Settings.StopWhenExited && !forceExit)
            {
                MessageBoxX.Show(i18N.Translate("Please press Stop button first"));

                NotifyIcon_MouseDoubleClick(null, null);
                return;
            }

            Hide();
            NotifyIcon.Visible = false;
            if (State != State.Waiting && State != State.Stopped)
            {
                // 已启动
                ControlFun();
            }

            Configuration.Save();
            State = State.Terminating;
        }

        #region MISC

        /// <summary>
        /// 监听电源事件，自动重启Netch服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            //不对Netch命令等待状态的电源事件做任何处理
            if (!State.Equals(State.Waiting))
            {
                switch (e.Mode)
                {
                    case PowerModes.Suspend: //操作系统即将挂起
                        Logging.Info("操作系统即将挂起，自动停止===>" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        ControlFun();
                        break;
                    case PowerModes.Resume: //操作系统即将从挂起状态继续
                        Logging.Info("操作系统即将从挂起状态继续，自动重启===>" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        ControlFun();
                        break;
                }
            }
        }

        private void EditServerPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            Form server = Global.Settings.Server[ServerComboBox.SelectedIndex].Type switch
            {
                "Socks5" => new Socks5(Global.Settings.Server[ServerComboBox.SelectedIndex]),
                "SS" => new Shadowsocks(Global.Settings.Server[ServerComboBox.SelectedIndex]),
                "SSR" => new ShadowsocksR(Global.Settings.Server[ServerComboBox.SelectedIndex]),
                "VMess" => new VMess(Global.Settings.Server[ServerComboBox.SelectedIndex]),
                "Trojan" => new Trojan(Global.Settings.Server[ServerComboBox.SelectedIndex]),
                _ => null
            };
            Hide();
            server?.ShowDialog();
            InitServer();
            Configuration.Save();
            Show();
        }

        private async void SpeedPictureBox_Click(object sender, EventArgs e)
        {
            Enabled = false;
            StatusText(i18N.Translate("Testing"));

            try
            {
                await Task.Run(TestServer);
            }
            finally
            {
                Enabled = true;
                StatusText(i18N.Translate("Test done"));
                Refresh();
            }
        }

        private void EditModePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ModeComboBox中至少有一项
            if (ModeComboBox.Items.Count <= 0 || ModeComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                return;
            }

            var selectedMode = (Models.Mode) ModeComboBox.SelectedItem;
            switch (selectedMode.Type)
            {
                case 0:
                {
                    Hide();
                    new Process(selectedMode).ShowDialog();
                    InitMode();
                    Show();
                    break;
                }
                default:
                {
                    MessageBoxX.Show($"Current not support editing {selectedMode.TypeToString()} Mode");
                    break;
                }
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

            var selectedMode = (Models.Mode) ModeComboBox.SelectedItem;
            this.ModeComboBox.Items.Remove(selectedMode);
            Modes.Delete(selectedMode);

            SelectLastMode();
        }

        private void CopyLinkPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex == -1)
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
                return;
            }

            var selectedMode = (Models.Server) ServerComboBox.SelectedItem;
            try
            {
                //听说巨硬BUG经常会炸，所以Catch一下 :D
                Clipboard.SetText(ShareLink.GetShareLink(selectedMode));
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

            var index = ServerComboBox.SelectedIndex;

            Global.Settings.Server.Remove(ServerComboBox.SelectedItem as Models.Server);
            InitServer();

            Configuration.Save();

            if (ServerComboBox.Items.Count > 0)
            {
                ServerComboBox.SelectedIndex = index != 0 ? index - 1 : index;
            }
        }

        #region NotifyIcon

        private void ShowMainFormToolStripButton_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                ShowInTaskbar = true; // 显示在系统任务栏 
                WindowState = FormWindowState.Normal; // 还原窗体 
            }

            Activate();
        }

        private void ExitToolStripButton_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                ShowInTaskbar = true; //显示在系统任务栏 
                WindowState = FormWindowState.Normal; //还原窗体
            }

            Activate();
        }

        private void NotifyTip(string text, int timeout = 0, bool info = true)
        {
            // 会阻塞线程 timeout 秒
            NotifyIcon.ShowBalloonTip(timeout,
                UpdateChecker.Name,
                text,
                info ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        #endregion

        #endregion

        private bool _comboBoxInitialized = false;

        private void ModeComboBox_SelectedIndexChanged(object sender, EventArgs o)
        {
            if (!_comboBoxInitialized) return;
            Global.Settings.ModeComboBoxSelectedIndex = ModeComboBox.SelectedIndex;
        }

        private void ServerComboBox_SelectedIndexChanged(object sender, EventArgs o)
        {
            if (!_comboBoxInitialized) return;
            Global.Settings.ServerComboBoxSelectedIndex = ServerComboBox.SelectedIndex;
        }

        private void NatTypeStatusLabel_Click(object sender, EventArgs e)
        {
            //一个不太优雅的🔒
            if (_state == State.Started && NatTypeStatusLabel.ToString().Contains("["))
            {
                _mainController.RetryNatTest();
            }
        }
    }
}
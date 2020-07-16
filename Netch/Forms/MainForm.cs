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

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 计算 ComboBox绘制 目标宽度
            _eWidth = ServerComboBox.Width / 10;

            // 加载服务器
            InitServer();

            // 加载模式
            InitMode();

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

                    if (_isFirstCloseWindow)
                    {
                        // 显示提示语
                        NotifyIcon.ShowBalloonTip(5,
                            UpdateChecker.Name,
                            i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."),
                            ToolTipIcon.Info);

                        _isFirstCloseWindow = false;
                    }

                    Hide();
                }
                // 如果勾选了关闭时退出，自动点击退出按钮
                else
                {
                    Exit(true);
                }
            }
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            ControlFun();
        }


        private void SettingsButton_Click(object sender, EventArgs e)
        {
            (Global.SettingForm = new SettingForm()).Show();
            Hide();
        }


        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible)
                return;

            if (i18N.LangCode != Global.Settings.Language)
            {
                i18N.Load(Global.Settings.Language);
                InitText();
                InitProfile();
            }

            if (ProfileButtons.Count != Global.Settings.ProfileCount)
                InitProfile();
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
            CreateProcessModeToolStripMenuItem.Text = i18N.Translate("Create Process Mode");
            SubscribeToolStripMenuItem.Text = i18N.Translate("Subscribe");
            ManageSubscribeLinksToolStripMenuItem.Text = i18N.Translate("Manage Subscribe Links");
            UpdateServersFromSubscribeLinksToolStripMenuItem.Text = i18N.Translate("Update Servers From Subscribe Links");
            OptionsToolStripMenuItem.Text = i18N.Translate("Options");
            ReloadModesToolStripMenuItem.Text = i18N.Translate("Reload Modes");
            UninstallServiceToolStripMenuItem.Text = i18N.Translate("Uninstall Service");
            CleanDNSCacheToolStripMenuItem.Text = i18N.Translate("Clean DNS Cache");
            UpdateACLToolStripMenuItem.Text = i18N.Translate("Update ACL");
            updateACLWithProxyToolStripMenuItem.Text = i18N.Translate("Update ACL with proxy");
            reinstallTapDriverToolStripMenuItem.Text = i18N.Translate("Reinstall TUN/TAP driver");
            OpenDirectoryToolStripMenuItem.Text = i18N.Translate("Open Directory");
            AboutToolStripButton.Text = i18N.Translate("About");
            // VersionLabel.Text = i18N.Translate("xxx");
            exitToolStripMenuItem.Text = i18N.Translate("Exit");
            RelyToolStripMenuItem.Text = i18N.Translate("Unable to start? Click me to download");
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
            UpdateStatus();

            VersionLabel.Text = UpdateChecker.Version;
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
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
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
            });
        }

        private void EditModePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ModeComboBox中至少有一项
            if (ModeComboBox.Items.Count > 0 && ModeComboBox.SelectedIndex != -1)
            {
                SaveConfigs();
                var selectedMode = (Models.Mode) ModeComboBox.SelectedItem;
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
                MessageBoxX.Show(i18N.Translate("Please select an mode first"));
            }
        }

        private void DeleteModePictureBox_Click(object sender, EventArgs e)
        {
            // 当前ModeComboBox中至少有一项
            if (ModeComboBox.Items.Count > 0 && ModeComboBox.SelectedIndex != -1)
            {
                var selectedMode = (Models.Mode) ModeComboBox.SelectedItem;

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
                MessageBoxX.Show(i18N.Translate("Please select an mode first"));
            }
        }

        private void CopyLinkPictureBox_Click(object sender, EventArgs e)
        {
            // 当前ServerComboBox中至少有一项
            if (ServerComboBox.SelectedIndex != -1)
            {
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
            else
            {
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
            }
        }

        private void DeleteServerPictureBox_Click(object sender, EventArgs e)
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
                MessageBoxX.Show(i18N.Translate("Please select a server first"));
            }
        }

        private void Exit(bool forceExit = false)
        {
            if(IsDisposed) return;
            // 已启动
            if (State != State.Waiting && State != State.Stopped)
            {
                if (forceExit)
                    ControlFun();
                else
                {
                    if (!Global.Settings.StopWhenExited)
                    {
                        // 未开启自动停止
                        MessageBoxX.Show(i18N.Translate("Please press Stop button first"));

                        Visible = true;
                        ShowInTaskbar = true; // 显示在系统任务栏 
                        WindowState = FormWindowState.Normal; // 还原窗体 
                        NotifyIcon.Visible = true; // 托盘图标隐藏 

                        return;
                    }
                }
            }

            NotifyIcon.Visible = false;
            Hide();

            Task.Run(() =>
            {
                for (var i = 0; i < 16; i++)
                {
                    if (State == State.Waiting || State == State.Stopped)
                        break;
                    Thread.Sleep(250);
                }

                SaveConfigs();
                UpdateStatus(State.Terminating);
                Dispose();
                Environment.Exit(Environment.ExitCode);
            });
        }

        #region NotifyIcon

        private void ShowMainFormToolStripButton_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = true;
                ShowInTaskbar = true; // 显示在系统任务栏 
                WindowState = FormWindowState.Normal; // 还原窗体 
                NotifyIcon.Visible = true; // 托盘图标隐藏 
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
                NotifyIcon.Visible = true; //托盘图标隐藏 
            }

            Activate();
        }

        #endregion

        #endregion
    }
}
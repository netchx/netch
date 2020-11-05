using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        public MainForm()
        {
            InitializeComponent();

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
            AddAddServerToolStripMenuItems();

            #region i18N Translations

            _mainFormText.Add(UninstallServiceToolStripMenuItem.Name, new[] {"Uninstall {0}", "NF Service"});
            _mainFormText.Add(UninstallTapDriverToolStripMenuItem.Name, new[] {"Uninstall {0}", "TUN/TAP driver"});

            #endregion

            OnlyInstance.Called += OnCalled;
            // 计算 ComboBox绘制 目标宽度
            _eWidth = ServerComboBox.Width / 10;

            ModeHelper.Load();
            InitMode();
            InitServer();
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


            Task.Run(async () =>
            {
                // 检查订阅更新
                if (Global.Settings.UpdateSubscribeatWhenOpened)
                {
                    await UpdateServersFromSubscribe();
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
                InitMode();
                InitProfile();
            }

            if (ProfileButtons.Count != Global.Settings.ProfileCount)
                InitProfile();

            Show();
        }

        private readonly Dictionary<string, object> _mainFormText = new Dictionary<string, object>();
        private bool _textRecorded;

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
                    else
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

        private void Exit(bool forceExit = false)
        {
            if (!IsWaiting && !Global.Settings.StopWhenExited && !forceExit)
            {
                MessageBoxX.Show(i18N.Translate("Please press Stop button first"));

                NotifyIcon_MouseDoubleClick(null, null);
                return;
            }

            Hide();
            NotifyIcon.Visible = false;
            if (!IsWaiting)
            {
                ControlFun();
            }

            Configuration.Save();

            foreach (var file in new[] {"data\\last.json", "data\\privoxy.conf"})
            {
                if (File.Exists(file))
                    File.Delete(file);
            }

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

            Hide();
            var server = Global.Settings.Server[ServerComboBox.SelectedIndex];
            ServerHelper.GetUtilByTypeName(server.Type).Edit(server);
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
            if (ModeComboBox.SelectedIndex == -1)
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

            ModeHelper.Delete((Models.Mode) ModeComboBox.SelectedItem);
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

            try
            {
                //听说巨硬BUG经常会炸，所以Catch一下 :D
                var server = (Server) ServerComboBox.SelectedItem;
                string text;
                if (ModifierKeys == Keys.Control)
                {
                    text = ShareLink.GetNetchLink(server);
                }
                else
                {
                    text = ShareLink.GetShareLink(server);
                }

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

            var index = ServerComboBox.SelectedIndex;
            Global.Settings.Server.Remove(ServerComboBox.SelectedItem as Server);
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

        public void NotifyTip(string text, int timeout = 0, bool info = true)
        {
            // 会阻塞线程 timeout 秒
            NotifyIcon.ShowBalloonTip(timeout,
                UpdateChecker.Name,
                text,
                info ? ToolTipIcon.Info : ToolTipIcon.Error);
        }

        #endregion

        #endregion

        private bool _comboBoxInitialized;

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

        private void ServerComboBox_SelectedIndexChanged(object sender, EventArgs o)
        {
            if (!_comboBoxInitialized) return;
            Global.Settings.ServerComboBoxSelectedIndex = ServerComboBox.SelectedIndex;
        }

        private void NatTypeStatusLabel_Click(object sender, EventArgs e)
        {
            if (_state == State.Started && MainController.NttTested)
            {
                MainController.NatTest();
            }
        }
    }
}
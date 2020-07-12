using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Controllers;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class Dummy
    {
    }

    partial class MainForm
    {
        public void ControlFun()
        {
            SaveConfigs();
            if (State == State.Waiting || State == State.Stopped)
            {
                // 服务器、模式 需选择
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBoxX.Show(i18N.Translate("Please select a server first"));
                    return;
                }

                if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBoxX.Show(i18N.Translate("Please select an mode first"));
                    return;
                }

                //MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = false;

                UpdateStatus(State.Starting);

                Firewall.AddNetchFwRules();

                Task.Run(() =>
                {
                    var server = ServerComboBox.SelectedItem as Models.Server;
                    var mode = ModeComboBox.SelectedItem as Models.Mode;

                    MainController ??= new MainController();

                    var startResult = MainController.Start(server, mode);

                    if (startResult)
                    {
                        Task.Run(() =>
                        {
                            LastUploadBandwidth = 0;
                            //LastDownloadBandwidth = 0;
                            //UploadSpeedLabel.Text = "↑: 0 KB/s";
                            DownloadSpeedLabel.Text = "↑↓: 0 KB/s";
                            UsedBandwidthLabel.Text = $"{i18N.Translate("Used",": ")}0 KB";
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
                                    i18N.Translate(
                                        "Netch is now minimized to the notification bar, double click this icon to restore."),
                                    ToolTipIcon.Info);

                                IsFirstOpened = false;
                            }

                            Hide();
                        }

                        // TODO 是否需要移到一个函数中
                        var text = new StringBuilder(" (");
                        text.Append(Global.Settings.LocalAddress == "0.0.0.0"
                            ? i18N.Translate("Allow other Devices to connect") + " "
                            : "");
                        if (server.Type == "Socks5")
                        {
                            // 不可控Socks5
                            if (mode.Type == 3 || mode.Type == 5)
                            {
                                // 可控HTTP
                                text.Append(
                                    $"HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
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
                            text.Append(
                                $"Socks5 {i18N.Translate("Local Port", ": ")}{Global.Settings.Socks5LocalPort}");
                            if (mode.Type == 3 || mode.Type == 5)
                            {
                                //有HTTP
                                text.Append(
                                    $" | HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
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
                        StatusText(i18N.Translate("Start failed"));
                    }
                });
            }
            else
            {
                // 停止
                UpdateStatus(State.Stopping);
                MainController.Stop();
                UpdateStatus(State.Stopped);

                Task.Run(() =>
                {
                    TestServer();
                });
            }
        }

        public void OnBandwidthUpdated(long download)
        {
            try
            {
                UsedBandwidthLabel.Text = $"{i18N.Translate("Used", ": ")}{Bandwidth.Compute(download)}";
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

                UsedBandwidthLabel.Text =
                    $"{i18N.Translate("Used",": ")}{Bandwidth.Compute(upload + download)}";
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

        /// <summary>
        ///     上一次上传的流量
        /// </summary>
        public long LastUploadBandwidth;

        /// <summary>
        ///     上一次下载的流量
        /// </summary>
        public long LastDownloadBandwidth;
    }
}
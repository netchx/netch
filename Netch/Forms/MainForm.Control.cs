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
        private bool _isFirstCloseWindow = true;

        private void ControlFun()
        {
            //防止模式选择框变成蓝色:D
            ModeComboBox.Select(0, 0);

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

                UpdateStatus(State.Starting);

                Task.Run(() =>
                {
                    Task.Run(Firewall.AddNetchFwRules);

                    var server = ServerComboBox.SelectedItem as Models.Server;
                    var mode = ModeComboBox.SelectedItem as Models.Mode;

                    if (_mainController.Start(server, mode))
                    {
                        Task.Run(() =>
                        {
                            UpdateStatus(State.Started,
                                i18N.Translate(StateExtension.GetStatusString(State.Started)) + PortText(server.Type, mode.Type));

                            Bandwidth.NetTraffic(server, mode, _mainController);
                        });

                        // 如果勾选启动后最小化
                        if (Global.Settings.MinimizeWhenStarted)
                        {
                            WindowState = FormWindowState.Minimized;
                            NotifyIcon.Visible = true;

                            if (_isFirstCloseWindow)
                            {
                                // 显示提示语
                                NotifyIcon.ShowBalloonTip(5,
                                    UpdateChecker.Name,
                                    i18N.Translate(
                                        "Netch is now minimized to the notification bar, double click this icon to restore."),
                                    ToolTipIcon.Info);

                                _isFirstCloseWindow = false;
                            }

                            Hide();
                        }

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
                        UpdateStatus(State.Stopped, i18N.Translate("Start failed"));
                    }
                });
            }
            else
            {
                // 停止
                UpdateStatus(State.Stopping);
                Task.Run(() =>
                {
                    _mainController.Stop();
                    UpdateStatus(State.Stopped);

                    TestServer();
                });
            }
        }

        private string PortText(string serverType, int modeType)
        {
            var text = new StringBuilder(" (");
            text.Append(Global.Settings.LocalAddress == "0.0.0.0"
                ? i18N.Translate("Allow other Devices to connect") + " "
                : "");
            if (serverType == "Socks5")
            {
                // 不可控Socks5
                if (modeType == 3 || modeType == 5)
                {
                    // 可控HTTP
                    text.Append(
                        $"HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
                }
                else
                {
                    // 不可控HTTP
                    return "";
                }
            }
            else
            {
                // 可控Socks5
                text.Append(
                    $"Socks5 {i18N.Translate("Local Port", ": ")}{Global.Settings.Socks5LocalPort}");
                if (modeType == 3 || modeType == 5)
                {
                    //有HTTP
                    text.Append(
                        $" | HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
                }
            }

            text.Append(")");
            return text.ToString();
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
                    $"{i18N.Translate("Used", ": ")}{Bandwidth.Compute(upload + download)}";
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
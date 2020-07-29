using System;
using System.IO;
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
                    MessageBoxX.Show(i18N.Translate("Please select a mode first"));
                    return;
                }

                State = State.Starting;

                // 清除模式搜索框文本选择
                ModeComboBox.Select(0, 0);

                Task.Run(() =>
                {
                    Task.Run(Firewall.AddNetchFwRules);

                    var server = ServerComboBox.SelectedItem as Models.Server;
                    var mode = ModeComboBox.SelectedItem as Models.Mode;
                    var result = false;

                    try
                    {
                        // TODO 完善控制器异常处理
                        result = _mainController.Start(server, mode);
                    }
                    catch (Exception e)
                    {
                        if (e is DllNotFoundException || e is FileNotFoundException)
                            MessageBoxX.Show(e.Message + "\n\n" + i18N.Translate("Missing File or runtime components"), owner: this);

                        Netch.Application_OnException(this, new ThreadExceptionEventArgs(e));
                    }

                    if (result)
                    {
                        Task.Run(() =>
                        {
                            State = State.Started;
                            StatusTextAppend(LocalPortText(server.Type, mode.Type));
                            Bandwidth.NetTraffic(server, mode, _mainController);
                        });
                        // 如果勾选启动后最小化
                        if (Global.Settings.MinimizeWhenStarted)
                        {
                            WindowState = FormWindowState.Minimized;

                            if (_isFirstCloseWindow)
                            {
                                // 显示提示语
                                NotifyTip(i18N.Translate("Netch is now minimized to the notification bar, double click this icon to restore."));
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
                        State = State.Stopped;
                        StatusText(i18N.Translate("Start failed"));
                    }
                });
            }
            else
            {
                Task.Run(() =>
                {
                    // 停止
                    State = State.Stopping;
                    _mainController.Stop();
                    State = State.Stopped;
                });
                Task.Run(TestServer);
            }
        }

        private static string LocalPortText(string serverType, int modeType)
        {
            var text = new StringBuilder(" (");
            if (Global.Settings.LocalAddress == "0.0.0.0")
                text.Append(i18N.Translate("Allow other Devices to connect") + " ");
            if (serverType == "Socks5")
            {
                // 不可控Socks5
                if (modeType == 3 || modeType == 5)
                {
                    // 可控HTTP
                    MainController.UsingPorts.Add(Global.Settings.HTTPLocalPort);
                    text.Append($"HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
                }
                else
                {
                    // 不可控HTTP
                    return string.Empty;
                }
            }
            else
            {
                // 可控Socks5
                MainController.UsingPorts.Add(Global.Settings.Socks5LocalPort);
                text.Append($"Socks5 {i18N.Translate("Local Port", ": ")}{Global.Settings.Socks5LocalPort}");
                if (modeType == 3 || modeType == 5)
                {
                    // 有HTTP
                    MainController.UsingPorts.Add(Global.Settings.HTTPLocalPort);
                    text.Append($" | HTTP {i18N.Translate("Local Port", ": ")}{Global.Settings.HTTPLocalPort}");
                }
            }

            if (modeType == 0)
                MainController.UsingPorts.Add(Global.Settings.RedirectorTCPPort);

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
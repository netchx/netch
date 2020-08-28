using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private async void ControlFun()
        {
            Configuration.Save();
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

                // 清除模式搜索框文本选择
                ModeComboBox.Select(0, 0);

                State = State.Starting;

                var server = ServerComboBox.SelectedItem as Models.Server;
                var mode = ModeComboBox.SelectedItem as Models.Mode;

                if (await _mainController.Start(server, mode))
                {
                    State = State.Started;
                    _ = Task.Run(() => { Bandwidth.NetTraffic(server, mode, ref _mainController); });
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
                        _ = Task.Run(() =>
                        {
                            while (State == State.Started)
                            {
                                server.Test();
                                // 重绘 ServerComboBox
                                ServerComboBox.Invalidate();

                                Thread.Sleep(Global.Settings.StartedTcping_Interval * 1000);
                            }
                        });
                    }
                }
                else
                {
                    State = State.Stopped;
                    StatusText(i18N.Translate("Start failed"));
                }
            }
            else
            {
                // 停止
                State = State.Stopping;
                await _mainController.Stop();
                State = State.Stopped;
                _ = Task.Run(TestServer);
            }
        }

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

        /// <summary>
        ///     上一次上传的流量
        /// </summary>
        public ulong LastUploadBandwidth;

        /// <summary>
        ///     上一次下载的流量
        /// </summary>
        public ulong LastDownloadBandwidth;
    }
}
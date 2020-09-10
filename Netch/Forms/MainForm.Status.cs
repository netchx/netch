using System;
using System.Drawing;
using System.Text;
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
                        updateACLWithProxyToolStripMenuItem.Enabled =
                            UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled =
                                reinstallTapDriverToolStripMenuItem.Enabled =
                                    ReloadModesToolStripMenuItem.Enabled = enabled;
                }

                _state = value;

                StatusText(i18N.Translate(StateExtension.GetStatusString(value)));
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

                        UsedBandwidthLabel.Visible /*= UploadSpeedLabel.Visible*/ = DownloadSpeedLabel.Visible = Bandwidth.NetTrafficAvailable;
                        break;
                    case State.Stopping:
                        ControlButton.Enabled = false;
                        ControlButton.Text = "...";

                        ProfileGroupBox.Enabled = false;
                        UsedBandwidthLabel.Visible /*= UploadSpeedLabel.Visible*/ = DownloadSpeedLabel.Visible = false;
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
                    case State.Terminating:
                        Dispose();
                        Environment.Exit(Environment.ExitCode);
                        return;
                }
            }
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

                if (int.TryParse(text, out var natType))
                {
                    if (natType > 0 && natType < 5)
                    {
                        NatTypeStatusLightLabel.Visible = true;
                        UpdateNatTypeLight(natType);
                    }
                }
                else
                {
                    NatTypeStatusLightLabel.Visible = false;
                }
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
        private void UpdateNatTypeLight(int natType)
        {
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

        /// <summary>
        ///     更新状态栏文本
        /// </summary>
        /// <param name="text"></param>
        public void StatusText(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(StatusText), text);
                return;
            }

            StatusLabel.Text = i18N.Translate("Status", ": ") + text;
        }

        public void StatusTextAppend(string text)
        {
            StatusLabel.Text += text;
        }

        public static class StatusPortInfoText
        {
            public static int Socks5Port = 0;
            public static int HttpPort = 0;
            public static bool ShareLan = false;

            public static string Value
            {
                get
                {
                    if (Socks5Port == 0 && HttpPort == 0)
                        return string.Empty;

                    var text = new StringBuilder();
                    if (ShareLan)
                        text.Append(i18N.Translate("Allow other Devices to connect") + " ");

                    if (Socks5Port != 0)
                        text.Append($"Socks5 {i18N.Translate("Local Port", ": ")}{Socks5Port}");

                    if (HttpPort != 0)
                    {
                        if (Socks5Port != 0)
                            text.Append(" | ");
                        text.Append($"HTTP {i18N.Translate("Local Port", ": ")}{HttpPort}");
                    }

                    return $" ({text})";
                }
            }
        }
    }
}
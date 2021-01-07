using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class Dummy
    {
    }

    partial class MainForm
    {
        private bool IsWaiting => State == State.Waiting || State == State.Stopped;

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
                    case State.Terminating:
                        Dispose();
                        Environment.Exit(Environment.ExitCode);
                        return;
                }
            }
        }

        public void BandwidthState(bool state)
        {
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

            public static void UpdateShareLan() => _shareLan = Global.Settings.LocalAddress != "127.0.0.1";

            public static string Value
            {
                get
                {
                    var strings = new List<string>();

                    if (_socks5Port != null)
                    {
                        strings.Add($"Socks5 {i18N.Translate("Local Port", ": ")}{_socks5Port}");
                    }

                    if (_httpPort != null)
                    {
                        strings.Add($"HTTP {i18N.Translate("Local Port", ": ")}{_httpPort}");
                    }

                    if (!strings.Any())
                        return string.Empty;

                    return $" ({(_shareLan ? i18N.Translate("Allow other Devices to connect") + " " : "")}{string.Join(" | ", strings)})";
                }
            }

            public static void Reset()
            {
                _httpPort = _socks5Port = null;
            }
        }
    }
}
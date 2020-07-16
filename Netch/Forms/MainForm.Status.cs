using System;
using System.Drawing;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class Dummy
    {
    }

    partial class MainForm
    {
        /// <summary>
        ///     当前状态
        /// </summary>
        public State State { get; private set; } = State.Waiting;

        public void NatTypeStatusText(string text = "", string country = "")
        {
            if (State != State.Started)
            {
                NatTypeStatusLabel.Text = "";
                NatTypeStatusLabel.Visible = NatTypeStatusLightLabel.Visible = false;
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                if (country != "")
                {
                    NatTypeStatusLabel.Text = String.Format("NAT{0}{1}[{2}]", i18N.Translate(": "), text, country);
                }
                else
                {
                    NatTypeStatusLabel.Text = String.Format("NAT{0}{1}", i18N.Translate(": "), text);
                }
                if (Enum.TryParse(text, false, out STUN_Client.NatType natType))
                {
                    NatTypeStatusLightLabel.Visible = true;
                    UpdateNatTypeLight(natType);
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
        private void UpdateNatTypeLight(STUN_Client.NatType natType)
        {
            Color c;
            switch (natType)
            {
                case STUN_Client.NatType.UdpBlocked:
                case STUN_Client.NatType.SymmetricUdpFirewall:
                case STUN_Client.NatType.Symmetric:
                    c = Color.Red;
                    break;
                case STUN_Client.NatType.RestrictedCone:
                case STUN_Client.NatType.PortRestrictedCone:
                    c = Color.Yellow;
                    break;
                case STUN_Client.NatType.OpenInternet:
                case STUN_Client.NatType.FullCone:
                    c = Color.LimeGreen;
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
            StatusLabel.Text = i18N.Translate("Status", ": ") + text;
        }

        /// <summary>
        ///     更新 UI, 状态栏文本, 状态
        /// </summary>
        /// <param name="state"></param>
        /// <param name="text"></param>
        private void UpdateStatus(State state, string text = "")
        {
            State = state;
            StatusText(text == "" ? i18N.Translate(StateExtension.GetStatusString(state)) : text);

            void MenuStripsEnabled(bool enabled)
            {
                // 需要禁用的菜单项
                UninstallServiceToolStripMenuItem.Enabled =
                    updateACLWithProxyToolStripMenuItem.Enabled =
                        UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled =
                            reinstallTapDriverToolStripMenuItem.Enabled = enabled;
            }

            // TODO 补充
            switch (state)
            {
                case State.Waiting:
                    ControlButton.Enabled = true;
                    ControlButton.Text = i18N.Translate("Start");

                    break;
                case State.Starting:
                    ControlButton.Enabled = false;
                    ControlButton.Text = "...";

                    ConfigurationGroupBox.Enabled = false;

                    MenuStripsEnabled(false);
                    break;
                case State.Started:
                    ControlButton.Enabled = true;
                    ControlButton.Text = i18N.Translate("Stop");

                    LastUploadBandwidth = 0;
                    //LastDownloadBandwidth = 0;
                    //UploadSpeedLabel.Text = "↑: 0 KB/s";
                    DownloadSpeedLabel.Text = @"↑↓: 0 KB/s";
                    UsedBandwidthLabel.Text = $@"{i18N.Translate("Used", ": ")}0 KB";
                    UsedBandwidthLabel.Visible /*= UploadSpeedLabel.Visible*/ = DownloadSpeedLabel.Visible = true;
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

                    ProfileGroupBox.Enabled = true;
                    ConfigurationGroupBox.Enabled = true;

                    MenuStripsEnabled(true);
                    break;
                case State.Terminating:

                    break;
            }
        }

        /// <summary>
        ///     刷新 UI
        /// </summary>
        private void UpdateStatus()
        {
            UpdateStatus(State);
        }
    }
}
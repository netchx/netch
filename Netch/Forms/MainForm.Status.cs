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

        public void NatTypeStatusText(string text = "",string Country = "")
        {
            if (State != State.Started)
            {
                NatTypeStatusLabel.Text = "";
                NatTypeStatusLabel.Visible = NatTypeStatusLightLabel.Visible = false;
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                if (Country != "")
                {
                    NatTypeStatusLabel.Text = String.Format("NAT{0}{1}[{2}]", i18N.Translate(": "), text, Country);
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
                NatTypeStatusLabel.Text = "NAT" + i18N.Translate(": ") + i18N.Translate("Test failed");
            }

            NatTypeStatusLabel.Visible = true;
        }

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


        public void StatusText(string text)
        {
            StatusLabel.Text = i18N.Translate("Status", ": ") + text;
        }

        /// <summary>
        ///     Update UI, Status, Status Label
        /// </summary>
        /// <param name="state"></param>
        public void UpdateStatus(State state)
        {
            State = state;
            StatusText(i18N.Translate(StateExtension.GetStatusString(state)));
            // TODO 补充
            switch (state)
            {
                case State.Waiting:
                    ControlButton.Enabled = true;
                    ControlButton.Text = i18N.Translate("Start");

                    MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
                    updateACLWithProxyToolStripMenuItem.Enabled = true;

                    NatTypeStatusText();
                    break;
                case State.Starting:
                    ControlButton.Enabled = false;
                    ControlButton.Text = "...";

                    ServerComboBox.Enabled = false;
                    ModeComboBox.Enabled = false;

                    UninstallServiceToolStripMenuItem.Enabled = false;
                    updateACLWithProxyToolStripMenuItem.Enabled = false;
                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = false;
                    reinstallTapDriverToolStripMenuItem.Enabled = false;
                    break;
                case State.Started:
                    ControlButton.Enabled = true;
                    ControlButton.Text = i18N.Translate("Stop");
                    break;
                case State.Stopping:
                    ControlButton.Enabled = false;
                    ControlButton.Text = "...";

                    ProfileGroupBox.Enabled = false;
                    MenuStrip.Enabled = ConfigurationGroupBox.Enabled = SettingsButton.Enabled = true;
                    UsedBandwidthLabel.Visible = UploadSpeedLabel.Visible = DownloadSpeedLabel.Visible = false;
                    NatTypeStatusText();
                    break;
                case State.Stopped:
                    ControlButton.Enabled = true;
                    ControlButton.Text = i18N.Translate("Start");

                    LastUploadBandwidth = 0;
                    LastDownloadBandwidth = 0;

                    ServerComboBox.Enabled = true;
                    ModeComboBox.Enabled = true;
                    ProfileGroupBox.Enabled = true;

                    UninstallServiceToolStripMenuItem.Enabled = true;
                    updateACLWithProxyToolStripMenuItem.Enabled = true;
                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = true;
                    reinstallTapDriverToolStripMenuItem.Enabled = true;
                    break;
                case State.Terminating:

                    break;
            }
        }

        public void UpdateStatus()
        {
            UpdateStatus(State);
        }
    }
}
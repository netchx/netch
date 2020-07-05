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

        public void NatTypeStatusText(string text = "")
        {
            if (State != State.Started)
            {
                NatTypeStatusLabel.Text = "";
                NatTypeStatusLabel.Visible = true;
                return;
            }

            NatTypeStatusLabel.Text = "NAT" + i18N.Translate(": ") +
                                      (!string.IsNullOrEmpty(text) ? text.Trim() : i18N.Translate("Test failed"));
            NatTypeStatusLabel.Visible = true;
        }

        public void StatusText(string text)
        {
            StatusLabel.Text = i18N.Translate("Status", ": ") + text;
        }

        /// <summary>
        /// Update UI, Status, Status Label
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
                    ControlButton.Text = i18N.Translate("Start");
                    ControlButton.Enabled = true;
                    
                    MenuStrip.Enabled = ConfigurationGroupBox.Enabled = ControlButton.Enabled = SettingsButton.Enabled = true;
                    updateACLWithProxyToolStripMenuItem.Enabled = true;
                    
                    NatTypeStatusText();
                    break;
                case State.Starting:
                    ControlButton.Text = "...";
                    ControlButton.Enabled = false;

                    ServerComboBox.Enabled = false;
                    ModeComboBox.Enabled = false;

                    UninstallServiceToolStripMenuItem.Enabled = false;
                    updateACLWithProxyToolStripMenuItem.Enabled = false;
                    UpdateServersFromSubscribeLinksToolStripMenuItem.Enabled = false;
                    reinstallTapDriverToolStripMenuItem.Enabled = false;
                    break;
                case State.Started:
                    ControlButton.Text = i18N.Translate("Stop");
                    ControlButton.Enabled = true;
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
                    ControlButton.Text = i18N.Translate("Start");
                    ControlButton.Enabled = true;
                    
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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class Dummy { }
    partial class MainForm
    {
        /// init at <see cref="MainForm_Load"/> 
        private int _sizeHeight;

        private int _profileConfigurationHeight;
        private int _profileGroupboxHeight;
        private int _configurationGroupBoxHeight;

        private void InitProfile()
        {
            // Clear
            foreach (var button in ProfileButtons)
                button.Dispose();

            ProfileButtons.Clear();
            ProfileTable.ColumnStyles.Clear();
            ProfileTable.RowStyles.Clear();

            var numProfile = Global.Settings.ProfileCount;
            if (numProfile == 0)
            {
                // Hide Profile GroupBox, Change window size
                configLayoutPanel.RowStyles[2].SizeType = SizeType.Percent;
                configLayoutPanel.RowStyles[2].Height = 0;
                ProfileGroupBox.Visible = false;

                ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, _configurationGroupBoxHeight - _profileConfigurationHeight);
                Size = new Size(Size.Width, _sizeHeight - (_profileConfigurationHeight + _profileGroupboxHeight));
            }
            else
            {
                // Load Profiles
                ProfileTable.ColumnCount = numProfile;

                while (Global.Settings.Profiles.Count < numProfile)
                {
                    Global.Settings.Profiles.Add(new Profile());
                }

                for (var i = 0; i < numProfile; ++i)
                {
                    var b = new Button();
                    b.Click += ProfileButton_Click;
                    b.Dock = DockStyle.Fill;
                    b.Text = !Global.Settings.Profiles[i].IsDummy ? Global.Settings.Profiles[i].ProfileName : i18N.Translate("None");

                    ProfileTable.Controls.Add(b, i, 0);
                    ProfileButtons.Add(b);
                }

                // equal column
                for (var i = 1; i <= ProfileTable.RowCount; i++)
                {
                    ProfileTable.RowStyles.Add(new RowStyle(SizeType.Percent, 1));
                }

                for (var i = 1; i <= ProfileTable.ColumnCount; i++)
                {
                    ProfileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
                }

                if (Size.Height == _sizeHeight) return;
                configLayoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
                ProfileGroupBox.Visible = true;
                ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, _configurationGroupBoxHeight);
                Size = new Size(Size.Width, _sizeHeight);
            }
        }

        private string LoadProfile(int index)
        {
            var p = Global.Settings.Profiles[index];

            if (p.IsDummy)
                throw new Exception("Profile not found.");

            var result = false;

            foreach (Models.Server server in ServerComboBox.Items)
            {
                if (server.Remark.Equals(p.ServerRemark))
                {
                    ServerComboBox.SelectedItem = server;
                    result = true;
                    break;
                }
            }

            if (!result)
                throw new Exception("Server not found.");

            result = false;
            foreach (Models.Mode mode in ModeComboBox.Items)
            {
                if (mode.Remark.Equals(p.ModeRemark))
                {
                    ModeComboBox.SelectedItem = mode;
                    result = true;
                    break;
                }
            }

            if (!result)
                throw new Exception("Mode not found.");

            return p.ProfileName;
        }

        private void SaveProfile(int index)
        {
            var selectedServer = (Models.Server) ServerComboBox.SelectedItem;
            var selectedMode = (Models.Mode) ModeComboBox.SelectedItem;
            var name = ProfileNameText.Text;

            Global.Settings.Profiles[index] = new Profile(selectedServer, selectedMode, name);
        }

        private void RemoveProfile(int index)
        {
            Global.Settings.Profiles[index] = new Profile();
        }


        private List<Button> ProfileButtons = new List<Button>();

        private void ProfileButton_Click(object sender, EventArgs e)
        {
            var index = ProfileButtons.IndexOf((Button) sender);

            if (ModifierKeys == Keys.Control)
            {
                if (ServerComboBox.SelectedIndex == -1)
                {
                    MessageBoxX.Show(i18N.Translate("Please select a server first"));
                }
                else if (ModeComboBox.SelectedIndex == -1)
                {
                    MessageBoxX.Show(i18N.Translate("Please select an mode first"));
                }
                else if (ProfileNameText.Text == "")
                {
                    MessageBoxX.Show(i18N.Translate("Please enter a profile name first"));
                }
                else
                {
                    SaveProfile(index);
                    ProfileButtons[index].Text = ProfileNameText.Text;
                }
            }
            else if (ModifierKeys == Keys.Shift)
            {
                if (MessageBoxX.Show(i18N.Translate("Remove this Profile?"), confirm: true) == DialogResult.OK)
                {
                    RemoveProfile(index);
                    ProfileButtons[index].Text = i18N.Translate("None");
                    MessageBoxX.Show(i18N.Translate("Profile Removed!"));
                }
            }
            else
            {
                if (Global.Settings.Profiles[index].IsDummy)
                {
                    MessageBoxX.Show(i18N.Translate("No saved profile here. Save a profile first by Ctrl+Click on the button"));
                    return;
                }

                try
                {
                    ProfileNameText.Text = LoadProfile(index);

                    // start the profile
                    ControlFun();
                    if (State == State.Stopping || State == State.Stopped)
                    {
                        Task.Run(() =>
                        {
                            while (State != State.Stopped)
                            {
                                Thread.Sleep(250);
                            }

                            ControlButton.PerformClick();
                        });
                    }
                }
                catch (Exception ee)
                {
                    Task.Run(() =>
                    {
                        Logging.Info(ee.Message);
                        ProfileButtons[index].Text = i18N.Translate("Error");
                        Thread.Sleep(1200);
                        ProfileButtons[index].Text = i18N.Translate("None");
                    });
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private int _configurationGroupBoxHeight;
        private int _profileConfigurationHeight;

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

                configLayoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
                ProfileGroupBox.Visible = true;
                ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, _configurationGroupBoxHeight);
            }
        }

        private void LoadProfile(int index)
        {
            var p = Global.Settings.Profiles[index];
            ProfileNameText.Text = p.ProfileName;
            ModeComboBox.ResetCompletionList();

            if (p.IsDummy)
                throw new Exception("Profile not found.");

            var server = ServerComboBox.Items.Cast<Server>().FirstOrDefault(s => s.Remark.Equals(p.ServerRemark));
            var mode = ModeComboBox.Items.Cast<Models.Mode>().FirstOrDefault(m => m.Remark.Equals(p.ModeRemark));

            if (server == null)
            {
                throw new Exception("Server not found.");
            }

            if (mode == null)
            {
                throw new Exception("Mode not found.");
            }

            ServerComboBox.SelectedItem = server;
            ModeComboBox.SelectedItem = mode;
        }

        private void SaveProfile(int index)
        {
            var selectedServer = (Server) ServerComboBox.SelectedItem;
            var selectedMode = (Models.Mode) ModeComboBox.SelectedItem;
            var name = ProfileNameText.Text;

            Global.Settings.Profiles[index] = new Profile(selectedServer, selectedMode, name);
        }

        private void RemoveProfile(int index)
        {
            Global.Settings.Profiles[index] = new Profile();
        }


        private List<Button> ProfileButtons = new List<Button>();

        private async void ProfileButton_Click(object sender, EventArgs e)
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
                    MessageBoxX.Show(i18N.Translate("Please select a mode first"));
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

                return;
            }

            if (Global.Settings.Profiles[index].IsDummy)
            {
                MessageBoxX.Show(
                    i18N.Translate("No saved profile here. Save a profile first by Ctrl+Click on the button"));
                return;
            }

            if (ModifierKeys == Keys.Shift)
            {
                if (MessageBoxX.Show(i18N.Translate("Remove this Profile?"), confirm: true) != DialogResult.OK) return;
                RemoveProfile(index);
                ProfileButtons[index].Text = i18N.Translate("None");
                return;
            }

            try
            {
                LoadProfile(index);
            }
            catch (Exception exception)
            {
                MessageBoxX.Show(exception.Message, LogLevel.ERROR);
                return;
            }

            // start the profile
            ControlFun();
            if (State == State.Stopping || State == State.Stopped)
            {
                while (State != State.Stopped)
                {
                    await Task.Delay(250);
                }

                ControlFun();
            }
        }
    }
}
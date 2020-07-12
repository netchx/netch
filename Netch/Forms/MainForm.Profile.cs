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
        private int _controlHeight;
        private int _profileBoxHeight;
        private int _configurationGroupBoxHeight;

        private void InitProfile()
        {
            foreach (var button in ProfileButtons)
            {
                button.Dispose();
            }

            ProfileButtons.Clear();
            ProfileTable.ColumnStyles.Clear();
            ProfileTable.RowStyles.Clear();

            var numProfile = Global.Settings.ProfileCount;
            if (numProfile == 0)
            {
                configLayoutPanel.RowStyles[2].SizeType = SizeType.Percent;
                configLayoutPanel.RowStyles[2].Height = 0;
                ProfileGroupBox.Visible = false;

                ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, _configurationGroupBoxHeight - _controlHeight);
                Size = new Size(Size.Width, _sizeHeight - (_controlHeight + _profileBoxHeight));

                return;
            }

            configLayoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
            ProfileGroupBox.Visible = true;
            ConfigurationGroupBox.Size = new Size(ConfigurationGroupBox.Size.Width, _configurationGroupBoxHeight);
            Size = new Size(Size.Width, _sizeHeight);


            ProfileTable.ColumnCount = numProfile;

            while (Global.Settings.Profiles.Count < numProfile)
            {
                Global.Settings.Profiles.Add(new Profile());
            }

            // buttons
            for (var i = 0; i < numProfile; ++i)
            {
                var b = new Button();
                ProfileTable.Controls.Add(b, i, 0);
                b.Location = new Point(i * 100, 0);
                b.Click += ProfileButton_Click;
                b.Dock = DockStyle.Fill;
                b.Text = !Global.Settings.Profiles[i].IsDummy ? Global.Settings.Profiles[i].ProfileName : i18N.Translate("None");

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

        public List<Button> ProfileButtons = new List<Button>();

        private void ProfileButton_Click(object sender, EventArgs e)
        {
            var index = ProfileButtons.IndexOf((Button) sender);

            //Utils.Logging.Info(String.Format("Button no.{0} clicked", index));

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
            else
            {
                if (ProfileButtons[index].Text == i18N.Translate("Error") || ProfileButtons[index].Text == i18N.Translate("None"))
                {
                    MessageBoxX.Show(i18N.Translate("No saved profile here. Save a profile first by Ctrl+Click on the button"));
                }

                try
                {
                    ProfileNameText.Text = LoadProfile(index);

                    // start the profile
                    var need2ndStart = true;
                    if (State == State.Waiting || State == State.Stopped)
                    {
                        need2ndStart = false;
                    }

                    ControlButton.PerformClick();

                    if (need2ndStart)
                    {
                        Task.Run(() =>
                        {
                            while (State != State.Stopped)
                            {
                                Thread.Sleep(200);
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
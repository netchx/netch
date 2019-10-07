using System;
using System.IO;
using System.Windows.Forms;

namespace Netch.Forms.Mode
{
    public partial class Process : Form
    {
        public Process()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
        }

        /// <summary>
		///		扫描目录
		/// </summary>
		/// <param name="DirName">路径</param>
		public void ScanDirectory(String DirName)
        {
            try
            {
                DirectoryInfo RDirInfo = new DirectoryInfo(DirName);
                if (!RDirInfo.Exists)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }

            System.Collections.Generic.Stack<string> DirStack = new System.Collections.Generic.Stack<string>();
            DirStack.Push(DirName);

            while (DirStack.Count > 0)
            {
                DirectoryInfo DirInfo = new DirectoryInfo(DirStack.Pop());
                foreach (DirectoryInfo DirChildInfo in DirInfo.GetDirectories())
                {
                    DirStack.Push(DirChildInfo.FullName);
                }
                foreach (FileInfo FileChildInfo in DirInfo.GetFiles())
                {
                    if (FileChildInfo.Name.EndsWith(".exe") && !RuleListBox.Items.Contains(FileChildInfo.Name))
                    {
                        RuleListBox.Items.Add(FileChildInfo.Name);
                    }
                }
            }
        }

        private void ModeForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate("Create Process Mode");
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            RemarkLabel.Text = Utils.i18N.Translate("Remark");
            FilenameLabel.Text = Utils.i18N.Translate("Filename");
            UseCustomFilenameBox.Text = Utils.i18N.Translate("Use Custom Filename");
            StaySameButton.Text = Utils.i18N.Translate("Stay the same");
            TimeDataButton.Text = Utils.i18N.Translate("Time data");
            AddButton.Text = Utils.i18N.Translate("Add");
            ScanButton.Text = Utils.i18N.Translate("Scan");
            ControlButton.Text = Utils.i18N.Translate("Save");

            if (Global.Settings.ModeFileNameType == 0)
            {
                UseCustomFilenameBox.Checked = true;
                StaySameButton.Enabled = false;
                TimeDataButton.Enabled = false;
            }
            else if (Global.Settings.ModeFileNameType == 1)
            {
                FilenameTextBox.Enabled = false;
                FilenameLabel.Enabled = false;
                StaySameButton.Checked = true;
            }
            else
            {
                FilenameTextBox.Enabled = false;
                FilenameLabel.Enabled = false;
                TimeDataButton.Checked = true;
            }
        }

        private void ModeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(ProcessNameTextBox.Text))
            {
                var process = ProcessNameTextBox.Text;
                if (!process.EndsWith(".exe"))
                {
                    process += ".exe";
                }

                if (!RuleListBox.Items.Contains(process))
                {
                    RuleListBox.Items.Add(process);
                }

                ProcessNameTextBox.Text = String.Empty;
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please enter an process name (xxx.exe)"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderSelect.FolderSelectDialog();
            dialog.Title = Utils.i18N.Translate("Select a folder");
            if (dialog.ShowDialog(Win32Native.GetForegroundWindow()))
            {
                ScanDirectory(dialog.FileName);
                MessageBox.Show(Utils.i18N.Translate("Scan completed"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            // 自定义文件名
            if (UseCustomFilenameBox.Checked)
            {
                Global.Settings.ModeFileNameType = 0;
            }
            // 使用和备注一致的文件名
            else if (StaySameButton.Checked)
            {
                Global.Settings.ModeFileNameType = 1;
                FilenameTextBox.Text = RemarkTextBox.Text;
            }
            // 使用时间数据作为文件名
            else
            {
                Global.Settings.ModeFileNameType = 2;
                FilenameTextBox.Text = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
            }

            Utils.Configuration.Save();

            if (!String.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                var ModeFilename = Path.Combine("mode", FilenameTextBox.Text);

                // 如果文件已存在，返回
                if (File.Exists(ModeFilename + ".txt"))
                {
                    MessageBox.Show(Utils.i18N.Translate("File already exists.\n Please Change the filename"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (RuleListBox.Items.Count != 0)
                {
                    var mode = new Models.Mode()
                    {
                        BypassChina = false,
                        FileName = ModeFilename,
                        Type = 0,
                        Remark = RemarkTextBox.Text
                    };

                    var text = $"# {RemarkTextBox.Text}, 0\r\n";
                    foreach (var item in RuleListBox.Items)
                    {
                        var process = item as String;
                        mode.Rule.Add(process);
                        text += process + "\r\n";
                    }

                    text = text.Substring(0, text.Length - 2);

                    if (!Directory.Exists("mode"))
                    {
                        Directory.CreateDirectory("mode");
                    }

                    File.WriteAllText(ModeFilename + ".txt", text);

                    MessageBox.Show(Utils.i18N.Translate("Mode added successfully"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Global.MainForm.AddMode(mode);
                    Close();
                }
                else
                {
                    MessageBox.Show(Utils.i18N.Translate("Unable to add empty rule"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(Utils.i18N.Translate("Please enter a mode remark"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void UseCustomFileNameBox_CheckedChanged(object sender, EventArgs e)
        {
            if (UseCustomFilenameBox.Checked)
            {
                StaySameButton.Enabled = false;
                TimeDataButton.Enabled = false;
                FilenameTextBox.Enabled = true;
                FilenameLabel.Enabled = true;
            }
            else
            {
                StaySameButton.Enabled = true;
                TimeDataButton.Enabled = true;
                FilenameTextBox.Enabled = false;
                FilenameLabel.Enabled = false;
            }
        }
    }
}

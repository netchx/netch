using System;
using System.IO;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class ModeForm : Form
    {
        public ModeForm()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;
        }

        /// <summary>
		///		扫描目录
		/// </summary>
		/// <param name="info">路径</param>
		public void ScanDirectory(FileSystemInfo info)
        {
            if (!info.Exists)
            {
                return;
            }

            var dir = info as DirectoryInfo;
            if (dir == null)
            {
                return;
            }

            FileSystemInfo[] files = dir.GetFileSystemInfos();
            foreach (var f in files)
            {
                if (f is FileInfo file && !RuleListBox.Items.Contains(file.Name))
                {
                    if (file.Name.EndsWith(".exe"))
                    {
                        RuleListBox.Items.Add(file.Name);
                    }
                }
                else
                {
                    ScanDirectory(f);
                }
            }
        }

        private void ModeForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate("Fast Create Mode");
            ConfigurationGroupBox.Text = Utils.i18N.Translate("Configuration");
            RemarkLabel.Text = Utils.i18N.Translate("Remark");
            AddButton.Text = Utils.i18N.Translate("Add");
            ScanButton.Text = Utils.i18N.Translate("Scan");
            ControlButton.Text = Utils.i18N.Translate("Save");
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
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ScanDirectory(new DirectoryInfo(dialog.SelectedPath));

                    MessageBox.Show(Utils.i18N.Translate("Scan completed"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                if (RuleListBox.Items.Count != 0)
                {
                    var text = "# " + RemarkTextBox.Text + "\r\n";
                    foreach (var item in RuleListBox.Items)
                    {
                        var process = item as String;

                        text += process + "\r\n";
                    }

                    text = text.Substring(0, text.Length - 2);

                    if (!Directory.Exists("mode"))
                    {
                        Directory.CreateDirectory("mode");
                    }

                    File.WriteAllText("mode\\" + (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds + ".txt", text);

                    MessageBox.Show(Utils.i18N.Translate("Mode added successfully"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Global.MainForm.InitMode();
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
    }
}

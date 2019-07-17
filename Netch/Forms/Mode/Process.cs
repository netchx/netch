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
            if (!String.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                if (RuleListBox.Items.Count != 0)
                {
                    var text = String.Format("# {0}, 0\r\n", RemarkTextBox.Text);
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

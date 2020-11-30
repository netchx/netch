using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Netch.Controllers;
using Netch.Utils;

namespace Netch.Forms.Mode
{
    public partial class Process : Form
    {
        /// <summary>
        ///     被编辑的模式
        /// </summary>
        private readonly Models.Mode _mode;

        /// <summary>
        ///     是否被编辑过
        /// </summary>
        public bool Edited { get; private set; }

        /// <summary>
        ///		编辑模式
        /// </summary>
        /// <param name="mode">模式</param>
        public Process(Models.Mode mode)
        {
            if (mode.Type != 0)
            {
                throw new Exception("请传入进程模式");
            }

            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            Text = "Edit Process Mode";
            _mode = mode;
            RuleListBox.Items.AddRange(mode.Rule.ToArray());

            #region 禁用文件名更改

            RemarkTextBox.TextChanged -= RemarkTextBox_TextChanged;
            FilenameTextBox.Enabled =
                UseCustomFilenameBox.Enabled = false;

            #endregion

            FilenameTextBox.Text = mode.FileName;
            RemarkTextBox.Text = mode.Remark;
        }

        public Process()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            FilenameTextBox.Enabled = false;
        }

        /// <summary>
        ///		扫描目录
        /// </summary>
        /// <param name="DirName">路径</param>
        public void ScanDirectory(string DirName)
        {
            try
            {
                var RDirInfo = new DirectoryInfo(DirName);
                if (!RDirInfo.Exists)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }

            var DirStack = new Stack<string>();
            DirStack.Push(DirName);

            while (DirStack.Count > 0)
            {
                var DirInfo = new DirectoryInfo(DirStack.Pop());
                try
                {
                    foreach (var DirChildInfo in DirInfo.GetDirectories())
                    {
                        DirStack.Push(DirChildInfo.FullName);
                    }

                    foreach (var FileChildInfo in DirInfo.GetFiles())
                    {
                        if (FileChildInfo.Name.EndsWith(".exe") && !RuleListBox.Items.Contains(FileChildInfo.Name))
                        {
                            RuleListBox.Items.Add(FileChildInfo.Name);
                            Edited = true;
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public void ModeForm_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);
            i18N.Translate(contextMenuStrip);
        }

        /// <summary>
        /// listBox右键菜单
        /// </summary>
        private void RuleListBox_MouseUp(object sender, MouseEventArgs e)
        {
            RuleListBox.SelectedIndex = RuleListBox.IndexFromPoint(e.X, e.Y);
            if (RuleListBox.SelectedIndex == -1)
                return;
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip.Show(RuleListBox, e.Location);
            }
        }

        void deleteRule_Click(object sender, EventArgs e)
        {
            if (RuleListBox.SelectedIndex == -1) return;
            RuleListBox.Items.RemoveAt(RuleListBox.SelectedIndex);
            Edited = true;
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(ProcessNameTextBox.Text))
                {
                    MessageBoxX.Show(i18N.Translate("Please enter an process name (xxx.exe)"));
                    return;
                }

                if (!NFController.CheckCppRegex(ProcessNameTextBox.Text))
                {
                    MessageBoxX.Show("Rule does not conform to C++ regular expression syntax");
                    return;
                }

                var process = ProcessNameTextBox.Text;

                if (!RuleListBox.Items.Contains(process))
                {
                    RuleListBox.Items.Add(process);
                }

                Edited = true;
                RuleListBox.SelectedIndex = RuleListBox.Items.IndexOf(process);
                ProcessNameTextBox.Text = string.Empty;
            });
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false,
                Title = i18N.Translate("Select a folder"),
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                NavigateToShortcut = true
            };
            if (dialog.ShowDialog(Win32Native.GetForegroundWindow()) == CommonFileDialogResult.Ok)
            {
                ScanDirectory(dialog.FileName);
                MessageBoxX.Show(i18N.Translate("Scan completed"));
            }
        }

        public void ControlButton_Click(object sender, EventArgs e)
        {
            if (RuleListBox.Items.Count == 0)
            {
                MessageBoxX.Show(i18N.Translate("Unable to add empty rule"));
                return;
            }

            if (string.IsNullOrWhiteSpace(RemarkTextBox.Text))
            {
                MessageBoxX.Show(i18N.Translate("Please enter a mode remark"));
                return;
            }

            if (string.IsNullOrWhiteSpace(FilenameTextBox.Text))
            {
                MessageBoxX.Show(i18N.Translate("Please enter a mode filename"));
                return;
            }

            if (_mode != null)
            {
                _mode.Remark = RemarkTextBox.Text;
                _mode.Rule.Clear();
                _mode.Rule.AddRange(RuleListBox.Items.Cast<string>());

                ModeHelper.WriteFile(_mode);
                Global.MainForm.InitMode();
                Edited = false;
                MessageBoxX.Show(i18N.Translate("Mode updated successfully"));
            }
            else
            {
                var fullName = ModeHelper.GetFullPath(FilenameTextBox.Text + ".txt");
                if (File.Exists(fullName))
                {
                    MessageBoxX.Show(i18N.Translate("File already exists.\n Please Change the filename"));
                    return;
                }

                var mode = new Models.Mode
                {
                    BypassChina = false,
                    FileName = FilenameTextBox.Text,
                    Type = 0,
                    Remark = RemarkTextBox.Text
                };
                mode.Rule.AddRange(RuleListBox.Items.Cast<string>());

                ModeHelper.WriteFile(mode);
                ModeHelper.Add(mode);
                MessageBoxX.Show(i18N.Translate("Mode added successfully"));
            }

            Close();
        }

        private async void RemarkTextBox_TextChanged(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                if (!UseCustomFilenameBox.Checked)
                {
                    var invalidFileChars = Path.GetInvalidFileNameChars();
                    var fileName = new StringBuilder(RemarkTextBox.Text);
                    foreach (var c in invalidFileChars)
                    {
                        fileName.Replace(c, '_');
                    }

                    FilenameTextBox.Text = fileName.ToString();
                }
            });
        }

        private void UseCustomFilenameBox_CheckedChanged(object sender, EventArgs e)
        {
            FilenameTextBox.Enabled = UseCustomFilenameBox.Checked;
        }
    }
}
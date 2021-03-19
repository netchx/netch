using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Netch.Controllers;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Forms.Mode
{
    public partial class Process : Form
    {
        /// <summary>
        ///     被编辑的模式
        /// </summary>
        private readonly Models.Mode? _mode;

        /// <summary>
        ///     编辑模式
        /// </summary>
        /// <param name="mode">模式</param>
        public Process(Models.Mode? mode = null)
        {
            if (mode != null && mode.Type is not 0)
                throw new ArgumentOutOfRangeException();

            InitializeComponent();
            Icon = Resources.icon;
            CheckForIllegalCrossThreadCalls = false;

            _mode = mode;
        }

        /// <summary>
        ///     是否被编辑过
        /// </summary>
        public bool Edited { get; private set; }

        public void ModeForm_Load(object sender, EventArgs e)
        {
            if (_mode != null)
            {
                Text = "Edit Process Mode";

                RemarkTextBox.TextChanged -= RemarkTextBox_TextChanged;
                RemarkTextBox.Text = _mode.Remark;
                FilenameTextBox.Text = _mode.RelativePath;
                RuleListBox.Items.AddRange(_mode.Rule.Cast<object>().ToArray());
            }

            i18N.TranslateForm(this);
            i18N.Translate(contextMenuStrip);
        }

        /// <summary>
        ///     listBox右键菜单
        /// </summary>
        private void RuleListBox_MouseUp(object sender, MouseEventArgs e)
        {
            RuleListBox.SelectedIndex = RuleListBox.IndexFromPoint(e.X, e.Y);
            if (RuleListBox.SelectedIndex == -1)
                return;

            if (e.Button == MouseButtons.Right)
                contextMenuStrip.Show(RuleListBox, e.Location);
        }

        private void deleteRule_Click(object sender, EventArgs e)
        {
            if (RuleListBox.SelectedIndex == -1)
                return;

            RuleListBox.Items.RemoveAt(RuleListBox.SelectedIndex);
            Edited = true;
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(ProcessNameTextBox.Text))
                {
                    MessageBoxX.Show(i18N.Translate("rule can not be empty"));
                    return;
                }

                if (!NFController.CheckCppRegex(ProcessNameTextBox.Text))
                {
                    MessageBoxX.Show("Rule does not conform to C++ regular expression syntax");
                    return;
                }

                var process = ProcessNameTextBox.Text;

                if (!RuleListBox.Items.Contains(process))
                    RuleListBox.Items.Add(process);

                Edited = true;
                RuleListBox.SelectedIndex = RuleListBox.Items.IndexOf(process);
                ProcessNameTextBox.Text = string.Empty;
            });
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = true,
                Title = i18N.Translate("Select a folder"),
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                NavigateToShortcut = true
            };

            if (dialog.ShowDialog(Handle) == CommonFileDialogResult.Ok)
            {
                foreach (string p in dialog.FileNames)
                {
                    string path = p;
                    if (!path.EndsWith(@"\"))
                        path += @"\";

                    RuleAdd($"^{path.ToRegexString()}");
                }
            }
        }

        private void RuleAdd(string value)
        {
            RuleListBox.Items.Add(value);
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

                _mode.WriteFile();
                Global.MainForm.LoadModes();
                Edited = false;
                MessageBoxX.Show(i18N.Translate("Mode updated successfully"));
            }
            else
            {
                var relativePath = FilenameTextBox.Text;
                var fullName = ModeHelper.GetFullPath(relativePath);
                if (File.Exists(fullName))
                {
                    MessageBoxX.Show(i18N.Translate("File already exists.\n Please Change the filename"));
                    return;
                }

                var mode = new Models.Mode(fullName)
                {
                    BypassChina = false,
                    Type = 0,
                    Remark = RemarkTextBox.Text
                };

                mode.Rule.AddRange(RuleListBox.Items.Cast<string>());

                mode.WriteFile();
                ModeHelper.Add(mode);
                MessageBoxX.Show(i18N.Translate("Mode added successfully"));
            }

            Close();
        }

        private void RemarkTextBox_TextChanged(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                FilenameTextBox.Text = FilenameTextBox.Text = ModeEditorUtils.GetCustomModeRelativePath(RemarkTextBox.Text);
            }));
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

            if (dialog.ShowDialog(Handle) == CommonFileDialogResult.Ok)
            {
                var path = dialog.FileName;
                var list = new List<string>();
                const uint maxCount = 50;
                try
                {
                    ScanDirectory(path, list);
                }
                catch
                {
                    MessageBoxX.Show(i18N.Translate($"The number of executable files in the \"{path}\" directory is greater than {maxCount}"),
                        LogLevel.WARNING);

                    return;
                }

                RuleListBox.Items.AddRange(list.Cast<object>().ToArray());
            }
        }

        private void ScanDirectory(string directory, List<string> list, uint maxCount = 30)
        {
            foreach (string dir in Directory.GetDirectories(directory))
                ScanDirectory(dir, list, maxCount);

            list.AddRange(Directory.GetFiles(directory).Select(Path.GetFileName).Where(s => s.EndsWith(".exe")).Select(s => s.ToRegexString()));

            if (maxCount != 0 && list.Count > maxCount)
                throw new Exception("The number of filter results is greater than maxCount");
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using Netch.Controllers;
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
            if (mode != null)
            {
                Text = "Edit Process Mode";

                RemarkTextBox.TextChanged -= RemarkTextBox_TextChanged;
                FilenameTextBox.Enabled = UseCustomFilenameBox.Enabled = false;

                RemarkTextBox.Text = mode.Remark;
                FilenameTextBox.Text = mode.RelativePath;
                RuleListBox.Items.AddRange(mode.Rule.ToArray());
            }
        }

        /// <summary>
        ///     是否被编辑过
        /// </summary>
        public bool Edited { get; private set; }

        /// <summary>
        ///     扫描目录
        /// </summary>
        /// <param name="DirName">路径</param>
        public void ScanDirectory(string DirName)
        {
            try
            {
                RuleListBox.Items.AddRange(Directory.GetFiles(DirName, "*.exe", SearchOption.AllDirectories)
                    .Select(f => Path.GetFileName(f))
                    .ToArray());
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void ModeForm_Load(object sender, EventArgs e)
        {
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
                    RuleListBox.Items.Add(process);

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

            if (dialog.ShowDialog(Handle) == CommonFileDialogResult.Ok)
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

                _mode.WriteFile();
                Global.MainForm.LoadModes();
                Edited = false;
                MessageBoxX.Show(i18N.Translate("Mode updated successfully"));
            }
            else
            {
                var relativePath = $"Custom\\{FilenameTextBox.Text}.txt";
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

        private async void RemarkTextBox_TextChanged(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                if (!UseCustomFilenameBox.Checked)
                {
                    FilenameTextBox.Text = ModeEditorUtils.ToSafeFileName(RemarkTextBox.Text);
                }
            });
        }
    }
}
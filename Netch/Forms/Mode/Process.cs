using Microsoft.WindowsAPICodePack.Dialogs;
using Netch.Controllers;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

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

        #region Model

        public IEnumerable<string> Rules => RuleRichTextBox.Lines;

        private void RuleAdd(string value)
        {
            RuleRichTextBox.AppendText($"{value}\n");
        }

        private void RuleAddRange(IEnumerable<string> value)
        {
            foreach (string s in value)
            {
                RuleAdd(s);
            }
        }

        #endregion

        public void ModeForm_Load(object sender, EventArgs e)
        {
            if (_mode != null)
            {
                Text = "Edit Process Mode";

                RemarkTextBox.TextChanged -= RemarkTextBox_TextChanged;
                RemarkTextBox.Text = _mode.Remark;
                FilenameTextBox.Text = _mode.RelativePath;
                RuleAddRange(_mode.Rule);
            }

            i18N.TranslateForm(this);
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

        public void ControlButton_Click(object sender, EventArgs e)
        {
            if (!RuleRichTextBox.Lines.Any())
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
                _mode.Rule.AddRange(RuleRichTextBox.Lines);

                _mode.WriteFile();
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
                    Type = 0,
                    Remark = RemarkTextBox.Text
                };

                mode.Rule.AddRange(RuleRichTextBox.Lines);

                mode.WriteFile();
                MessageBoxX.Show(i18N.Translate("Mode added successfully"));
            }

            Close();
        }

        private void RemarkTextBox_TextChanged(object? sender, EventArgs? e)
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

                RuleAddRange(list);
            }
        }

        private void ScanDirectory(string directory, List<string> list, uint maxCount = 30)
        {
            foreach (string dir in Directory.GetDirectories(directory))
                ScanDirectory(dir, list, maxCount);

            list.AddRange(
                Directory.GetFiles(directory).Select(s => Path.GetFileName(s)).Where(s => s.EndsWith(".exe")).Select(s => s.ToRegexString()));

            if (maxCount != 0 && list.Count > maxCount)
                throw new Exception("The number of results is greater than maxCount");
        }

        private void ValidationButton_Click(object sender, EventArgs e)
        {
            if (!NFController.CheckRules(Rules, out var results))
                MessageBoxX.Show(NFController.GenerateInvalidRulesMessage(results), LogLevel.WARNING);
            else
                MessageBoxX.Show("Fine");
        }
    }
}
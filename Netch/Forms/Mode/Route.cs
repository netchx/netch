using Netch.Models;
using Netch.Properties;
using Netch.Utils;
using System;
using System.IO;
using System.Windows.Forms;

namespace Netch.Forms.Mode
{
    public partial class Route : Form
    {
        private readonly TagItem<int>[] _items = { new(1, "Proxy Rule IPs"), new(2, "Bypass Rule IPs") };

        private readonly Models.Mode? _mode;

        public Route(Models.Mode? mode = null)
        {
            if (mode != null && mode.Type is not (1 or 2))
                throw new ArgumentOutOfRangeException();

            _mode = mode;

            InitializeComponent();
            Icon = Resources.icon;
            comboBox1.DataSource = _items;
            comboBox1.ValueMember = nameof(TagItem<int>.Value);
            comboBox1.DisplayMember = nameof(TagItem<int>.Text);
        }

        private void Route_Load(object sender, EventArgs e)
        {
            if (_mode != null)
            {
                Text = "Edit Route Table Rule";

                RemarkTextBox.TextChanged -= RemarkTextBox_TextChanged;
                RemarkTextBox.Text = _mode.Remark;
                comboBox1.SelectedValue = _mode.Type; // ComboBox SelectedValue worked after ctor
                FilenameTextBox.Text = _mode.RelativePath;
                richTextBox1.Lines = _mode.Rule.ToArray();
            }

            i18N.TranslateForm(this);
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
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
                _mode.Rule.AddRange(richTextBox1.Lines);
                _mode.Type = (int)comboBox1.SelectedValue;

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
                    Type = (int)comboBox1.SelectedValue,
                    Remark = RemarkTextBox.Text
                };

                mode.Rule.AddRange(richTextBox1.Lines);

                mode.WriteFile();
                MessageBoxX.Show(i18N.Translate("Mode added successfully"));
            }

            Close();
        }

        private void RemarkTextBox_TextChanged(object? sender, EventArgs? e)
        {
            BeginInvoke(new Action(() => { FilenameTextBox.Text = ModeEditorUtils.GetCustomModeRelativePath(RemarkTextBox.Text); }));
        }
    }
}
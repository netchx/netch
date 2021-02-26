using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Forms.Mode
{
    public partial class Route : Form
    {
        class Item
        {
            private string _text;

            public Item(int value, string text)
            {
                _text = text;
                Value = value;
            }

            public string Text
            {
                get => i18N.Translate(_text);
                set => _text = value;
            }

            public int Value { get; set; }
        }

        private readonly Item[] _items = {new(1, "Proxy Rule IPs"), new(2, "Bypass Rule IPs")};

        private readonly Models.Mode? _mode;

        public Route(Models.Mode? mode = null)
        {
            if (mode != null && mode.Type is not (1 or 2))
                throw new ArgumentOutOfRangeException();

            _mode = mode;

            InitializeComponent();
            Icon = Resources.icon;
            comboBox1.DataSource = _items;
            comboBox1.ValueMember = "Value";
            comboBox1.DisplayMember = "Text";

            i18N.TranslateForm(this);
        }

        private void Route_Load(object sender, EventArgs e)
        {
            if (_mode != null)
            {
                Text = "Edit Route Table Rule";

                RemarkTextBox.TextChanged -= RemarkTextBox_TextChanged;
                FilenameTextBox.Enabled = UseCustomFilenameBox.Enabled = false;

                RemarkTextBox.Text = _mode.Remark;
                comboBox1.SelectedValue = _mode.Type; // ComboBox SelectedValue worked after ctor
                FilenameTextBox.Text = _mode.RelativePath;
                richTextBox1.Lines = _mode.Rule.ToArray();
            }
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
                _mode.Type = (int) comboBox1.SelectedValue;

                _mode.WriteFile();
                Global.MainForm.LoadModes();
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
                    Type = (int) comboBox1.SelectedValue,
                    Remark = RemarkTextBox.Text
                };

                mode.Rule.AddRange(richTextBox1.Lines);

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
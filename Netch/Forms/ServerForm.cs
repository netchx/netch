using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    public abstract partial class ServerForm : Form
    {
        protected abstract string TypeName { get; }
        protected Server Server { get; set; }

        private int _controlLines = 2;

        private const int ControlLineHeight = 28;
        private const int InputBoxWidth = 294;

        protected ServerForm()
        {
            InitializeComponent();

            _checkActions.Add(RemarkTextBox, s => true);
            _saveActions.Add(RemarkTextBox, s => Server.Remark = (string) s);

            _checkActions.Add(AddressTextBox, s => s != string.Empty);
            _saveActions.Add(AddressTextBox, s => Server.Hostname = (string) s);

            _checkActions.Add(PortTextBox, s => ushort.TryParse(s, out var port) && port != 0);
            _saveActions.Add(PortTextBox, s => Server.Port = ushort.Parse((string) s));
        }

        protected void CreateTextBox(string name, string remark, Func<string, bool> check, Action<object> save, string value, int width = InputBoxWidth)
        {
            _controlLines++;

            var textBox = new TextBox
            {
                Location = new Point(120, ControlLineHeight * _controlLines),
                Name = $"{name}TextBox",
                Size = new Size(width, 23),
                TextAlign = HorizontalAlignment.Center,
                Text = value
            };
            _checkActions.Add(textBox, check);
            _saveActions.Add(textBox, save);
            ConfigurationGroupBox.Controls.AddRange(
                new Control[]
                {
                    textBox,
                    new Label
                    {
                        AutoSize = true,
                        Location = new Point(10, ControlLineHeight * _controlLines),
                        Name = $"{name}Label",
                        Size = new Size(56, 17),
                        Text = remark
                    }
                }
            );
        }

        protected void CreateComboBox(string name, string remark, List<string> values, Func<string, bool> parse, Action<object> save, string value, int width = InputBoxWidth)
        {
            _controlLines++;

            var comboBox = new ComboBox
            {
                Location = new Point(120, ControlLineHeight * _controlLines),
                Name = $"{name}ComboBox",
                Size = new Size(width, 23),
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true
            };
            comboBox.Items.AddRange(values.ToArray());
            comboBox.SelectedIndex = values.IndexOf(value);
            comboBox.DrawItem += Utils.Utils.DrawCenterComboBox;
            _checkActions.Add(comboBox, parse);
            _saveActions.Add(comboBox, save);
            ConfigurationGroupBox.Controls.AddRange(
                new Control[]
                {
                    comboBox,
                    new Label
                    {
                        AutoSize = true,
                        Location = new Point(10, ControlLineHeight * _controlLines),
                        Name = $"{name}Label",
                        Size = new Size(56, 17),
                        Text = remark
                    }
                }
            );
        }

        protected void CreateCheckBox(string name, string remark, Action<object> save, bool value)
        {
            _controlLines++;

            var checkBox = new CheckBox
            {
                AutoSize = true,
                Location = new Point(120, ControlLineHeight * _controlLines),
                Name = $"{name}CheckBox",
                Checked = value,
                Text = remark
            };
            _saveActions.Add(checkBox, save);
            ConfigurationGroupBox.Controls.AddRange(
                new Control[]
                {
                    checkBox
                }
            );
        }

        private readonly Dictionary<Control, Func<string, bool>> _checkActions = new Dictionary<Control, Func<string, bool>>();

        private readonly Dictionary<Control, Action<object>> _saveActions = new Dictionary<Control, Action<object>>();

        private void ServerForm_Load(object sender, EventArgs e)
        {
            this.Text = TypeName ?? string.Empty;

            RemarkTextBox.Text = Server.Remark;
            AddressTextBox.Text = Server.Hostname;
            PortTextBox.Text = Server.Port.ToString();

            AddSaveButton();
            i18N.TranslateForm(this);
        }

        private void AddSaveButton()
        {
            _controlLines++;
            var control = new Button
            {
                Location = new Point(340, _controlLines * ControlLineHeight + 10),
                Name = "ControlButton",
                Size = new Size(75, 23),
                Text = "Save",
                UseVisualStyleBackColor = true
            };
            control.Click += ControlButton_Click;
            ConfigurationGroupBox.Controls.Add(control);
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (_checkActions.All(pair => pair.Value.Invoke(pair.Key.Text)))
            {
                foreach (var pair in _saveActions)
                {
                    switch (pair.Key)
                    {
                        case CheckBox c:
                            pair.Value.Invoke(c.Checked);
                            break;
                        default:
                            pair.Value.Invoke(pair.Key.Text);
                            break;
                    }
                }

                if (Global.Settings.Server.IndexOf(Server) == -1)
                    Global.Settings.Server.Add(Server);

                MessageBoxX.Show(i18N.Translate("Saved"));
            }
            else
                return;

            Close();
        }
    }
}
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Forms
{
    public abstract class ServerForm : Form
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

        public new void ShowDialog()
        {
            AfterFactor();
            base.ShowDialog();
        }

        public new void Show()
        {
            AfterFactor();
            base.Show();
        }

        private void AfterFactor()
        {
            Text = TypeName ?? string.Empty;

            RemarkTextBox.Text = Server.Remark;
            AddressTextBox.Text = Server.Hostname;
            PortTextBox.Text = Server.Port.ToString();

            AddSaveButton();
            i18N.TranslateForm(this);

            ConfigurationGroupBox.ResumeLayout(false);
            ConfigurationGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        protected void CreateTextBox(string name, string remark, Func<string, bool> check, Action<string> save, string value, int width = InputBoxWidth)
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
            _saveActions.Add(textBox, o => save.Invoke((string) o));
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

        protected void CreateComboBox(string name, string remark, List<string> values, Action<string> save, string value, int width = InputBoxWidth)
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
            _saveActions.Add(comboBox, o => save.Invoke((string) o));
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

        protected void CreateCheckBox(string name, string remark, Action<bool> save, bool value)
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
            _saveActions.Add(checkBox, o => save.Invoke((bool) o));
            ConfigurationGroupBox.Controls.AddRange(
                new Control[]
                {
                    checkBox
                }
            );
        }

        private readonly Dictionary<Control, Func<string, bool>> _checkActions = new Dictionary<Control, Func<string, bool>>();

        private readonly Dictionary<Control, Action<object>> _saveActions = new Dictionary<Control, Action<object>>();

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
            Utils.Utils.ComponentIterator(this, component => Utils.Utils.ChangeControlForeColor(component, Color.Black));

            var flag = true;
            foreach (var pair in _checkActions.Where(pair => !pair.Value.Invoke(pair.Key.Text)))
            {
                Utils.Utils.ChangeControlForeColor(pair.Key, Color.Red);
                flag = false;
            }

            if (!flag)
            {
                return;
            }

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

            Close();
        }

        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ConfigurationGroupBox = new GroupBox();
            AddressLabel = new Label();
            PortTextBox = new TextBox();
            AddressTextBox = new TextBox();
            RemarkTextBox = new TextBox();
            RemarkLabel = new Label();
            PortLabel = new Label();
            ConfigurationGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // ConfigurationGroupBox
            // 
            ConfigurationGroupBox.AutoSize = true;
            ConfigurationGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ConfigurationGroupBox.Controls.Add(AddressLabel);
            ConfigurationGroupBox.Controls.Add(PortTextBox);
            ConfigurationGroupBox.Controls.Add(AddressTextBox);
            ConfigurationGroupBox.Controls.Add(RemarkTextBox);
            ConfigurationGroupBox.Controls.Add(RemarkLabel);
            ConfigurationGroupBox.Controls.Add(PortLabel);
            ConfigurationGroupBox.Dock = DockStyle.Fill;
            ConfigurationGroupBox.Location = new Point(5, 5);
            ConfigurationGroupBox.Name = "ConfigurationGroupBox";
            ConfigurationGroupBox.Size = new Size(434, 127);
            ConfigurationGroupBox.TabIndex = 0;
            ConfigurationGroupBox.TabStop = false;
            ConfigurationGroupBox.Text = "Configuration";
            // 
            // AddressLabel
            // 
            AddressLabel.AutoSize = true;
            AddressLabel.Location = new Point(10, ControlLineHeight * 2);
            AddressLabel.Name = "AddressLabel";
            AddressLabel.Size = new Size(56, 17);
            AddressLabel.TabIndex = 2;
            AddressLabel.Text = "Address";
            // 
            // PortTextBox
            // 
            PortTextBox.Location = new Point(358, ControlLineHeight * 2);
            PortTextBox.Name = "PortTextBox";
            PortTextBox.Size = new Size(56, 23);
            PortTextBox.TabIndex = 5;
            PortTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // AddressTextBox
            // 
            AddressTextBox.Location = new Point(120, ControlLineHeight * 2);
            AddressTextBox.Name = "AddressTextBox";
            AddressTextBox.Size = new Size(232, 23);
            AddressTextBox.TabIndex = 3;
            AddressTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // RemarkTextBox
            // 
            RemarkTextBox.Location = new Point(120, ControlLineHeight);
            RemarkTextBox.Name = "RemarkTextBox";
            RemarkTextBox.Size = new Size(294, 23);
            RemarkTextBox.TabIndex = 1;
            RemarkTextBox.TextAlign = HorizontalAlignment.Center;
            // 
            // RemarkLabel
            // 
            RemarkLabel.AutoSize = true;
            RemarkLabel.Location = new Point(10, ControlLineHeight);
            RemarkLabel.Name = "RemarkLabel";
            RemarkLabel.Size = new Size(53, 17);
            RemarkLabel.TabIndex = 0;
            RemarkLabel.Text = "Remark";
            // 
            // PortLabel
            // 
            PortLabel.AutoSize = true;
            PortLabel.Location = new Point(351, ControlLineHeight * 2);
            PortLabel.Name = "PortLabel";
            PortLabel.Size = new Size(11, 17);
            PortLabel.TabIndex = 4;
            PortLabel.Text = ":";
            // 
            // ServerForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(444, 137);
            Controls.Add(ConfigurationGroupBox);
            Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, (byte) 134);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = Icon.FromHandle(Resources.Netch.GetHicon());
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "ServerForm";
            Padding = new Padding(11, 5, 11, 4);
            StartPosition = FormStartPosition.CenterScreen;
        }

        private GroupBox ConfigurationGroupBox;
        private Label RemarkLabel;
        protected TextBox RemarkTextBox;
        private Label PortLabel;
        protected TextBox AddressTextBox;
        private TextBox PortTextBox;
        private Label AddressLabel;
    }
}
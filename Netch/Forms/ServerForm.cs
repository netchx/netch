using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Netch.Models;
using Netch.Servers.Shadowsocks;
using Netch.Utils;

namespace Netch.Forms
{
    public abstract partial class ServerForm : Form
    {
        public Shadowsocks Server;

        protected ServerForm(Server server = default)
        {
            InitializeComponent();
            CreateTextBox(name: "EncryptMethod", remark: "Encrypt Method", save: str => { Server.EncryptMethod = str; }, parse: str => true);
        }

        private void CreateTextBox(string name, string remark, Action<string> save, Func<string, bool> parse = default)
        {
            var textBox = new TextBox
            {
                Location = new System.Drawing.Point(358, 48),
                Name = $"{name}TextBox",
                Size = new System.Drawing.Size(56, 23),
                TextAlign = HorizontalAlignment.Center,
            };
            ParseActions.Add(textBox, parse);
            SaveActions.Add(textBox,save);
            ConfigurationGroupBox.Controls.AddRange(
                new Control[]
                {
                    textBox,
                    new Label
                    {
                        AutoSize = true,
                        Location = AddressLabel.Location = new System.Drawing.Point(10, 51),
                        Name = $"{name}Label",
                        Size = AddressLabel.Size = new System.Drawing.Size(56, 17),
                        Text = remark,
                    }
                }
            );
        }

        private Dictionary<Control, Action<string>> SaveActions = new Dictionary<Control, Action<string>>();
        private Dictionary<Control, Func<string, bool>> ParseActions = new Dictionary<Control, Func<string, bool>>();

        private void Socks5_Load(object sender, EventArgs e)
        {
            i18N.TranslateForm(this);
        }

        private void ControlButton_Click(object sender, EventArgs e)
        {
            if (!ushort.TryParse(PortTextBox.Text, out var port)) return;

            if (Save())
                MessageBoxX.Show(i18N.Translate("Saved"));
            else
                return;

            Close();
        }

        protected abstract bool Save();
    }
}
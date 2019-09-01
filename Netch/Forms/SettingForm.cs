using System;
using System.Net;
using System.Windows.Forms;

namespace Netch.Forms
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate("Settings");
            PortGroupBox.Text = Utils.i18N.Translate("Port");
            TUNTAPAddressLabel.Text = Utils.i18N.Translate("Address");
            TUNTAPNetmaskLabel.Text = Utils.i18N.Translate("Netmask");
            TUNTAPGatewayLabel.Text = Utils.i18N.Translate("Gateway");
            TUNTAPUseCustomDNSCheckBox.Text = Utils.i18N.Translate("Use Custom DNS");
            GlobalBypassIPsButton.Text = Utils.i18N.Translate("Global Bypass IPs");
            ControlButton.Text = Utils.i18N.Translate("Save");

            Socks5PortTextBox.Text = Global.Settings["Socks5Port"].ToString();
            HTTPPortTextBox.Text = Global.Settings["HTTPPort"].ToString();

            TUNTAPAddressTextBox.Text = Global.TUNTAP.Address.ToString();
            TUNTAPNetmaskTextBox.Text = Global.TUNTAP.Netmask.ToString();
            TUNTAPGatewayTextBox.Text = Global.TUNTAP.Gateway.ToString();

            var dns = "";
            foreach (var ip in Global.TUNTAP.DNS)
            {
                dns += ip.ToString();
                dns += ',';
            }
            dns = dns.Trim();
            TUNTAPDNSTextBox.Text = dns.Substring(0, dns.Length - 1);
            TUNTAPUseCustomDNSCheckBox.Checked = Global.TUNTAP.UseCustomDNS;
        }

        private void SettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Global.MainForm.Show();
        }

        private void GlobalBypassIPsButton_Click(object sender, EventArgs e)
        {
            new GlobalBypassIPForm().Show();
            Hide();
        }
        
        private void ControlButton_Click(object sender, EventArgs e)
        {
            Global.TUNTAP.Address = IPAddress.Parse(TUNTAPAddressTextBox.Text);
            Global.TUNTAP.Netmask = IPAddress.Parse(TUNTAPNetmaskTextBox.Text);
            Global.TUNTAP.Gateway = IPAddress.Parse(TUNTAPGatewayTextBox.Text);

            try
            {
                var Socks5Port = Int32.Parse(Socks5PortTextBox.Text);
                var HTTPPort = Int32.Parse(HTTPPortTextBox.Text);

                if (Socks5Port > 0 && Socks5Port < 65536)
                {
                    Global.Settings["Socks5Port"] = Socks5Port;
                }
                else
                {
                    Socks5PortTextBox.Text = Global.Settings["Socks5Port"].ToString();
                    throw new FormatException();
                }

                if (HTTPPort > 0 && HTTPPort < 65536)
                {
                    Global.Settings["HTTPPort"] = HTTPPort;
                }
                else
                {
                    HTTPPortTextBox.Text = Global.Settings["HTTPPort"].ToString();
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                MessageBox.Show(Utils.i18N.Translate("Port value illegal. Try again."), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            Global.TUNTAP.DNS.Clear();
            foreach (var ip in TUNTAPDNSTextBox.Text.Split(','))
            {
                Global.TUNTAP.DNS.Add(IPAddress.Parse(ip));
            }

            Global.TUNTAP.UseCustomDNS = TUNTAPUseCustomDNSCheckBox.Checked;

            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }        
    }
}

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

        private void TUNTAPUseCustomDNSCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!TUNTAPUseCustomDNSCheckBox.Checked)
            {
                TUNTAPDNSTextBox.Enabled = false;
            }
            else
            {
                TUNTAPDNSTextBox.Enabled = true;
            }

            if (Global.Settings.TUNTAP.DNS.Count > 0)
            {
                var dns = "";
                foreach (var ip in Global.Settings.TUNTAP.DNS)
                {
                    dns += ip;
                    dns += ',';
                }
                dns = dns.Trim();
                TUNTAPDNSTextBox.Text = dns.Substring(0, dns.Length - 1);
            }
            // 如果 DNS 为空，设置为默认 DNS 1.1.1.1
            else
            {
                Global.Settings.TUNTAP.DNS.Add("1.1.1.1");
                TUNTAPDNSTextBox.Text = "1.1.1.1";
            }
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            Text = Utils.i18N.Translate(Text);
            PortGroupBox.Text = Utils.i18N.Translate(PortGroupBox.Text);
            AllowDevicesCheckBox.Text = Utils.i18N.Translate(AllowDevicesCheckBox.Text);
            TUNTAPAddressLabel.Text = Utils.i18N.Translate(TUNTAPAddressLabel.Text);
            TUNTAPNetmaskLabel.Text = Utils.i18N.Translate(TUNTAPNetmaskLabel.Text);
            TUNTAPGatewayLabel.Text = Utils.i18N.Translate(TUNTAPGatewayLabel.Text);
            TUNTAPUseCustomDNSCheckBox.Text = Utils.i18N.Translate(TUNTAPUseCustomDNSCheckBox.Text);
            GlobalBypassIPsButton.Text = Utils.i18N.Translate(GlobalBypassIPsButton.Text);
            ControlButton.Text = Utils.i18N.Translate(ControlButton.Text);

            ExitWhenClosedCheckBox.Checked = Global.Settings.ExitWhenClosed;
            StopWhenExitedCheckBox.Checked = Global.Settings.StopWhenExited;
            StartWhenOpenedCheckBox.Checked = Global.Settings.StartWhenOpened;
            CheckUpdateWhenOpenedCheckBox.Checked = Global.Settings.CheckUpdateWhenOpened;

            Socks5PortTextBox.Text = Global.Settings.Socks5LocalPort.ToString();
            HTTPPortTextBox.Text = Global.Settings.HTTPLocalPort.ToString();
            RedirectorTextBox.Text = Global.Settings.RedirectorTCPPort.ToString();

            TUNTAPAddressTextBox.Text = Global.Settings.TUNTAP.Address;
            TUNTAPNetmaskTextBox.Text = Global.Settings.TUNTAP.Netmask;
            TUNTAPGatewayTextBox.Text = Global.Settings.TUNTAP.Gateway;

            TUNTAPUseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;

            BehaviorGroupBox.Text = Utils.i18N.Translate(BehaviorGroupBox.Text);
            ExitWhenClosedCheckBox.Text = Utils.i18N.Translate(ExitWhenClosedCheckBox.Text);
            StopWhenExitedCheckBox.Text = Utils.i18N.Translate(StopWhenExitedCheckBox.Text);
            StartWhenOpenedCheckBox.Text = Utils.i18N.Translate(StartWhenOpenedCheckBox.Text);
            CheckUpdateWhenOpenedCheckBox.Text = Utils.i18N.Translate(CheckUpdateWhenOpenedCheckBox.Text);
            ProfileCount_Label.Text = Utils.i18N.Translate(ProfileCount_Label.Text);

            ProfileCount_TextBox.Text = Global.Settings.ProfileCount.ToString();

            if (Global.Settings.TUNTAP.DNS.Count > 0)
            {
                var dns = "";
                foreach (var ip in Global.Settings.TUNTAP.DNS)
                {
                    dns += ip;
                    dns += ',';
                }
                dns = dns.Trim();
                TUNTAPDNSTextBox.Text = dns.Substring(0, dns.Length - 1);
            }
            // 如果 DNS 为空，设置为默认 DNS 1.1.1.1
            else
            {
                Global.Settings.TUNTAP.DNS.Add("1.1.1.1");
                TUNTAPDNSTextBox.Text = "1.1.1.1";
            }

            if (!TUNTAPUseCustomDNSCheckBox.Checked)
            {
                TUNTAPDNSTextBox.Enabled = false;
            }

            // 设置本地代理是否允许其他设备连接
            if (Global.Settings.LocalAddress == "127.0.0.1")
            {
                AllowDevicesCheckBox.Checked = false;
            }
            else if (Global.Settings.LocalAddress == "0.0.0.0")
            {
                AllowDevicesCheckBox.Checked = true;
            }
            else
            {
                Global.Settings.LocalAddress = "127.0.0.1";
                AllowDevicesCheckBox.Checked = false;
            }
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
            Global.Settings.ExitWhenClosed = ExitWhenClosedCheckBox.Checked;
            Global.Settings.StopWhenExited = StopWhenExitedCheckBox.Checked;
            Global.Settings.StartWhenOpened = StartWhenOpenedCheckBox.Checked;
            Global.Settings.CheckUpdateWhenOpened = CheckUpdateWhenOpenedCheckBox.Checked;

            try
            {
                var Socks5Port = int.Parse(Socks5PortTextBox.Text);

                if (Socks5Port > 0 && Socks5Port < 65536)
                {
                    Global.Settings.Socks5LocalPort = Socks5Port;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                Socks5PortTextBox.Text = Global.Settings.Socks5LocalPort.ToString();
                MessageBox.Show(Utils.i18N.Translate("Port value illegal. Try again."), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            try
            {
                var HTTPPort = int.Parse(HTTPPortTextBox.Text);

                if (HTTPPort > 0 && HTTPPort < 65536)
                {
                    Global.Settings.HTTPLocalPort = HTTPPort;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                HTTPPortTextBox.Text = Global.Settings.HTTPLocalPort.ToString();
                MessageBox.Show(Utils.i18N.Translate("Port value illegal. Try again."), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            try
            {
                var RedirectorPort = int.Parse(RedirectorTextBox.Text);

                if (RedirectorPort > 0 && RedirectorPort < 65536)
                {
                    Global.Settings.RedirectorTCPPort = RedirectorPort;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                RedirectorTextBox.Text = Global.Settings.RedirectorTCPPort.ToString();
                MessageBox.Show(Utils.i18N.Translate("Port value illegal. Try again."), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            if (AllowDevicesCheckBox.Checked)
            {
                Global.Settings.LocalAddress = "0.0.0.0";
            }
            else
            {
                Global.Settings.LocalAddress = "127.0.0.1";
            }

            try
            {
                var Address = IPAddress.Parse(TUNTAPAddressTextBox.Text);
                var Netmask = IPAddress.Parse(TUNTAPNetmaskTextBox.Text);
                var Gateway = IPAddress.Parse(TUNTAPGatewayTextBox.Text);

                var DNS = new System.Collections.Generic.List<IPAddress>();
                foreach (var ip in TUNTAPDNSTextBox.Text.Split(','))
                {
                    DNS.Add(IPAddress.Parse(ip));
                }
            }
            catch (FormatException)
            {
                MessageBox.Show(Utils.i18N.Translate("IP address format illegal. Try again."), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                TUNTAPAddressTextBox.Text = Global.Settings.TUNTAP.Address;
                TUNTAPNetmaskTextBox.Text = Global.Settings.TUNTAP.Netmask;
                TUNTAPGatewayTextBox.Text = Global.Settings.TUNTAP.Gateway;

                var DNS = "";
                foreach (var ip in Global.Settings.TUNTAP.DNS)
                {
                    DNS += ip;
                    DNS += ',';
                }
                DNS = DNS.Trim();
                TUNTAPDNSTextBox.Text = DNS.Substring(0, DNS.Length - 1);
                TUNTAPUseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;

                return;
            }
            try
            {
                var ProfileCount = int.Parse(ProfileCount_TextBox.Text);

                if (ProfileCount>0)
                {
                    Global.Settings.ProfileCount = ProfileCount;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                ProfileCount_TextBox.Text = Global.Settings.ProfileCount.ToString();
                MessageBox.Show(Utils.i18N.Translate("ProfileCount value illegal. Try again."), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;
            }

            Global.Settings.TUNTAP.Address = TUNTAPAddressTextBox.Text;
            Global.Settings.TUNTAP.Netmask = TUNTAPNetmaskTextBox.Text;
            Global.Settings.TUNTAP.Gateway = TUNTAPGatewayTextBox.Text;

            Global.Settings.TUNTAP.DNS.Clear();
            foreach (var ip in TUNTAPDNSTextBox.Text.Split(','))
            {
                Global.Settings.TUNTAP.DNS.Add(ip);
            }

            Global.Settings.TUNTAP.UseCustomDNS = TUNTAPUseCustomDNSCheckBox.Checked;

            Utils.Configuration.Save();
            MessageBox.Show(Utils.i18N.Translate("Saved"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
    }
}

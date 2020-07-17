using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using Netch.Utils;
using TaskScheduler;

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
            if (!UseCustomDNSCheckBox.Checked)
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

        private void InitText()
        {
            Text = i18N.Translate(Text);
            PortGroupBox.Text = i18N.Translate(PortGroupBox.Text);
            AllowDevicesCheckBox.Text = i18N.Translate(AllowDevicesCheckBox.Text);
            TUNTAPAddressLabel.Text = i18N.Translate(TUNTAPAddressLabel.Text);
            TUNTAPNetmaskLabel.Text = i18N.Translate(TUNTAPNetmaskLabel.Text);
            TUNTAPGatewayLabel.Text = i18N.Translate(TUNTAPGatewayLabel.Text);
            UseCustomDNSCheckBox.Text = i18N.Translate(UseCustomDNSCheckBox.Text);
            ProxyDNSCheckBox.Text = i18N.Translate(ProxyDNSCheckBox.Text);
            UseFakeDNSCheckBox.Text = i18N.Translate(UseFakeDNSCheckBox.Text);
            GlobalBypassIPsButton.Text = i18N.Translate(GlobalBypassIPsButton.Text);
            ControlButton.Text = i18N.Translate(ControlButton.Text);
            BootShadowsocksFromDLLCheckBox.Text = i18N.Translate(BootShadowsocksFromDLLCheckBox.Text);

            ExitWhenClosedCheckBox.Checked = Global.Settings.ExitWhenClosed;
            StopWhenExitedCheckBox.Checked = Global.Settings.StopWhenExited;
            StartWhenOpenedCheckBox.Checked = Global.Settings.StartWhenOpened;
            CheckUpdateWhenOpenedCheckBox.Checked = Global.Settings.CheckUpdateWhenOpened;
            MinimizeWhenStartedCheckBox.Checked = Global.Settings.MinimizeWhenStarted;
            RunAtStartup.Checked = Global.Settings.RunAtStartup;
            EnableStartedTcping_CheckBox.Checked = Global.Settings.StartedTcping;
            DetectionInterval_TextBox.Text = Global.Settings.StartedTcping_Interval.ToString();
            BootShadowsocksFromDLLCheckBox.Checked = Global.Settings.BootShadowsocksFromDLL;

            Socks5PortTextBox.Text = Global.Settings.Socks5LocalPort.ToString();
            HTTPPortTextBox.Text = Global.Settings.HTTPLocalPort.ToString();
            RedirectorTextBox.Text = Global.Settings.RedirectorTCPPort.ToString();

            TUNTAPAddressTextBox.Text = Global.Settings.TUNTAP.Address;
            TUNTAPNetmaskTextBox.Text = Global.Settings.TUNTAP.Netmask;
            TUNTAPGatewayTextBox.Text = Global.Settings.TUNTAP.Gateway;

            UseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;
            ProxyDNSCheckBox.Checked = Global.Settings.TUNTAP.ProxyDNS;
            UseFakeDNSCheckBox.Checked = Global.Settings.TUNTAP.UseFakeDNS;

            BehaviorGroupBox.Text = i18N.Translate(BehaviorGroupBox.Text);
            ExitWhenClosedCheckBox.Text = i18N.Translate(ExitWhenClosedCheckBox.Text);
            StopWhenExitedCheckBox.Text = i18N.Translate(StopWhenExitedCheckBox.Text);
            StartWhenOpenedCheckBox.Text = i18N.Translate(StartWhenOpenedCheckBox.Text);
            MinimizeWhenStartedCheckBox.Text = i18N.Translate(MinimizeWhenStartedCheckBox.Text);
            RunAtStartup.Text = i18N.Translate(RunAtStartup.Text);
            CheckUpdateWhenOpenedCheckBox.Text = i18N.Translate(CheckUpdateWhenOpenedCheckBox.Text);
            ProfileCount_Label.Text = i18N.Translate(ProfileCount_Label.Text);
            DelayTestAfterStartup_Label.Text = i18N.Translate(DelayTestAfterStartup_Label.Text);
            EnableStartedTcping_CheckBox.Text = i18N.Translate(EnableStartedTcping_CheckBox.Text);
            DetectionInterval_Label.Text = i18N.Translate(DetectionInterval_Label.Text);
            DelayTestAfterStartup_Label.Text = i18N.Translate(DelayTestAfterStartup_Label.Text);
            STUNServerLabel.Text = i18N.Translate(STUNServerLabel.Text);
            STUNServerPortLabel.Text = i18N.Translate(STUNServerPortLabel.Text);

            ProfileCount_TextBox.Text = Global.Settings.ProfileCount.ToString();
            STUN_ServerTextBox.Text = Global.Settings.STUN_Server;
            STUN_ServerPortTextBox.Text = Global.Settings.STUN_Server_Port.ToString();

            AclLabel.Text = i18N.Translate(AclLabel.Text);
            AclAddr.Text = Global.Settings.ACL;

            LanguageLabel.Text = i18N.Translate(LanguageLabel.Text);
            LanguageComboBox.Items.AddRange(i18N.GetTranslateList().ToArray());
            LanguageComboBox.SelectedItem = Global.Settings.Language;
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            InitText();

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

            if (!UseCustomDNSCheckBox.Checked)
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
            Global.Settings.MinimizeWhenStarted = MinimizeWhenStartedCheckBox.Checked;
            Global.Settings.RunAtStartup = RunAtStartup.Checked;
            Global.Settings.BootShadowsocksFromDLL = BootShadowsocksFromDLLCheckBox.Checked;
            Global.Settings.Language = LanguageComboBox.SelectedItem.ToString();

            // 开机自启判断
            var scheduler = new TaskSchedulerClass();
            scheduler.Connect();
            var folder = scheduler.GetFolder("\\");
            var taskIsExists = false;
            try
            {
                folder.GetTask("Netch Startup");
                taskIsExists = true;
            }
            catch (Exception) { }

            if (RunAtStartup.Checked)
            {
                if (taskIsExists)
                    folder.DeleteTask("Netch Startup", 0);

                var task = scheduler.NewTask(0);
                task.RegistrationInfo.Author = "Netch";
                task.RegistrationInfo.Description = "Netch run at startup.";
                task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;

                task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON);
                var action = (IExecAction)task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
                action.Path = Application.ExecutablePath;


                task.Settings.ExecutionTimeLimit = "PT0S";
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.RunOnlyIfIdle = false;

                folder.RegisterTaskDefinition("Netch Startup", task, (int)_TASK_CREATION.TASK_CREATE, null, null, _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN, "");
            }
            else
            {
                if (taskIsExists)
                    folder.DeleteTask("Netch Startup", 0);
            }

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
                MessageBoxX.Show(i18N.Translate("Port value illegal. Try again."));

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
                MessageBoxX.Show(i18N.Translate("Port value illegal. Try again."));

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
                MessageBoxX.Show(i18N.Translate("Port value illegal. Try again."));

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

                var DNS = new List<IPAddress>();
                foreach (var ip in TUNTAPDNSTextBox.Text.Split(','))
                {
                    DNS.Add(IPAddress.Parse(ip));
                }
            }
            catch (FormatException)
            {
                MessageBoxX.Show(i18N.Translate("IP address format illegal. Try again."));

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
                UseCustomDNSCheckBox.Checked = Global.Settings.TUNTAP.UseCustomDNS;

                return;
            }
            try
            {
                var ProfileCount = int.Parse(ProfileCount_TextBox.Text);

                if (ProfileCount > -1)
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
                MessageBoxX.Show(i18N.Translate("ProfileCount value illegal. Try again."));

                return;
            }
            try
            {
                var STUN_Server = STUN_ServerTextBox.Text;
                Global.Settings.STUN_Server = STUN_Server;

                var STUN_ServerPort = int.Parse(STUN_ServerPortTextBox.Text);

                if (STUN_ServerPort > 0)
                {
                    Global.Settings.STUN_Server_Port = STUN_ServerPort;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                ProfileCount_TextBox.Text = Global.Settings.ProfileCount.ToString();
                MessageBoxX.Show(i18N.Translate("STUN_ServerPort value illegal. Try again."));

                return;
            }
            try
            {
                Global.Settings.StartedTcping = EnableStartedTcping_CheckBox.Checked;

                var DetectionInterval = int.Parse(DetectionInterval_TextBox.Text);

                if (DetectionInterval > 0)
                {
                    Global.Settings.StartedTcping_Interval = DetectionInterval;
                }
                else
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                ProfileCount_TextBox.Text = Global.Settings.ProfileCount.ToString();
                MessageBoxX.Show(i18N.Translate("Detection interval value illegal. Try again."));

                return;
            }

            Global.Settings.ACL = AclAddr.Text;

            Global.Settings.TUNTAP.Address = TUNTAPAddressTextBox.Text;
            Global.Settings.TUNTAP.Netmask = TUNTAPNetmaskTextBox.Text;
            Global.Settings.TUNTAP.Gateway = TUNTAPGatewayTextBox.Text;

            Global.Settings.TUNTAP.DNS.Clear();
            foreach (var ip in TUNTAPDNSTextBox.Text.Split(','))
            {
                Global.Settings.TUNTAP.DNS.Add(ip);
            }

            Global.Settings.TUNTAP.UseCustomDNS = UseCustomDNSCheckBox.Checked;
            Global.Settings.TUNTAP.ProxyDNS = ProxyDNSCheckBox.Checked;
            Global.Settings.TUNTAP.UseFakeDNS = UseFakeDNSCheckBox.Checked;

            #region 检查端口是否被占用
            if (PortHelper.PortInUse(Global.Settings.Socks5LocalPort))
            {
                MessageBoxX.Show(i18N.Translate("The Socks5 port is in use. Please reset."));
                return;
            }

            if (PortHelper.PortInUse(Global.Settings.HTTPLocalPort))
            {
                MessageBoxX.Show(i18N.Translate("The HTTP port is in use. Please reset."));
                return;
            }

            if (PortHelper.PortInUse(Global.Settings.RedirectorTCPPort, PortType.TCP))
            {
                MessageBoxX.Show(i18N.Translate("The RedirectorTCP port is in use. Please reset."));
                return;
            }
            #endregion

            Configuration.Save();
            MessageBoxX.Show(i18N.Translate("Saved"));
            Close();
        }
    }
}
